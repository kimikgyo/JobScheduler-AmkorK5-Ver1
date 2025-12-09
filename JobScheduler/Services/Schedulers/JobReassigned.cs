using Common.Models.Bases;
using Common.Models.Jobs;
using Common.Templates;

namespace JOB.Services
{
    public partial class SchedulerService
    {
        /// <summary>
        ///  Job 재할당 메인 루틴 (인자 없음)
        ///  - 1회 호출 시, 현재 미션 상태를 보고 재할당이 가능한지 판단하고 실행
        /// </summary>
        public void JobReassigned()
        {
            // [LOG] 재할당 스캔 시작
            //EventLogger.Info("[ASSIGN][REASSIGN][SCAN][START] JobReassigned scan started.");

            // 1) 재할당 판단 대상이 될 Mission 목록 조회
            //    - 상태:  PENDING / EXECUTING / COMPLETED 만 대상으로 함
            var runningMissions = _repository.Missions.GetAll().Where(m =>
                       m.state == nameof(MissionState.PENDING)
                    || m.state == nameof(MissionState.EXECUTING)
                    || m.state == nameof(MissionState.COMPLETED))
                .ToList();

            // 1-1) 조회된 Mission 이 하나도 없으면 재할당 처리할 것이 없음 → 종료
            if (runningMissions == null || runningMissions.Count == 0)
            {
                //EventLogger.Info("[ASSIGN][REASSIGN][SCAN][NO-MISSION] No mission to check for reassign.");
                return;
            }

            // 2) 각 Mission 을 순회하면서, Worker + Mission 상태를 보고 재할당 여부 판단
            foreach (var mission in runningMissions)
            {
                // 2-0) 기본 방어: assignedWorkerId 가 비어 있으면 스킵
                if (mission.assignedWorkerId == null)
                {
                    EventLogger.Info($"[ASSIGN][REASSIGN][SKIP][NO-WORKER] missionId={mission.guid}, jobId={mission.jobId}, state={mission.state}");
                    continue;
                }

                // 2-1) Mission 에 할당된 Worker 조회
                var worker = _repository.Workers.GetById(mission.assignedWorkerId);
                if (worker == null)
                {
                    EventLogger.Warn(
                        $"[ASSIGN][REASSIGN][SKIP][WORKER-NOT-FOUND] missionId={mission.guid}, jobId={mission.jobId}, assignedWorkerId={mission.assignedWorkerId}"
                    );
                    continue;
                }

                // 2-2) Lock / 적재량 Full 등으로 재할당 금지 조건 확인
                // ------------------------------------------------------------------
                // 2-2-1) Mission 자체가 Lock 이면 재할당 금지
                if (mission.isLocked)
                {
                    EventLogger.Info(
                        $"[ASSIGN][REASSIGN][SKIP][LOCK] workerId={worker.id}, workerName={worker.name}, missionId={mission.guid}, jobId={mission.jobId}"
                    );
                    continue;
                }

                // 2-2-2) Worker 적재량 Full 사용하는 경우 아래 활성화
                // if (worker.currentLoad >= worker.maxLoad)
                // {
                //     EventLogger.Info(
                //         $"[ASSIGN][REASSIGN][SKIP][FULL-LOAD] workerId={worker.id}, missionId={mission.id}, jobId={mission.jobId}"
                //     );
                //     continue;
                // }
                // ------------------------------------------------------------------

                // 3) "재할당을 하면 안 되는 조건" 정의
                //    - Mission 이 EXECUTING(실행 중)이고
                //    - 타입이 PICK 또는 Elevator 이동 관련 4종이면
                //      → 재할당 자체를 시도하지 않음
                bool cannotReassign =
                    mission.state == nameof(MissionState.EXECUTING) &&
                    (
                           mission.type == nameof(MissionSubType.PICK)
                        || mission.type == nameof(MissionSubType.ELEVATORENTERMOVE)
                        || mission.type == nameof(MissionSubType.ELEVATOREXITMOVE)
                        || mission.type == nameof(MissionSubType.ELEVATORSOURCEFLOOR)
                        || mission.type == nameof(MissionSubType.ELEVATORDESTINATIONFLOOR)
                    );

                if (cannotReassign)
                {
                    EventLogger.Info(
                        $"[ASSIGN][REASSIGN][SKIP][FORBIDDEN-STATE] workerId={worker.id}, workerName={worker.name}, missionId={mission.guid}, jobId={mission.jobId}, state={mission.state}, type={mission.type}"
                    );
                    continue;
                }

                // 4) "재할당이 되는 조건(트리거)" 정의
                //    - Mission 타입이 PICK 이고
                //    - 상태가 COMPLETED 인 경우
                bool canReassign = mission.type == nameof(MissionSubType.PICK) && mission.state == nameof(MissionState.COMPLETED);

                if (!canReassign)
                {
                    // 재할당 트리거는 아니지만, 스캔 대상인 Mission 이므로 조용히 패스
                    continue;
                }

                // [LOG] 재할당 트리거 발생 로그
                EventLogger.Info(
                    $"[ASSIGN][REASSIGN][TRIGGER] workerId={worker.id}, workerName={worker.name}, missionId={mission.guid}, jobId={mission.jobId}, type={mission.type}, state={mission.state}"
                );

                // 5) 실제 Job 재할당 로직 실행
                JobReassignAfter(worker, mission);
            }

            // [LOG] 재할당 스캔 완료
            //EventLogger.Info("[ASSIGN][REASSIGN][SCAN][DONE] JobReassigned scan completed.");
        }

