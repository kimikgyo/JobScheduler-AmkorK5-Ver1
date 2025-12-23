using Common.Models.Bases;
using Common.Models.Jobs;
using Common.Templates;

namespace JOB.Services
{
    public partial class SchedulerService
    {
        public void JobReassigned()
        {
            // ============================================================
            // [LOG] 재할당 스캔 시작
            // ============================================================
            //EventLogger.Info($"[ASSIGN][REASSIGN][SCAN][START] scanning PICK COMPLETED missions for reassign.");

            // ============================================================
            // 1) 우선 "Pick 이 COMPLETED 된 Mission" 만 조회
            // ============================================================
            var pickCompletedMissions = _repository.Missions.GetAll().Where(m => m.type == nameof(MissionSubType.PICK) && m.state == nameof(MissionState.COMPLETED)).ToList();

            if (pickCompletedMissions == null || pickCompletedMissions.Count == 0)
            {
                //EventLogger.Info($"[ASSIGN][REASSIGN][SCAN][NO-PICK-COMPLETED] no PICK COMPLETED missions found. reassign skipped.");
                return;
            }

            EventLogger.Info($"[ASSIGN][REASSIGN][SCAN][FOUND], pickCompletedCount={pickCompletedMissions.Count}");

            int triggerCount = 0;

            foreach (var mission in pickCompletedMissions)
            {
                if (mission == null)
                    continue;

                // 2-1) Worker 조회
                if (mission.assignedWorkerId == null)
                {
                    EventLogger.Warn($"[ASSIGN][REASSIGN][SKIP][NO-WORKER], missionId={mission.guid}, jobId={mission.jobId}");
                    continue;
                }

                var worker = _repository.Workers.GetById(mission.assignedWorkerId);
                if (worker == null)
                {
                    EventLogger.Warn($"[ASSIGN][REASSIGN][SKIP][WORKER-NOT-FOUND], workerId={mission.assignedWorkerId}, missionId={mission.guid}, jobId={mission.jobId}");
                    continue;
                }

                // =======================================================
                // (2-2) Worker Full 여부
                // =======================================================
                //if (worker.isFull)
                //{
                //    EventLogger.Info($"[ASSIGN][REASSIGN][SKIP][FULL], workerName={worker.name}, workerId={worker.id}, missionId={mission.guid}, jobId={mission.jobId}");
                //    continue;
                //}

                // =======================================================
                // (2-3) Mission Lock 여부
                // =======================================================
                if (mission.isLocked)
                {
                    EventLogger.Info($"[ASSIGN][REASSIGN][SKIP][LOCKED], workerName={worker.name}, workerId={worker.id}, missionId={mission.guid}, jobId={mission.jobId}");
                    continue;
                }
                // --------------------------------------------------------
                // 2-4) 이 Worker가 가지고 있는 Mission 목록 조회
                // --------------------------------------------------------
                var workerMissions = _repository.Missions.GetByAssignedWorkerId(worker.id).ToList();

                if (workerMissions == null || workerMissions.Count == 0)
                {
                    EventLogger.Info($"[ASSIGN][REASSIGN][SKIP][NO-WORKER-MISSIONS], workerName={worker.name}, workerId={worker.id}, missionId={mission.guid}, jobId={mission.jobId}");
                    continue;
                }

                // =======================================================
                // (A) 미완료 PICK 존재 여부
                // =======================================================
                bool hasUnfinishedPick = false;

                foreach (var m2 in workerMissions)
                {
                    if (m2.type == nameof(MissionSubType.PICK) &&
                        m2.state != nameof(MissionState.COMPLETED))
                    {
                        hasUnfinishedPick = true;
                        break;
                    }
                }

                if (hasUnfinishedPick)
                {
                    EventLogger.Info($"[ASSIGN][REASSIGN][SKIP][UNFINISHED-PICK], workerName={worker.name}, workerId={worker.id}, missionId={mission.guid}, jobId={mission.jobId}");
                    continue;
                }

                // =======================================================
                // (B) 엘리베이터 이동 중인지 여부
                // =======================================================
                bool hasExecutingElevator = false;

                foreach (var m2 in workerMissions)
                {
                    if (m2.state == nameof(MissionState.EXECUTING) &&
                        (m2.type == nameof(MissionSubType.ELEVATORENTERMOVE) ||
                         m2.type == nameof(MissionSubType.ELEVATOREXITMOVE) ||
                         m2.type == nameof(MissionSubType.ELEVATORSOURCEFLOOR) ||
                         m2.type == nameof(MissionSubType.ELEVATORDESTINATIONFLOOR)))
                    {
                        hasExecutingElevator = true;
                        break;
                    }
                }

                if (hasExecutingElevator)
                {
                    EventLogger.Info($"[ASSIGN][REASSIGN][SKIP][ELEVATOR-EXECUTING], workerName={worker.name}, workerId={worker.id}, missionId={mission.guid}, jobId={mission.jobId}");
                    continue;
                }

             

                // =======================================================
                // 재할당 트리거 조건 충족 → 수행
                // =======================================================
                EventLogger.Info($"[ASSIGN][REASSIGN][TRIGGER] reassign start, workerName={worker.name}, workerId={worker.id}, missionId={mission.guid}, jobId={mission.jobId}");

                JobReassignAfter(worker, mission);
                triggerCount++;

                EventLogger.Info($"[ASSIGN][REASSIGN][TRIGGER][DONE], workerName={worker.name}, workerId={worker.id}, missionId={mission.guid}, jobId={mission.jobId}");
            }

            // ============================================================
            // [LOG] 재할당 스캔 종료
            // ============================================================
            EventLogger.Info($"[ASSIGN][REASSIGN][SCAN][END] pickCompletedCount={pickCompletedMissions.Count}, triggeredReassignCount={triggerCount}");
        }