        /// <summary>
        ///  PICK 미션을 완료한 Worker 에 대해
        ///  - Unassigned Job 하나를 선택해서
        ///  - 해당 Worker 에 재할당하고
        ///  - Worker 의 Mission 큐를 재구성한다.
        ///  ※ 재할당 불가 조건(1번 조건)은 이미 JobReassigned 에서 필터링된 상태라고 가정.
        /// </summary>
        private void JobReassignAfter(Worker worker, Mission completedMission)
        {
            if (worker == null || completedMission == null)
                return;

            // [LOG] 재할당 시도 시작
            //EventLogger.Info($"[ASSIGN][REASSIGN][START] workerId={worker.id}, completedMissionId={completedMission.guid}, jobId={completedMission.jobId}");

            //신규 Job 이있는지 확인
            var UnAssignedWorkerJobs = _repository.Jobs.UnAssignedJobs().OrderByDescending(r => r.priority).ThenBy(r => r.createdAt).ToList();

            if (UnAssignedWorkerJobs == null || UnAssignedWorkerJobs.Count == 0)
            {
                EventLogger.Info($"[ASSIGN][REASSIGN][NO-UNASSIGNED-JOB] workerId={worker.id}, workerName={worker.name}");
                return;
            }

            var batterySetting = _repository.Battery.GetAll();
            if (batterySetting == null)
            {
                EventLogger.Warn($"[ASSIGN][REASSIGN][ABORT][NO-BATTERY-SETTING] workerId={worker.id}, workerName={worker.name}");
                return;
            }

            // 1) 재할당 대상으로 사용할 Unassigned Job 목록 조회
            //    - 같은 group 의 Job
            //    - 타입이 CHARGE / WAIT 아닌 Job
            var candidateJobs = UnAssignedWorkerJobs
                .Where(j => j.group == worker.group && j.type != nameof(JobType.CHARGE) && j.type != nameof(JobType.WAIT)).ToList();

            // 1-1) 재할당 가능한 Job 이 없으면 종료
            if (candidateJobs == null || candidateJobs.Count == 0)
            {
                EventLogger.Info($"[ASSIGN][REASSIGN][NO-CANDIDATE] workerId={worker.id}, workerName={worker.name}, workerGroup={worker.group}");
                return;
            }

            // 2) 이 Worker 에게 가장 적합한 Job 하나 선택
            var jobToReassign = SelectNearestJobForWorker(worker, candidateJobs);

            // 2-1) 선택된 Job 이 없으면 종료
            if (jobToReassign == null)
            {
                EventLogger.Info($"[ASSIGN][REASSIGN][NO-JOB-SELECTED] workerId={worker.id}, workerName={worker.name}");
                return;
            }

            // 3) WorkerCondition 검사 (배터리, 상태 등)
            if (!WorkerCondition(jobToReassign, worker, batterySetting))
            {
                EventLogger.Info($"[ASSIGN][REASSIGN][BLOCKED-WORKER] workerId={worker.id}, workerName={worker.name}, jobId={jobToReassign.guid}, jobType={jobToReassign.type}");
                return;
            }

            // 6) 재할당된 Job 을 반영해서 Worker 의 Mission 큐 재구성
            EventLogger.Info($"[ASSIGN][REASSIGN][MQUEUE][REBUILD-TRY] workerId={worker.id}, workerName={worker.name}, jobSecondId={jobToReassign.guid}");
            bool ok = InsertReassignJobMission(worker, completedMission, jobToReassign);
            if (ok)
            {
                // 4) (옵션) 기존 WAIT 미션 정리
                if (!ChangeWaitDeleteMission(worker))
                {
                    EventLogger.Warn($"[ASSIGN][REASSIGN][ABORT][WAIT-CLEAN-FAIL] workerId={worker.id}, workerName={worker.name}, jobSecondId={jobToReassign.guid}");
                    return;
                }

                // 5) Job 자체를 Worker 에 재할당
                jobToReassign.assignedWorkerId = worker.id;
                jobToReassign.state = nameof(JobState.WORKERASSIGNED);
                _repository.Jobs.Update(jobToReassign);
                EventLogger.Info($"[ASSIGN][REASSIGN][DONE] workerId={worker.id}, workerName={worker.name}, jobSecondId={jobToReassign.guid}, jobType={jobToReassign.type}, group={jobToReassign.group}");
            }
            else
            {
                // 재구성 실패 → 이번 재할당 시도 포기.
                EventLogger.Warn($"[ASSIGN][REASSIGN][FAIL][MQUEUE-REBUILD] workerId={worker.id}, workerName={worker.name}, jobSecondId={jobToReassign.guid}");
                return;
            }
        }