        /// <summary>
        ///  PICK 미션을 완료한 Worker 에 대해
        ///  - Unassigned Job 하나를 선택해서
        ///  - 해당 Worker 에 재할당하고
        ///  - Worker 의 Mission 큐를 재구성한다.
        ///  ※ 재할당 불가 조건(1번 조건)은 이미 JobReassigned 에서 필터링된 상태라고 가정.
        /// </summary>
        /// <summary>
        /// JobReassigned() 에서 재할당 트리거가 발생했을 때,
        /// 실제로 Unassigned Job 중 하나를 선택해서
        /// - MissionQueue 에 끼워 넣고
        /// - Job 을 Worker 에 할당하는 함수
        /// </summary>
        private void JobReassignAfter(Worker worker, Mission completedMission)
        {
            // ------------------------------------------------------------
            // [0] 기본 유효성 / 안전 체크
            // ------------------------------------------------------------
            if (worker == null)
            {
                EventLogger.Warn($"[ASSIGN][REASSIGN][AFTER][SKIP] worker is null.");
                return;
            }

            if (completedMission == null)
            {
                EventLogger.Warn($"[ASSIGN][REASSIGN][AFTER][SKIP] completedMission is null. workerName={worker.name}, workerId={worker.id}");
                return;
            }

            // (이중 방어) Worker 자재 Full 이면 재할당 시도 안 함
            //if (worker.isFull)
            //{
            //    EventLogger.Info($"[ASSIGN][REASSIGN][AFTER][SKIP][FULL] workerName={worker.name}, workerId={worker.id}, missionId={completedMission.guid}, jobId={completedMission.jobId}");
            //    return;
            //}

            EventLogger.Info($"[ASSIGN][REASSIGN][AFTER][START] workerName={worker.name}, workerId={worker.id}, missionId={completedMission.guid}, jobId={completedMission.jobId}");

            // ------------------------------------------------------------
            // [1] Unassigned Job 목록 조회
            //      - priority 내림차순
            //      - createdAt 오름차순
            // ------------------------------------------------------------
            var unAssignedJobs = _repository.Jobs.UnAssignedJobs().OrderByDescending(j => j.priority).ThenBy(j => j.createdAt).ToList();

            if (unAssignedJobs == null || unAssignedJobs.Count == 0)
            {
                EventLogger.Info($"[ASSIGN][REASSIGN][AFTER][NO-JOB] workerName={worker.name}, workerId={worker.id} → Unassigned Job 없음.");
                return;
            }

            // ------------------------------------------------------------
            // [2] 배터리 설정 조회 (WorkerCondition 에서 사용)
            // ------------------------------------------------------------
            var batterySetting = _repository.Battery.GetAll();
            if (batterySetting == null)
            {
                EventLogger.Warn($"[ASSIGN][REASSIGN][AFTER][SKIP][NO-BATTERY] workerName={worker.name}, workerId={worker.id}");
                return;
            }

            // ------------------------------------------------------------
            // [3] 재할당 후보 Job 필터링
            //      - 같은 group
            //      - 타입이 CHARGE/WAIT 이 아닌 Job
            //      - 지정 워커 규칙 반영:
            //          * specifiedWorkerId 없음  → 어떤 워커든 가능
            //          * specifiedWorkerId == worker.id → 이 워커 전용
            //          * else → 이 워커는 해당 Job 후보에서 제외
            // ------------------------------------------------------------
            var candidateJobs = new List<Job>();

            foreach (var job in unAssignedJobs)
            {
                // group 이 다르면 스킵
                if (job.group != worker.group)
                {
                    continue;
                }

                // CHARGE / WAIT Job 은 재할당 대상에서 제외
                if (job.type == nameof(JobType.CHARGE) ||
                    job.type == nameof(JobType.WAIT))
                {
                    continue;
                }

                // 지정 워커(Job.specifiedWorkerId) 체크
                bool specifiedIsEmpty = IsInvalid(job.specifiedWorkerId);
                bool specifiedIsThisWorker = (!specifiedIsEmpty && job.specifiedWorkerId == worker.id);

                // 지정이 없거나, 나에게 지정된 Job 만 후보에 포함
                if (specifiedIsEmpty || specifiedIsThisWorker)
                {
                    candidateJobs.Add(job);
                }
                else
                {
                    // 다른 워커에게 지정된 Job → 이 워커 기준 후보에서 제외
                    continue;
                }
            }

            if (candidateJobs == null || candidateJobs.Count == 0)
            {
                EventLogger.Info($"[ASSIGN][REASSIGN][AFTER][NO-CANDIDATE] workerName={worker.name}, workerId={worker.id} → 조건에 맞는 후보 Job 없음.");
                return;
            }

            EventLogger.Info($"[ASSIGN][REASSIGN][AFTER][CANDIDATE] workerName={worker.name}, workerId={worker.id}, candidateCount={candidateJobs.Count}");

            // ------------------------------------------------------------
            // [4] 이 Worker 에게 가장 적합한 Job 하나 선택
            //      - 거리 기준 (SelectNearestJobForWorker 사용)
            // ------------------------------------------------------------
            var jobToReassign = SelectNearestJobForWorker(worker, candidateJobs);

            if (jobToReassign == null)
            {
                EventLogger.Info($"[ASSIGN][REASSIGN][AFTER][SKIP][NO-SELECT] workerName={worker.name}, workerId={worker.id} → SelectNearestJobForWorker 결과 없음.");
                return;
            }

            EventLogger.Info($"[ASSIGN][REASSIGN][AFTER][SELECT] workerName={worker.name}, workerId={worker.id}, secondJobId={jobToReassign.guid}, jobType={jobToReassign.type}");

            // ------------------------------------------------------------
            // [5] WorkerCondition 검사 (배터리, 상태 등)
            // ------------------------------------------------------------
            bool canRun = WorkerCondition(jobToReassign, worker, batterySetting);
            if (!canRun)
            {
                EventLogger.Info($"[ASSIGN][REASSIGN][AFTER][SKIP][WORKER-CONDITION] workerName={worker.name}, workerId={worker.id}, secondJobId={jobToReassign.guid}");
                return;
            }

            // ------------------------------------------------------------
            // [6] 재할당 Job 을 반영해서 MissionQueue 재구성
            //      - InsertReassignJobMission 내부에서:
            //          * A_CompletedSegment
            //          * Second_PreMission
            //          * A_RemainSegment
            //          * Second_FinalMission
            //        구조로 새 큐를 구성하고 sequence 재배열
            // ------------------------------------------------------------
            bool ok = InsertReassignJobMission(worker, completedMission, jobToReassign);

            if (!ok)
            {
                EventLogger.Warn($"[ASSIGN][REASSIGN][AFTER][FAIL][REBUILD-MISSION] workerName={worker.name}, workerId={worker.id}, secondJobId={jobToReassign.guid}");
                return;
            }

            EventLogger.Info($"[ASSIGN][REASSIGN][AFTER][REBUILD-MISSION][OK] workerName={worker.name}, workerId={worker.id}, secondJobId={jobToReassign.guid}");

            // ------------------------------------------------------------
            // [7] 기존 WAIT 미션 정리 (정책에 따라)
            //      - 재구성 후에도 Worker 에 WAIT 미션이 필요 없다면 삭제
            // ------------------------------------------------------------
            ChangeWaitDeleteJob(worker, "[ASSIGN][REASSIGN][AFTER]");
            //if (!waitCleaned)
            //{
            //    // WAIT 삭제 실패했다고 해서 재구성 자체를 롤백하지는 않음(정책에 따라 조정 가능)
            //    EventLogger.Warn($"[ASSIGN][REASSIGN][AFTER][WARN][WAIT-CLEAN-FAIL] workerName={worker.name}, workerId={worker.id}, secondJobId={jobToReassign.guid}");
            //    // 필요하다면 여기서 return; 으로 바꿔도 됨
            //}

            // ------------------------------------------------------------
            // [8] Job 자체를 Worker 에 재할당 (DB 업데이트)
            // ------------------------------------------------------------
            jobToReassign.assignedWorkerId = worker.id;
            jobToReassign.state = nameof(JobState.WORKERASSIGNED);
            _repository.Jobs.Update(jobToReassign);

            EventLogger.Info($"[ASSIGN][REASSIGN][AFTER][DONE] workerName={worker.name}, workerId={worker.id}, secondJobId={jobToReassign.guid}, jobState={jobToReassign.state}");
        }

        /// <summary>
        /// 재할당 시 기존 Job 들의 Mission 큐에
        /// 새 Job 을 끼워 넣으면서, Drop 목적지 체인을
        /// JobA → JobB → JobC → ... 순서로 이어가도록 재구성한다.
        ///
        /// - completedMission 이 속한 Job 을 "firstJob" 으로 보고
        ///   그 Job 의 Pick 이후 구간 일부를 잘라내고,
        ///   새 Job(secondJob) 의 PreMission + FinalMission 을 삽입한다.
        /// - FinalMission 의 시작 위치는
        ///   "현재 Worker 미션 큐에서 가장 마지막에 있는 Job 의 목적지" 기준으로
        ///   → JobA 목적지 → JobB 목적지 → JobC 목적지 ... 형태의 체인을 만든다.
        /// </summary>
        private bool InsertReassignJobMission(Worker worker, Mission completedMission, Job jobSecond)
        {
            // ------------------------------------------------------------
            // [0] 기본 유효성 확인
            // ------------------------------------------------------------
            if (worker == null)
            {
                EventLogger.Warn("[ASSIGN][REASSIGN][MERGE][FAIL] worker is null.");
                return false;
            }

            if (completedMission == null)
            {
                EventLogger.Warn($"[ASSIGN][REASSIGN][MERGE][FAIL] completedMission is null. workerName={worker.name}, workerId={worker.id}");
                return false;
            }

            if (jobSecond == null)
            {
                EventLogger.Warn($"[ASSIGN][REASSIGN][MERGE][FAIL] secondJob is null. workerName={worker.name}, workerId={worker.id}, completedJobId={completedMission.jobId}");
                return false;
            }

            EventLogger.Info($"[ASSIGN][REASSIGN][MERGE][START] workerName={worker.name}, workerId={worker.id}, completedMissionId={completedMission.guid}" +
                             $", firstJobId={completedMission.jobId}, secondJobId={jobSecond.guid}");

            // ------------------------------------------------------------
            // [1] JobFirst 조회 (completedMission 이 속해 있는 Job)
            // ------------------------------------------------------------
            var jobFirst = _repository.Jobs.GetByid(completedMission.jobId);
            if (jobFirst == null)
            {
                EventLogger.Warn($"[ASSIGN][REASSIGN][MERGE][FAIL][FIRST-JOB-NOTFOUND] workerName={worker.name}, workerId={worker.id}, firstJobId={completedMission.jobId}");
                return false;
            }

            // ------------------------------------------------------------
            // [2] RoutePlan API (Resource 서비스) 조회 준비
            // ------------------------------------------------------------
            var resource = _repository.ServiceApis.GetAll().FirstOrDefault(s => s.type == "Resource");
            if (resource == null)
            {
                EventLogger.Warn($"[ASSIGN][REASSIGN][MERGE][FAIL][NO-RESOURCE-SERVICE] workerName={worker.name}, workerId={worker.id}, firstJobId={jobFirst.guid}" +
                                 $", secondJobId={jobSecond.guid}");
                return false;
            }

            // ------------------------------------------------------------
            // [3] Worker 기준 가장 가까운 Position 조회
            // ------------------------------------------------------------
            var posList = _repository.Positions.MiR_GetByMapId(worker.mapId);
            if (posList == null || posList.Count == 0)
            {
                EventLogger.Warn($"[ASSIGN][REASSIGN][MERGE][FAIL][NO-POSITION-LIST] workerName={worker.name}, workerId={worker.id}, mapId={worker.mapId}");
                return false;
            }

            var workerNearestPosition = _repository.Positions.FindNearestWayPoint(worker, posList).FirstOrDefault();
            if (workerNearestPosition == null)
            {
                EventLogger.Warn($"[ASSIGN][REASSIGN][MERGE][FAIL][NO-NEAREST-POSITION] workerName={worker.name}, workerId={worker.id}, mapId={worker.mapId}");
                return false;
            }

            EventLogger.Info($"[ASSIGN][REASSIGN][MERGE][WORKER-POS] workerName={worker.name}, workerId={worker.id}, nearestPositionId={workerNearestPosition.id}");

            // ------------------------------------------------------------
            // [4] "현재 큐에서 마지막 목적지" 계산
            //
            //  - 기존 코드: firstJob.destinationId 를 사용
            //  - 변경 코드: 현재 Worker 에 할당된 전체 미션을 보고
            //               가장 sequence 가 큰 미션의 Job 을 기준으로
            //               그 Job 의 목적지(destinationId) Position 을 사용
            //
            //  이렇게 하면:
            //   1차 재할당: 마지막 Job = A → startPos = A 목적지
            //   2차 재할당: 현재 마지막 Job = B → startPos = B 목적지
            //   3차 재할당: 마지막 Job = C → startPos = C 목적지
            //   ...
            //   ⇒ A → B → C → D 체인 보장
            // ------------------------------------------------------------
            var allMissionsOfWorker = _repository.Missions.GetByAssignedWorkerId(worker.id).OrderBy(m => m.sequence).ToList();

            if (allMissionsOfWorker == null || allMissionsOfWorker.Count == 0)
            {
                EventLogger.Warn(
                    $"[ASSIGN][REASSIGN][MERGE][FAIL][NO-WORKER-MISSIONS] workerName={worker.name}, workerId={worker.id}"
                );
                return false;
            }

            // sequence 가 가장 큰 (큐의 맨 마지막) 미션
            var lastMission = allMissionsOfWorker.OrderBy(m => m.sequence).LastOrDefault();

            if (lastMission == null)
            {
                EventLogger.Warn($"[ASSIGN][REASSIGN][MERGE][FAIL][NO-LAST-MISSION] workerName={worker.name}, workerId={worker.id}");
                return false;
            }

            var lastJob = _repository.Jobs.GetByid(lastMission.jobId);
            if (lastJob == null)
            {
                EventLogger.Warn($"[ASSIGN][REASSIGN][MERGE][FAIL][LAST-JOB-NOTFOUND] workerName={worker.name}, workerId={worker.id}, lastMissionId={lastMission.guid}" +
                                 $", lastJobId={lastMission.jobId}");
                return false;
            }

            var lastDestinationPosition = _repository.Positions.GetById(lastJob.destinationId);
            if (lastDestinationPosition == null)
            {
                EventLogger.Warn($"[ASSIGN][REASSIGN][MERGE][FAIL][LAST-DEST-NOTFOUND] workerName={worker.name}, workerId={worker.id}" +
                                 $", lastJobId={lastJob.guid}, destId={lastJob.destinationId}");
                return false;
            }

            EventLogger.Info($"[ASSIGN][REASSIGN][MERGE][TAIL-START] workerName={worker.name}, workerId={worker.id}, tailStartJobId={lastJob.guid}" +
                             $", tailStartPosId={lastDestinationPosition.id}");

            // ------------------------------------------------------------
            // [5] JobSecond 의 출발/목적 Position 계산
            //      - 출발(sourceId) 없으면 Worker 현재 위치 사용
            // ------------------------------------------------------------
            Position secondSourcePosition = null;

            if (IsInvalid(jobSecond.sourceId))
            {
                // source 가 없으면 Worker 위치를 출발점으로 사용
                secondSourcePosition = workerNearestPosition;
                EventLogger.Info($"[ASSIGN][REASSIGN][MERGE][SECOND-SRC] use WORKER position as source. secondJobId={jobSecond.guid}, workerNearestPosId={workerNearestPosition.id}");
            }
            else
            {
                secondSourcePosition = _repository.Positions.GetById(jobSecond.sourceId);
                if (secondSourcePosition != null)
                {
                    EventLogger.Info($"[ASSIGN][REASSIGN][MERGE][SECOND-SRC] use jobSecond.sourceId. secondJobId={jobSecond.guid}, sourceId={jobSecond.sourceId}" +
                                     $", secondSourcePositionName={secondSourcePosition.name}, secondSourcePositionId={secondSourcePosition.id}");
                }
            }

            if (secondSourcePosition == null)
            {
                EventLogger.Warn($"[ASSIGN][REASSIGN][MERGE][FAIL][SECOND-SRC-NOTFOUND] secondJobId={jobSecond.guid}, sourceId={jobSecond.sourceId}");
                return false;
            }

            var secondDestinationPosition = _repository.Positions.GetById(jobSecond.destinationId);
            if (secondDestinationPosition == null)
            {
                EventLogger.Warn($"[ASSIGN][REASSIGN][MERGE][FAIL][SECOND-DEST-NOTFOUND] secondJobId={jobSecond.guid}, destId={jobSecond.destinationId}");
                return false;
            }

            // ------------------------------------------------------------
            // [6] 현재 Worker 에 할당된 전체 Mission 중,
            //     JobFirst 에 속한 미션들만 필터링
            // ------------------------------------------------------------
            var currentMissions = allMissionsOfWorker; // 이미 위에서 정렬한 목록 재사용

            var firstMissions = new List<Mission>();
            foreach (var item in currentMissions)
            {
                if (item.jobId == jobFirst.guid)
                {
                    firstMissions.Add(item);
                }
            }

            if (firstMissions.Count == 0)
            {
                EventLogger.Warn($"[ASSIGN][REASSIGN][MERGE][FAIL][NO-FIRST-MISSIONS] workerName={worker.name}, workerId={worker.id}, firstJobId={jobFirst.guid}");
                return false;
            }

            // ------------------------------------------------------------
            // [7-1] "지금 실행 중인 미션" 찾기
            // ------------------------------------------------------------
            Mission currentFirstMission = null;

            foreach (var item in firstMissions)
            {
                if (item.state == nameof(MissionState.EXECUTING))
                {
                    currentFirstMission = item;
                    break;
                }
            }

            if (currentFirstMission == null)
            {
                foreach (var item in firstMissions)
                {
                    if (item.guid == completedMission.guid)
                    {
                        currentFirstMission = item;
                        break;
                    }
                }
            }

            if (currentFirstMission == null)
            {
                EventLogger.Warn($"[ASSIGN][REASSIGN][MERGE][FAIL][NO-CURRENT-FIRST-MISSION] workerName={worker.name}, workerId={worker.id}, firstJobId={jobFirst.guid}");
                return false;
            }

            int cancelSeq = currentFirstMission.sequence;

            EventLogger.Info($"[ASSIGN][REASSIGN][MERGE][CURRENT-FIRST] workerName={worker.name}, workerId={worker.id}, firstJobId={jobFirst.guid}, cancelSeq={cancelSeq}" +
                             $", currentMissionId={currentFirstMission.guid}");

            // ------------------------------------------------------------
            // [7-2] ElevatorExitMission(A 의 EV Exit) 찾기
            // ------------------------------------------------------------
            Mission firstElevatorExit = null;

            foreach (var item in firstMissions)
            {
                if (item.type == nameof(MissionSubType.ELEVATOREXITMOVE))
                {
                    firstElevatorExit = item;
                    break;
                }
            }

            if (firstElevatorExit == null)
            {
                EventLogger.Warn($"[ASSIGN][REASSIGN][MERGE][FAIL][NO-EV-EXIT] workerName={worker.name}, workerId={worker.id}, firstJobId={jobFirst.guid}");
                return false;
            }

            int elevatorExitSeq = firstElevatorExit.sequence;

            EventLogger.Info($"[ASSIGN][REASSIGN][MERGE][EV-EXIT] workerName={worker.name}, workerId={worker.id}, firstJobId={jobFirst.guid}, elevatorExitSeq={elevatorExitSeq}" +
                             $", evExitMissionId={firstElevatorExit.guid}");

            // ------------------------------------------------------------
            // [8] JobFirst 의 Mission 을 4개 구간으로 나누기
            // ------------------------------------------------------------
            var First_CompletedSegment = new List<Mission>();
            var First_SkipSegment = new List<Mission>();
            var First_RemainSegment = new List<Mission>();
            Mission First_CancelMission = null;

            foreach (var item in firstMissions)
            {
                if (item.sequence < cancelSeq)
                {
                    First_CompletedSegment.Add(item);
                }
                else if (item.sequence == cancelSeq)
                {
                    First_CancelMission = item;
                }
                else if (item.sequence > cancelSeq && item.sequence <= elevatorExitSeq)
                {
                    First_SkipSegment.Add(item);
                }
                else if (item.sequence > elevatorExitSeq)
                {
                    First_RemainSegment.Add(item);
                }
            }

            EventLogger.Info($"[ASSIGN][REASSIGN][MERGE][SEGMENT] workerName={worker.name}, workerId={worker.id}, firstJobId={jobFirst.guid}" +
                             $", completedCount={First_CompletedSegment.Count} , skipCount={First_SkipSegment.Count}, remainCount={First_RemainSegment.Count}");

            // ------------------------------------------------------------
            // [8-1] Cancel 대상 미션 삭제 요청
            // ------------------------------------------------------------
            if (First_CancelMission != null)
            {
                EventLogger.Info($"[ASSIGN][REASSIGN][MERGE][CANCEL-MISSION] workerName={worker.name}, workerId={worker.id}, missionId={First_CancelMission.guid}" +
                                 $", sequence={First_CancelMission.sequence}");

                bool deleted = deleteMission(First_CancelMission);
                if (!deleted)
                {
                    EventLogger.Warn($"[ASSIGN][REASSIGN][MERGE][FAIL][CANCEL-DELETE] workerName={worker.name}, workerId={worker.id}, missionId={First_CancelMission.guid}");
                    return false;
                }
            }

            // ------------------------------------------------------------
            // [8-2] Skip 대상 구간 상태를 SKIPPED 로 변경
            // ------------------------------------------------------------
            foreach (var skipMission in First_SkipSegment)
            {
                updateStateMission(skipMission, nameof(MissionState.SKIPPED), true);
                EventLogger.Info($"[ASSIGN][REASSIGN][MERGE][SKIP-MISSION] workerName={worker.name}, workerId={worker.id}, missionId={skipMission.guid}" +
                                 $", sequence={skipMission.sequence}");
            }

            // ------------------------------------------------------------
            // [9] Second_PreMission 생성
            //    - Worker 현재 위치(workerNearestPosition) → secondSourcePosition → (B 의 PICK/Elevator)
            // ------------------------------------------------------------
            var Second_PreMission = BuildSecondPreMissions(jobSecond, worker, resource, workerNearestPosition, secondSourcePosition);

            if (Second_PreMission != null)
            {
                EventLogger.Info($"[ASSIGN][REASSIGN][MERGE][SECOND-PRE] workerName={worker.name}, workerId={worker.id}, secondJobId={jobSecond.guid}" +
                                 $", count={Second_PreMission.Count}");
            }

            // ------------------------------------------------------------
            // [10] Second_FinalMission 생성
            //     - ★ 중요: 시작 위치 = lastDestinationPosition
            //       (현재 큐에서 마지막 Job 의 목적지)
            //     - lastDestinationPosition → secondDestinationPosition → B Drop ...
            //  기존: firstDestination → 변경: lastDestinationPosition
            // ------------------------------------------------------------
            var Second_FinalMission = BuildSecondFinalMissions(jobSecond, worker, resource, lastDestinationPosition, secondDestinationPosition);

            if (Second_FinalMission != null)
            {
                EventLogger.Info($"[ASSIGN][REASSIGN][MERGE][SECOND-FINAL] workerName={worker.name}, workerId={worker.id}, secondJobId={jobSecond.guid}" +
                                 $", count={Second_FinalMission.Count}");
            }

            // ------------------------------------------------------------
            // [11] 새 Mission Queue 재구성
            //      순서:
            //        1) A_CompletedSegment
            //        2) Second_PreMission
            //        3) First_RemainSegment
            //        4) (기존 tail 까지 포함된) 나머지 미션들 + Second_FinalMission
            //
            //  ※ 여기서는 간단하게:
            //     - currentMissions 에서 JobFirst 관련 세그먼트들을 제거하고
            //     - 위에서 새로 구성한 순서로 삽입 + 기존 나머지 Job 들은 뒤에 그대로 두는 구조로 확장 가능
            //     (지금 버전은 기본 구조만 보여주는 예시)
            // ------------------------------------------------------------
            var newQueue = new List<Mission>();

            if (First_CompletedSegment.Count > 0)
            {
                newQueue.AddRange(First_CompletedSegment);
            }

            if (Second_PreMission != null && Second_PreMission.Count > 0)
            {
                newQueue.AddRange(Second_PreMission);
            }

            if (First_RemainSegment.Count > 0)
            {
                newQueue.AddRange(First_RemainSegment);
            }

            if (Second_FinalMission != null && Second_FinalMission.Count > 0)
            {
                newQueue.AddRange(Second_FinalMission);
            }

            if (newQueue.Count == 0)
            {
                EventLogger.Warn($"[ASSIGN][REASSIGN][MERGE][FAIL][EMPTY-QUEUE] workerName={worker.name}, workerId={worker.id}, firstJobId={jobFirst.guid}" +
                                 $", secondJobId={jobSecond.guid}");
                return false;
            }

            // ------------------------------------------------------------
            // [12] Sequence 재배열 및 DB Update
            // ------------------------------------------------------------
            int seq = 1;

            foreach (var mission in newQueue)
            {
                mission.sequence = seq;
                mission.assignedWorkerId = worker.id;

                _repository.Missions.Update(mission);

                EventLogger.Info($"[ASSIGN][REASSIGN][MERGE][UPDATE] workerName={worker.name}, workerId={worker.id}, missionId={mission.guid}" +
                                 $", sequence={mission.sequence}, jobId={mission.jobId}");

                seq++;
            }

            EventLogger.Info($"[ASSIGN][REASSIGN][MERGE][DONE] workerName={worker.name}, workerId={worker.id}, firstJobId={jobFirst.guid}, secondJobId={jobSecond.guid}" +
                             $", totalSequence={newQueue.Count}");

            return true;
        }