        /// <summary>
        /// InsertReassignJobMission
        /// ------------------------------------------------------------
        /// JobFirst 의 PICK 미션이 끝난 이후,
        /// 현재 실행 중인 미션을 기준으로 JobSecond 를 끼워 넣어
        /// MissionQueue 를 다음 순서로 재구성한다.
        ///
        ///   1) First_CompletedSegment   : JobFirst 에서 이미 지나간 구간
        ///   2) Second_PreMission        : (WorkerNearest) → JobSecond 출발지(PICK/ELEVATOR 전까지)
        ///   3) First_RemainSegment      : JobFirst 의 남은 구간 (ElevatorExit 이후 ~ Drop 까지)
        ///   4) Second_FinalMission      : JobFirst 목적지 → JobSecond 목적지 → JobSecond DROP
        ///
        /// 추가 규칙:
        ///   - "지금 실행 중인 미션" 은 Cancel + deleteMission() 요청
        ///   - "Cancel 이후 ~ ElevatorExit 까지" 미션들도 deleteMission() 요청 대상
        ///   - deleteMission() 이 한 개라도 실패하면
        ///        → 재할당 전체를 중단하고 false 를 반환
        ///
        /// 경로(패스 플랜) 규칙:
        ///   - Second_PreMission      : WorkerNearestPosition → JobSecond Source(Pick)
        ///   - Second_FinalMission    : JobFirst Destination  → JobSecond Destination
        /// </summary>
        private bool InsertReassignJobMission(Worker worker, Mission completedMission, Job jobSecond)
        {
            // ------------------------------------------------------------
            // [0] 기본 유효성 확인
            // ------------------------------------------------------------
            if (worker == null) return false;
            if (completedMission == null) return false;
            if (jobSecond == null) return false;

            // ------------------------------------------------------------
            // [1] JobFirst 조회
            // ------------------------------------------------------------
            var jobFirst = _repository.Jobs.GetByid(completedMission.jobId);
            if (jobFirst == null) return false;

            // ------------------------------------------------------------
            // [2] RoutePlan API (Resource 서비스) 조회
            // ------------------------------------------------------------
            var resource = _repository.ServiceApis.GetAll().FirstOrDefault(s => s.type == "Resource");
            if (resource == null) return false;

            // ------------------------------------------------------------
            // [3] Worker 기준 가장 가까운 Position 조회
            // ------------------------------------------------------------
            var posList = _repository.Positions.MiR_GetByMapId(worker.mapId);
            if (posList == null || posList.Count == 0) return false;

            var workerNearestPosition = _repository.Positions.FindNearestWayPoint(worker, posList).FirstOrDefault();
            if (workerNearestPosition == null) return false;

            // ------------------------------------------------------------
            // [4] JobFirst 목적지 Position (Drop 위치)
            // ------------------------------------------------------------
            var firstDestination = _repository.Positions.GetById(jobFirst.destinationId);
            if (firstDestination == null) return false;

            // ------------------------------------------------------------
            // [5] JobSecond 의 출발/목적 Position 계산
            // ------------------------------------------------------------
            Position secondSourcePosition = null;

            if (IsInvalid(jobSecond.sourceId))
            {
                // source 가 없으면 Worker 위치를 출발점으로 사용
                secondSourcePosition = workerNearestPosition;
            }
            else
            {
                secondSourcePosition = _repository.Positions.GetById(jobSecond.sourceId);
            }

            if (secondSourcePosition == null) return false;

            var secondDestinationPosition = _repository.Positions.GetById(jobSecond.destinationId);
            if (secondDestinationPosition == null) return false;

            // ------------------------------------------------------------
            // [6] 현재 Worker 에 할당된 전체 Mission 조회
            // ------------------------------------------------------------
            var currentMissions = _repository.Missions.GetByAssignedWorkerId(worker.id).OrderBy(m => m.sequence).ToList();

            if (currentMissions == null || currentMissions.Count == 0)
                return false;

            // ------------------------------------------------------------
            // [7] JobFirst 에 속한 Mission 만 필터링
            // ------------------------------------------------------------
            var firstMissions = currentMissions.Where(m => m.jobId == jobFirst.guid).OrderBy(m => m.sequence).ToList();
            if (firstMissions == null || firstMissions.Count == 0)
                return false;

            // ------------------------------------------------------------
            // [7-1] "지금 실행 중인 미션" 찾기
            // ------------------------------------------------------------
            var currentFirstMission = firstMissions.FirstOrDefault(m => m.state == nameof(MissionState.EXECUTING));

            if (currentFirstMission == null)
            {
                // EXECUTING 이 없으면, 최소 completedMission 기준으로 사용
                currentFirstMission = firstMissions.FirstOrDefault(m => m.guid == completedMission.guid);

                if (currentFirstMission == null) return false;
            }

            int cancelSeq = currentFirstMission.sequence;

            // ------------------------------------------------------------
            // [7-2] ElevatorExitMission 찾기
            // ------------------------------------------------------------
            var firstElevatorExit = firstMissions.FirstOrDefault(m => m.type == nameof(MissionSubType.ELEVATOREXITMOVE));
            if (firstElevatorExit == null) return false;
            int elevatorExitSeq = firstElevatorExit.sequence;

            // ------------------------------------------------------------
            // [8] JobFirst 의 Mission 을 4개 구간으로 나누기
            // ------------------------------------------------------------

            // 1) 이미 지나간 구간
            var First_CompletedSegment = firstMissions.Where(m => m.sequence < cancelSeq).ToList();

            // 2) 지금 실행 중인 미션 1개 (Cancel 대상)
            var First_CancelMission = firstMissions.FirstOrDefault(m => m.sequence == cancelSeq);

            // 3) Cancel 이후 ~ ElevatorExit 까지 (Skip + Delete 대상)
            var First_SkipSegment = firstMissions.Where(m => m.sequence > cancelSeq && m.sequence <= elevatorExitSeq).ToList();

            // 4) ElevatorExit 이후 ~ Drop 까지 (A 의 남은 구간)
            var First_RemainSegment = firstMissions.Where(m => m.sequence > elevatorExitSeq).ToList();

            // ------------------------------------------------------------
            // [8-1] Cancel 대상 미션에 대한 deleteMission 호출
            //       - 삭제 실패 시 전체 재할당 중단
            // ------------------------------------------------------------
            if (First_CancelMission != null)
            {
                bool deleted = deleteMission(First_CancelMission);
                // Service 에 미션 삭제 요청 실패 → 재할당 시도 포기
                if (deleted == false) return false;
            }

            // ------------------------------------------------------------
            // [8-2] Skip 대상 구간들에 Skipped 상태로 변경
            // ------------------------------------------------------------
            foreach (var skipMission in First_SkipSegment)
            {
                updateStateMission(skipMission, nameof(MissionState.SKIPPED), true);
            }

            // 여기까지 왔다면:
            //  - 현재 실행중인 미션 + EV Exit 까지 이어지는 구간은
            //    Service / Worker 쪽에서 실제 삭제 완료된 상태
            //  - DB 상에서도 삭제된 상태 (혹은 상태만 남겨도 됨 → 정책에 따라 조정)

            // ------------------------------------------------------------
            // [9] Second_PreMission 생성
            // ------------------------------------------------------------
            var Second_PreMission = BuildSecondPreMissions(jobSecond, worker, resource, workerNearestPosition, secondSourcePosition);

            // ------------------------------------------------------------
            // [10] Second_FinalMission 생성
            // ------------------------------------------------------------
            var Second_FinalMission = BuildSecondFinalMissions(jobSecond, worker, resource, firstDestination, secondDestinationPosition);

            // ------------------------------------------------------------
            // [11] 새 Mission Queue 재구성
            // ------------------------------------------------------------
            var newQueue = new List<Mission>();

            if (First_CompletedSegment != null && First_CompletedSegment.Count > 0)
            {
                newQueue.AddRange(First_CompletedSegment);
            }

            if (Second_PreMission != null && Second_PreMission.Count > 0)
            {
                newQueue.AddRange(Second_PreMission);
            }

            if (First_RemainSegment != null && First_RemainSegment.Count > 0)
            {
                newQueue.AddRange(First_RemainSegment);
            }

            if (Second_FinalMission != null && Second_FinalMission.Count > 0)
            {
                newQueue.AddRange(Second_FinalMission);
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
                seq++;
            }

            return true;
        }