        /// <summary>
        /// 재할당 시, 두 번째 Job(jobSecond)의 "PICK 전까지" 미션들을 생성한다.
        ///
        /// 경로:
        ///   1) workerNearestPosition → secondSourcePosition 까지 MOVE / TRAFFIC / ELEVATOR
        ///   2) secondSourcePosition 에서 PICK 그룹 생성
        ///
        /// ※ 이 함수는 실제 Mission 엔티티를 생성( template_SingleMission / template_GroupMission )
        ///   한 뒤, DB에서 다시 조회해서 List<Mission> 으로 반환한다.
        /// </summary>
        private List<Mission> BuildSecondPreMissions(Job jobSecond, Worker worker, ServiceApi resource, Position workerNearestPosition, Position secondSourcePosition)
        {
            // 반환용 리스트
            var result = new List<Mission>();

            // ------------------------------------------------------------
            // [0] 기본 방어 코드
            // ------------------------------------------------------------
            if (jobSecond == null)
            {
                EventLogger.Warn("[ASSIGN][REASSIGN][BUILD-PRE][FAIL] jobSecond is null.");
                return result;
            }

            if (worker == null)
            {
                EventLogger.Warn($"[ASSIGN][REASSIGN][BUILD-PRE][FAIL] worker is null. jobSecondId={jobSecond.guid}");
                return result;
            }

            if (resource == null)
            {
                EventLogger.Warn($"[ASSIGN][REASSIGN][BUILD-PRE][FAIL] resource(ServiceApi) is null. workerName={worker.name}, workerId={worker.id}, jobSecondId={jobSecond.guid}");
                return result;
            }

            if (workerNearestPosition == null)
            {
                EventLogger.Warn($"[ASSIGN][REASSIGN][BUILD-PRE][FAIL] workerNearestPosition is null. workerName={worker.name},workerId={worker.id}, jobSecondId={jobSecond.guid}");
                return result;
            }

            if (secondSourcePosition == null)
            {
                EventLogger.Warn($"[ASSIGN][REASSIGN][BUILD-PRE][FAIL] secondSourcePosition is null. workerName={worker.name}, workerId={worker.id}, jobSecondId={jobSecond.guid}");
                return result;
            }

            EventLogger.Info($"[ASSIGN][REASSIGN][BUILD-PRE][START] workerName={worker.name}, workerId={worker.id}, workerPosId={workerNearestPosition.positionId}, " +
                             $"jobSecondId={jobSecond.guid}, secondSourcePosId={secondSourcePosition.positionId}");

            // ------------------------------------------------------------
            // [1] 시작/도착 Position 이 같다면:
            //     - 굳이 경로 계산 없이 PICK 그룹만 생성해도 된다.
            // ------------------------------------------------------------
            if (workerNearestPosition.positionId == secondSourcePosition.positionId)
            {
                int seqFrom = 1;

                EventLogger.Info($"[ASSIGN][REASSIGN][BUILD-PRE][DIRECT-PICK] workerName={worker.name}, workerId={worker.id}, jobSecondId={jobSecond.guid}" +
                                 $", posId={secondSourcePosition.positionId}");

                // 바로 PICK 그룹만 생성
                seqFrom = template_GroupMission(jobSecond, secondSourcePosition, worker, seqFrom, nameof(MissionsTemplateGroup.PICK));

                // 여기까지 미션 생성은 DB에 반영되었으므로, 다시 조회해서 result 채움
                result = _repository.Missions.GetByJobId(jobSecond.guid).Where(m => m.assignedWorkerId == worker.id).OrderBy(m => m.sequence).ToList();

                EventLogger.Info($"[ASSIGN][REASSIGN][BUILD-PRE][DIRECT-PICK][DONE] workerName={worker.name}, workerId={worker.id}, jobSecondId={jobSecond.guid}" +
                                 $", preMissionCount={result.Count}");

                return result;
            }

            // ------------------------------------------------------------
            // [2] Routes_Plan 호출:
            //     WorkerNearestPosition → secondSourcePosition
            // ------------------------------------------------------------
            var routesPlanRequest = _mapping.RoutesPlanas.Request(
                workerNearestPosition.positionId,
                secondSourcePosition.positionId);

            var routesPlanResponse = resource.Api.Post_Routes_Plan_Async(routesPlanRequest).Result;

            if (routesPlanResponse == null)
            {
                EventLogger.Warn($"[ASSIGN][REASSIGN][BUILD-PRE][FAIL][NO-ROUTE-RESP] workerName={worker.name}, workerId={worker.id}, jobSecondId={jobSecond.guid}");
                return result;
            }

            if (routesPlanResponse.nodes == null)
            {
                EventLogger.Warn($"[ASSIGN][REASSIGN][BUILD-PRE][FAIL][NO-ROUTE-NODES] workerName={worker.name}, workerId={worker.id}, jobSecondId={jobSecond.guid}");
                return result;
            }

            EventLogger.Info($"[ASSIGN][REASSIGN][BUILD-PRE][ROUTE] workerName={worker.name}, workerId={worker.id}, jobSecondId={jobSecond.guid}, nodeCount={routesPlanResponse.nodes.Count}");

            // Mission sequence 시작 값 (일단 1부터 시작; 이후 전체 Queue 에서 재정렬할 예정)
            int seq = 1;

            //ELEVATOR 중복생성을 막기위함.
            Position ElevatorSource = null;
            Position Elevatordest = null;

            // ------------------------------------------------------------
            // [3] RoutesPlan 의 nodes 를 순회하며 Mission 생성
            // ------------------------------------------------------------
            foreach (var node in routesPlanResponse.nodes)
            {
                Position position = null;

                if (!string.IsNullOrEmpty(node.positionId))
                {
                    position = _repository.Positions.GetByPositionId(node.positionId);
                }

                string nodeTypeUpper = string.Empty;
                if (!string.IsNullOrEmpty(node.nodeType))
                {
                    nodeTypeUpper = node.nodeType.ToUpper();
                }

                // --------------------------------------------------------
                // [3-1] 마지막 노드(= JobSecond 출발지 Position) → PICK 그룹
                // --------------------------------------------------------
                if (node.positionId == secondSourcePosition.positionId)
                {
                    if (position != null)
                    {
                        EventLogger.Info($"[ASSIGN][REASSIGN][BUILD-PRE][PICK-GROUP] workerName={worker.name}, workerId={worker.id}" +
                                         $", jobSecondId={jobSecond.guid}, pickPosId={position.positionId}, seqStart={seq}");

                        seq = template_GroupMission(jobSecond, position, worker, seq, nameof(MissionsTemplateGroup.PICK));
                    }

                    // 여기서 break 를 걸면, PICK 이후의 노드는 없다고 보는 정책(현재 설계상 OK)
                    // break;
                }
                // --------------------------------------------------------
                // [3-2] Elevator 노드 처리
                // --------------------------------------------------------
                else if (nodeTypeUpper == nameof(NodeType.ELEVATOR))
                {
                    if (ElevatorSource == null)
                    {
                        ElevatorSource = position;
                        seq = template_GroupMission(jobSecond, ElevatorSource, worker, seq, nameof(MissionsTemplateGroup.ELEVATORSOURCE));
                        EventLogger.Info($"[ASSIGN][REASSIGN][BUILD-PRE][ELEVATOR-GROUP][ELEVATORSOURCE] workerName={worker.name}, workerId={worker.id}, jobSecondId={jobSecond.guid}, seq={seq}");
                    }
                    else if (ElevatorSource != null)
                    {
                        Elevatordest = position;
                        seq = template_GroupMission(jobSecond, Elevatordest, worker, seq, nameof(MissionsTemplateGroup.ELEVATORDEST));
                        EventLogger.Info($"[ASSIGN][REASSIGN][BUILD-PRE][ELEVATOR-GROUP][ELEVATORDEST] workerName={worker.name}, workerId={worker.id}, jobSecondId={jobSecond.guid}, seq={seq}");
                    }
                }
                // --------------------------------------------------------
                // [3-3] TRAFFIC 노드 → TRAFFIC 그룹
                // --------------------------------------------------------
                else if (nodeTypeUpper == nameof(NodeType.TRAFFIC))
                {
                    if (position != null)
                    {
                        EventLogger.Info($"[ASSIGN][REASSIGN][BUILD-PRE][TRAFFIC-GROUP] workerName={worker.name}, workerId={worker.id}, jobSecondId={jobSecond.guid}, posId={position.positionId}, seq={seq}");

                        seq = template_GroupMission(jobSecond, position, worker, seq, nameof(MissionsTemplateGroup.TRAFFIC));
                    }
                }
                // --------------------------------------------------------
                // [3-4] 그 외 일반 노드 → MOVE(STOPOVERMOVE) 단일 미션
                // --------------------------------------------------------
                else
                {
                    if (position != null)
                    {
                        seq = template_SingleMission(jobSecond, position, worker, seq, nameof(MissionTemplateType.MOVE), nameof(MissionTemplateSubType.STOPOVERMOVE));

                        // 로깅은 너무 많아질 수 있으니 필요하면만
                        // EventLogger.Info($"[ASSIGN][REASSIGN][BUILD-PRE][MOVE] workerName={worker.name}, workerId={worker.id}, jobSecondId={jobSecond.guid}, posId={position.positionId}, seq={seq - 1}");
                    }
                }
            }

            // ------------------------------------------------------------
            // [4] 생성된 미션들을 DB 에서 다시 조회하여 반환용 리스트 구성
            //
            //    ※ 현재 시점에서는:
            //       - jobSecond 에 대한 Pre 구간 미션(PRE MOVE + PICK)이 모두 생성된 상태이고,
            //       - Final 구간(Second_FinalMission)은 아직 생성되지 않은 상태이므로
            //         → jobSecond 에 속한 모든 미션 = 곧 Pre 구간 전체라고 볼 수 있다.
            // ------------------------------------------------------------
            result = _repository.Missions.GetByJobId(jobSecond.guid).Where(m => m.assignedWorkerId == worker.id).OrderBy(m => m.sequence).ToList();

            EventLogger.Info($"[ASSIGN][REASSIGN][BUILD-PRE][DONE] workerName={worker.name}, workerId={worker.id}, jobSecondId={jobSecond.guid}, preMissionCount={result.Count}");

            return result;
        }

        /// <summary>
        /// 두 번째 Job(jobSecond)의 "최종 목적지(DROP)"까지 가는
        /// 마지막 구간 미션들을 생성한다.
        ///
        /// 경로:
        ///   1) startPosition(firstDestination) → secondDestinationPosition 까지
        ///      MOVE / TRAFFIC / ELEVATOR 미션들
        ///   2) secondDestinationPosition 에서 DROP 그룹 생성
        ///
        /// ※ InsertReassignJobMission 에서 startPosition 으로 넘기는 값은
        ///    "현재 큐의 마지막 Job 목적지 Position (tailStartPosition)" 이다.
        ///    즉, A→B→C→D ... 체인을 만드는 tail 구간 생성 함수.
        /// </summary>
        ///  tailStartPosition (A, B, C... 마지막 목적지)
        ///   jobSecond 의 최종 목적지 Position
        private List<Mission> BuildSecondFinalMissions(Job jobSecond, Worker worker, ServiceApi resource, Position firstDestination, Position secondDestinationPosition)
        {
            // 반환용 리스트
            var result = new List<Mission>();

            // ------------------------------------------------------------
            // [0] 기본 방어 코드
            // ------------------------------------------------------------
            if (jobSecond == null)
            {
                EventLogger.Warn("[ASSIGN][REASSIGN][BUILD-FINAL][FAIL] jobSecond is null.");
                return result;
            }

            if (worker == null)
            {
                EventLogger.Warn($"[ASSIGN][REASSIGN][BUILD-FINAL][FAIL] worker is null. jobSecondId={jobSecond.guid}");
                return result;
            }

            if (resource == null)
            {
                EventLogger.Warn($"[ASSIGN][REASSIGN][BUILD-FINAL][FAIL] resource(ServiceApi) is null. workerName={worker.name}, workerId={worker.id}, jobSecondId={jobSecond.guid}");
                return result;
            }

            if (firstDestination == null)
            {
                EventLogger.Warn($"[ASSIGN][REASSIGN][BUILD-FINAL][FAIL] firstDestination(startPosition) is null. workerName={worker.name},workerId={worker.id}, jobSecondId={jobSecond.guid}");
                return result;
            }

            if (secondDestinationPosition == null)
            {
                EventLogger.Warn($"[ASSIGN][REASSIGN][BUILD-FINAL][FAIL] secondDestination is null. workerName={worker.name},workerId={worker.id}, jobSecondId={jobSecond.guid}");
                return result;
            }

            EventLogger.Info($"[ASSIGN][REASSIGN][BUILD-FINAL][START] workerName={worker.name},workerId={worker.id}, jobSecondId={jobSecond.guid}, " +
                             $"startPosId={firstDestination.positionId}, finalPosId={secondDestinationPosition.positionId}");

            // ------------------------------------------------------------
            // [1] 시작/도착 Position 이 같다면:
            //     - 굳이 경로계산 없이 DROP 그룹만 생성
            // ------------------------------------------------------------
            if (firstDestination.positionId == secondDestinationPosition.positionId)
            {
                int seqFrom = 1;

                EventLogger.Info($"[ASSIGN][REASSIGN][BUILD-FINAL][DIRECT-DROP] workerName={worker.name}, workerId={worker.id}, jobSecondId={jobSecond.guid}" +
                                 $", posId={secondDestinationPosition.positionId}");

                // 같은 위치라면 바로 DROP 그룹 생성
                seqFrom = template_GroupMission(jobSecond, secondDestinationPosition, worker, seqFrom, nameof(MissionsTemplateGroup.DROP));

                // 생성된 미션들을 DB에서 다시 조회하여 result 에 채운다.
                result = _repository.Missions.GetByJobId(jobSecond.guid).Where(m => m.assignedWorkerId == worker.id).OrderBy(m => m.sequence).ToList();

                EventLogger.Info($"[ASSIGN][REASSIGN][BUILD-FINAL][DIRECT-DROP][DONE] workerName={worker.name}, workerId={worker.id}, jobSecondId={jobSecond.guid}" +
                                 $", finalMissionCount={result.Count}");

                return result;
            }

            // ------------------------------------------------------------
            // [2] Routes_Plan 호출:
            //     firstDestination → secondDestinationPosition
            //     (현재 tail 의 목적지 → jobSecond 최종 목적지)
            // ------------------------------------------------------------
            var routesPlanRequest = _mapping.RoutesPlanas.Request(firstDestination.positionId, secondDestinationPosition.positionId);

            var routesPlanResponse = resource.Api.Post_Routes_Plan_Async(routesPlanRequest).Result;

            if (routesPlanResponse == null)
            {
                EventLogger.Warn($"[ASSIGN][REASSIGN][BUILD-FINAL][FAIL][NO-ROUTE-RESP] workerName={worker.name}, workerId={worker.id}, jobSecondId={jobSecond.guid}");
                return result;
            }

            if (routesPlanResponse.nodes == null)
            {
                EventLogger.Warn($"[ASSIGN][REASSIGN][BUILD-FINAL][FAIL][NO-ROUTE-NODES] workerName={worker.name}, workerId={worker.id}, jobSecondId={jobSecond.guid}");
                return result;
            }

            EventLogger.Info($"[ASSIGN][REASSIGN][BUILD-FINAL][ROUTE] workerName={worker.name}, workerId={worker.id}, jobSecondId={jobSecond.guid}, nodeCount={routesPlanResponse.nodes.Count}");

            // Mission sequence 시작 값
            int seq = 1;

            //ELEVATOR 중복생성을 막기위함.
            Position ElevatorSource = null;
            Position Elevatordest = null;

            // ------------------------------------------------------------
            // [3] RoutesPlan 의 nodes 를 순회하며 Mission 생성
            // ------------------------------------------------------------
            foreach (var node in routesPlanResponse.nodes)
            {
                Position position = null;

                if (!string.IsNullOrEmpty(node.positionId))
                {
                    position = _repository.Positions.GetByPositionId(node.positionId);
                }

                string nodeTypeUpper = string.Empty;
                if (!string.IsNullOrEmpty(node.nodeType))
                {
                    nodeTypeUpper = node.nodeType.ToUpper();
                }

                // --------------------------------------------------------
                // [3-1] 마지막 노드(= JobSecond 최종 목적지) → DROP 그룹
                // --------------------------------------------------------
                if (node.positionId == secondDestinationPosition.positionId)
                {
                    if (position != null)
                    {
                        EventLogger.Info($"[ASSIGN][REASSIGN][BUILD-FINAL][DROP-GROUP] workerName={worker.name}, workerId={worker.id}, jobSecondId={jobSecond.guid}" +
                                         $", dropPosId={position.positionId}, seqStart={seq}");

                        // B 의 최종 목적지에서 DROP 그룹 생성
                        seq = template_GroupMission(jobSecond, position, worker, seq, nameof(MissionsTemplateGroup.DROP));
                    }

                    // DROP 까지 생성했으므로 이후 노드는 없다고 가정하고 종료 가능
                    // break;   // 정책상 필요하면 사용
                }
                // --------------------------------------------------------
                // [3-2] Elevator 노드 → Elevator 그룹 (최대 1번)
                // --------------------------------------------------------
                else if (nodeTypeUpper == nameof(NodeType.ELEVATOR))
                {
                    if (ElevatorSource == null)
                    {
                        ElevatorSource = position;
                        seq = template_GroupMission(jobSecond, ElevatorSource, worker, seq, nameof(MissionsTemplateGroup.ELEVATORSOURCE));
                        EventLogger.Info($"[ASSIGN][REASSIGN][BUILD-PRE][ELEVATOR-GROUP][ELEVATORSOURCE] workerName={worker.name}, workerId={worker.id}, jobSecondId={jobSecond.guid}, seq={seq}");
                    }
                    else if (ElevatorSource != null)
                    {
                        Elevatordest = position;
                        seq = template_GroupMission(jobSecond, Elevatordest, worker, seq, nameof(MissionsTemplateGroup.ELEVATORDEST));
                        EventLogger.Info($"[ASSIGN][REASSIGN][BUILD-PRE][ELEVATOR-GROUP][ELEVATORDEST] workerName={worker.name}, workerId={worker.id}, jobSecondId={jobSecond.guid}, seq={seq}");
                    }
                }
                // --------------------------------------------------------
                // [3-3] TRAFFIC 노드 → TRAFFIC 그룹
                // --------------------------------------------------------
                else if (nodeTypeUpper == nameof(NodeType.TRAFFIC))
                {
                    if (position != null)
                    {
                        EventLogger.Info($"[ASSIGN][REASSIGN][BUILD-FINAL][TRAFFIC-GROUP] workerName={worker.name}, workerId={worker.id}, jobSecondId={jobSecond.guid}" +
                                         $", posId={position.positionId}, seq={seq}");

                        seq = template_GroupMission(jobSecond, position, worker, seq, nameof(MissionsTemplateGroup.TRAFFIC));
                    }
                }
                // --------------------------------------------------------
                // [3-4] 그 외 일반 노드 → MOVE(STOPOVERMOVE) 단일 미션
                // --------------------------------------------------------
                else
                {
                    if (position != null)
                    {
                        seq = template_SingleMission(jobSecond, position, worker, seq, nameof(MissionTemplateType.MOVE), nameof(MissionTemplateSubType.STOPOVERMOVE));

                        // 너무 로그가 많아질 수 있어서 필요시만 활성화
                        // EventLogger.Info(
                        //     $"[ASSIGN][REASSIGN][BUILD-FINAL][MOVE] workerId={worker.id}, jobSecondId={jobSecond.guid}, posId={position.positionId}, seq={seq - 1}");
                    }
                }
            }

            // ------------------------------------------------------------
            // [4] 생성된 미션들을 DB 에서 다시 조회하여 result 리스트 구성
            //     (현재 시점에서는 jobSecond 에 해당하는 Final 구간 미션들 전체)
            // ------------------------------------------------------------
            result = _repository.Missions.GetByJobId(jobSecond.guid).Where(m => m.assignedWorkerId == worker.id).OrderBy(m => m.sequence).ToList();

            EventLogger.Info($"[ASSIGN][REASSIGN][BUILD-FINAL][DONE] workerName={worker.name}, workerId={worker.id}, jobSecondId={jobSecond.guid}, finalMissionCount={result.Count}");

            return result;
        }
    }
}