        /// <summary>
        /// BuildSecondPreMissions
        /// ------------------------------------------------------------
        /// Second_PreMission 구간을 생성한다.
        ///
        /// 목적:
        ///   - 현재 Worker 위치(WorkerNearestPosition)에서
        ///     JobSecond 의 출발지(secondSourcePosition, 보통 PICK 위치)까지
        ///     이동 경로를 계산하고, 그에 따른 Mission 들을 생성한다.
        ///
        /// 경로(패스 플랜) 규칙:
        ///   - 시작: workerNearestPosition.positionId
        ///   - 도착: secondSourcePosition.positionId
        ///
        /// 미션 생성 규칙:
        ///   - 중간 노드:
        ///       → MOVE(STOPOVERMOVE) 또는 TRAFFIC/ELEVATOR 그룹 (NodeType 에 따라)
        ///   - 마지막 노드(= secondSourcePosition):
        ///       → MissionsTemplateGroup.PICK 그룹 생성
        ///
        /// 주의:
        ///   - 실제 Mission 엔티티 생성 / DB Insert 는
        ///     create_SingleMission / create_GroupMission 내부에서 수행한다고 가정.
        ///   - 이 함수의 반환 List<Mission> 은 선택적으로 활용 가능하며,
        ///     필요 시 _repository.Missions 에서 JobSecond 기준으로 다시 조회해 사용하는 것을 추천.
        /// </summary>
        private List<Mission> BuildSecondPreMissions(Job jobSecond, Worker worker, ServiceApi resource, Position workerNearestPosition, Position secondSourcePosition)
        {
            // 반환용 리스트 (현재는 구조상 제공, 실제 데이터는 필요 시 재조회 권장)
            var result = new List<Mission>();

            // ------------------------------------------------------------
            // [0] 기본 방어 코드
            // ------------------------------------------------------------
            if (jobSecond == null) return result;
            if (worker == null) return result;
            if (resource == null) return result;
            if (workerNearestPosition == null) return result;
            if (secondSourcePosition == null) return result;

            // ------------------------------------------------------------
            // [1] 시작/도착 Position 이 같다면:
            //     - 굳이 경로 계산 없이 PICK 그룹만 생성해도 된다.
            // ------------------------------------------------------------
            if (workerNearestPosition.positionId == secondSourcePosition.positionId)
            {
                int seqFrom = 1;

                // 바로 PICK 그룹만 생성 (필요 시 Elevator/Traffic 은 이후 구간에서 처리)
                // secondSourcePosition Pick위치
                seqFrom = create_GroupMission(jobSecond, secondSourcePosition, worker, seqFrom, nameof(MissionsTemplateGroup.PICK));

                // 실제 Mission 엔티티는 위 create_GroupMission 안에서 생성된다고 가정.
                // 필요하다면 여기서 _repository.Missions.GetByJobId(jobSecond.id) 로 다시 조회 가능.

                return result;
            }

            // ------------------------------------------------------------
            // [2] Routes_Plan 호출:
            //     WorkerNearestPosition → secondSourcePosition
            // ------------------------------------------------------------
            var routesPlanRequest = _mapping.RoutesPlanas.Request(workerNearestPosition.positionId, secondSourcePosition.positionId);

            var routesPlanResponse = resource.Api.Post_Routes_Plan_Async(routesPlanRequest).Result;

            if (routesPlanResponse == null) return result;
            if (routesPlanResponse.nodes == null) return result;

            // Mission sequence 시작 값 (일단 1부터 시작; 이후 전체 Queue 에서 재정렬할 예정)
            int seq = 1;

            // Elevator 중복 생성을 막기 위한 플래그
            bool elevatorMissionCreated = false;

            // ------------------------------------------------------------
            // [3] RoutesPlan 의 nodes 를 순회하며 Mission 생성
            // ------------------------------------------------------------
            foreach (var node in routesPlanResponse.nodes)
            {
                // Elevator 타입 노드는 position 이 없을 수도 있으므로,
                // 먼저 Position 조회를 시도하고, 없으면 nodeType 으로 분기 처리
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
                    // PICK 그룹 생성
                    if (position != null)
                    {
                        seq = create_GroupMission(jobSecond, position, worker, seq, nameof(MissionsTemplateGroup.PICK));
                    }
                    // PICK 까지만 필요하므로 나머지 노드는 없다고 가정하고, 루프 종료 가능
                    // break;  // 필요 시 사용
                }
                // --------------------------------------------------------
                // [3-2] Elevator 노드 처리 (필요 시)
                //       - WorkerNearest → B 출발지 사이에 Elevator 가 있을 수도 있다고 가정
                // --------------------------------------------------------
                else if (nodeTypeUpper == nameof(NodeType.ELEVATOR))
                {
                    if (!elevatorMissionCreated)
                    {
                        // Elevator 그룹은 Position 없이 생성 (템플릿에서 처리한다고 가정)
                        seq = create_GroupMission(jobSecond, null, worker, seq, nameof(MissionsTemplateGroup.ELEVATOR));
                        elevatorMissionCreated = true;
                    }
                    // 두 번째 이후 Elevator 노드는 무시 (이미 그룹 생성됨)
                }
                // --------------------------------------------------------
                // [3-3] TRAFFIC 노드 → TRAFFIC 그룹
                // --------------------------------------------------------
                else if (nodeTypeUpper == nameof(NodeType.TRAFFIC))
                {
                    if (position != null)
                    {
                        seq = create_GroupMission(jobSecond, position, worker, seq, nameof(MissionsTemplateGroup.TRAFFIC));
                    }
                }
                // --------------------------------------------------------
                // [3-4] 그 외 일반 노드 → MOVE(STOPOVERMOVE) 단일 미션
                // --------------------------------------------------------
                else
                {
                    if (position != null)
                    {
                        seq = create_SingleMission(jobSecond, position, worker, seq, nameof(MissionTemplateType.MOVE), nameof(MissionTemplateSubType.STOPOVERMOVE));
                    }
                }
            }

            // ------------------------------------------------------------
            // [4] 생성된 미션을 반환하고 싶다면, 여기서 다시 조회해서 채울 수 있다.
            //     (예: JobSecond 에 속하고, 아직 EXECUTING 이전 상태의 미션만 모아오기 등)
            //     지금은 구조 설명용으로 비워둔다.
            // ------------------------------------------------------------
            // 예시)
            // result = _repository.Missions
            //     .GetByJobId(jobSecond.id)
            //     .Where(m => m.assignedWorkerId == worker.id)
            //     .OrderBy(m => m.sequence)
            //     .ToList();

            return result;
        }

        /// <summary>
        /// BuildSecondFinalMissions
        /// ------------------------------------------------------------
        /// Second_FinalMission 구간을 생성한다.
        ///
        /// 목적:
        ///   - JobFirst 의 최종 목적지(firstDestination)에서
        ///     JobSecond 의 최종 목적지(secondDestinationPosition)까지
        ///     이동 경로를 계산하고, 그에 따른 Mission 들을 생성한다.
        ///
        /// 경로(패스 플랜) 규칙:
        ///   - 시작: firstDestination.positionId
        ///   - 도착: secondDestinationPosition.positionId
        ///
        /// 미션 생성 규칙:
        ///   - 중간 노드:
        ///       → MOVE(STOPOVERMOVE) 또는 TRAFFIC/ELEVATOR 그룹 (NodeType 에 따라)
        ///   - 마지막 노드(= secondDestinationPosition):
        ///       → MissionsTemplateGroup.DROP 그룹 생성
        ///
        /// 주의:
        ///   - 실제 Mission 엔티티 생성 / DB Insert 는
        ///     create_SingleMission / create_GroupMission 내부에서 수행한다고 가정.
        ///   - 이 함수의 반환 List<Mission> 은 선택적으로 활용 가능하며,
        ///     필요 시 _repository.Missions 에서 JobSecond 기준으로 다시 조회해 사용하는 것을 추천.
        /// </summary>
        private List<Mission> BuildSecondFinalMissions(Job jobSecond, Worker worker, ServiceApi resource, Position firstDestination, Position secondDestinationPosition)
        {
            // 반환용 리스트 (구조 상 제공. 실제 데이터는 필요 시 재조회 권장)
            var result = new List<Mission>();

            // ------------------------------------------------------------
            // [0] 기본 방어 코드
            // ------------------------------------------------------------
            if (jobSecond == null) return result;
            if (worker == null) return result;
            if (resource == null) return result;
            if (firstDestination == null) return result;
            if (secondDestinationPosition == null) return result;

            // ------------------------------------------------------------
            // [1] 시작/도착 Position 이 같다면:
            //     - 굳이 경로계산 없이 DROP 그룹만 생성해도 된다.
            // ------------------------------------------------------------
            if (firstDestination.positionId == secondDestinationPosition.positionId)
            {
                int seqFrom = 1;

                // 같은 위치라면 바로 DROP 그룹 생성
                seqFrom = create_GroupMission(jobSecond, secondDestinationPosition, worker, seqFrom, nameof(MissionsTemplateGroup.DROP));
                return result;
            }

            // ------------------------------------------------------------
            // [2] Routes_Plan 호출:
            //     firstDestination → secondDestinationPosition
            // ------------------------------------------------------------
            var routesPlanRequest = _mapping.RoutesPlanas.Request(firstDestination.positionId, secondDestinationPosition.positionId);

            var routesPlanResponse = resource.Api.Post_Routes_Plan_Async(routesPlanRequest).Result;

            if (routesPlanResponse == null) return result;
            if (routesPlanResponse.nodes == null) return result;

            // Mission sequence 시작 값
            int seq = 1;

            // Elevator 그룹이 여러 번 생기는 것을 막기 위한 플래그
            bool elevatorMissionCreated = false;

            // ------------------------------------------------------------
            // [3] RoutesPlan 의 nodes 를 순회하며 Mission 생성
            // ------------------------------------------------------------
            foreach (var node in routesPlanResponse.nodes)
            {
                // positionId 가 있는 노드는 Position 조회
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
                        // B 의 최종 목적지에서 DROP 그룹 생성
                        seq = create_GroupMission(jobSecond, position, worker, seq, nameof(MissionsTemplateGroup.DROP));
                    }

                    // DROP 까지 생성했으므로 이후 노드는 없다고 가정하고 종료해도 됨
                    // break;   // 필요 시 사용
                }
                // --------------------------------------------------------
                // [3-2] Elevator 노드 → Elevator 그룹 (최대 1번)
                // --------------------------------------------------------
                else if (nodeTypeUpper == nameof(NodeType.ELEVATOR))
                {
                    if (!elevatorMissionCreated)
                    {
                        // Elevator 그룹은 Position 없이 생성한다고 가정
                        seq = create_GroupMission(jobSecond, null, worker, seq, nameof(MissionsTemplateGroup.ELEVATOR));
                        elevatorMissionCreated = true;
                    }
                }
                // --------------------------------------------------------
                // [3-3] TRAFFIC 노드 → TRAFFIC 그룹
                // --------------------------------------------------------
                else if (nodeTypeUpper == nameof(NodeType.TRAFFIC))
                {
                    if (position != null)
                    {
                        seq = create_GroupMission(jobSecond, position, worker, seq, nameof(MissionsTemplateGroup.TRAFFIC));
                    }
                }
                // --------------------------------------------------------
                // [3-4] 그 외 일반 노드 → MOVE(STOPOVERMOVE) 단일 미션
                // --------------------------------------------------------
                else
                {
                    if (position != null)
                    {
                        seq = create_SingleMission(jobSecond, position, worker, seq, nameof(MissionTemplateType.MOVE), nameof(MissionTemplateSubType.STOPOVERMOVE));
                    }
                }
            }

            // ------------------------------------------------------------
            // [4] 필요하다면 여기서 실제로 생성된 미션을 다시 조회하여
            //     result 리스트를 채울 수 있다.
            // ------------------------------------------------------------
            // 예시)
            // result = _repository.Missions
            //     .GetByJobId(jobSecond.id)
            //     .Where(m => m.assignedWorkerId == worker.id)
            //     .OrderBy(m => m.sequence)
            //     .ToList();

            return result;
        }
    }
}