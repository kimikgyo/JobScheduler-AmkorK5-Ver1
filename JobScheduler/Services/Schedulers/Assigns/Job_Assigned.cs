using Common.Models.Bases;
using Common.Models.Jobs;
using Common.Models.Settings;

namespace JOB.Services
{
    public partial class SchedulerService
    {
        /// <summary>
        /// [진입점] 워커 할당 메인 함수
        /// - JobAssigned_Normal() 호출 → 미할당 Job을 워커에게 할당
        /// </summary>
        private void WorkerAssined()
        {
            JobAssigned_Normal();
            //JobReassigned();
        }

        /// <summary>
        /// JobAssigned_Normal
        /// ============================================================
        /// [목적]
        /// 미할당(Unassigned) Job 목록을 조회해서
        /// 활성 워커에게 거리/우선순위 기반으로 할당한다.
        /// 
        /// [처리 흐름]
        /// 1) UnAssigned Job 목록 조회 (priority 내림차순 + createdAt 오름차순)
        /// 2) 엘리베이터 상태에 따라 EV 관련 Job 필터링
        /// 3) 거리 기반으로 Subscribe_Worker 에 Job 어사인 시도 (distance 방식)
        /// ============================================================
        /// </summary>
        private void JobAssigned_Normal()
        {
            // [LOG] 노멀 어사인 시작
            //EventLogger.Info("[ASSIGN][NORMAL][START] JobAssigned_Normal called.");

            // ============================================================
            // [단계 1] UnAssigned Job 목록 조회
            // ============================================================
            // 설명: DB에서 아직 워커에게 할당되지 않은 모든 Job을 조회
            // 정렬 순서:
            //   - 우선순위(priority) 높은 것부터 (내림차순)
            //   - 같은 우선순위면 먼저 생성된 것부터 (오름차순)
            // 예시: priority=10인 Job이 priority=5인 Job보다 먼저 처리됨
            // 우선순위 높은 것 우선
            // 생성 시간 오래된 것 우선
            var unAssignedWorkerJobs = _repository.Jobs.UnAssignedJobs().OrderByDescending(r => r.priority).ThenBy(r => r.createdAt).ToList();

            // [방어 조건 1] Job이 하나도 없으면 처리할 것이 없으므로 즉시 반환
            if (unAssignedWorkerJobs == null || unAssignedWorkerJobs.Count == 0)
            {
                //EventLogger.Info("[ASSIGN][NORMAL][NO-JOB] Unassigned jobs not found.");
                return;
            }

            // ============================================================
            // [단계 2] 배터리 설정 조회
            // ============================================================
            // 설명: 워커의 배터리 상태 판단용 기준값(최소 배터리 %)을 조회
            // 용도: WorkerCondition() 메서드에서 "이 워커가 이 Job을 할 수 있는가?" 판단 시 사용
            var batterySetting = _repository.Battery.GetAll();

            // [방어 조건 2] 배터리 설정이 없으면 워커 할당 불가 (안전상 이유)
            if (batterySetting == null)
            {
                EventLogger.Warn("[ASSIGN][NORMAL][ABORT] Battery setting not found.");
                return;
            }

            // ============================================================
            // [단계 3] 엘리베이터 상태 확인 및 층간 이동 Job 필터링
            // ============================================================
            // 설명: 
            // - 엘리베이터가 "사용 불가 상태"일 때는 층간 이동(cross-floor) Job을 처리하면 안 됨
            // - 따라서 사용 불가 상태 감지 → 같은 층(same-floor) Job만 남김
            //
            // 사용 불가 엘리베이터 상태:
            //   * mode == NOTAGVMODE          : 엘리베이터가 AGV 모드가 아님
            //   * mode == AGVMODE_CHANGING    : AGV 모드로 전환 중
            //   * state == PROTOCOLERROR      : 통신 오류
            //   * state == DISCONNECT         : 연결 끊김
            // ============================================================
            //var elevators = _repository.Elevator.GetAll();

            //// [조건 검사] 엘리베이터 목록이 있는 경우만 진행
            //if (elevators != null && elevators.Count > 0)
            //{
            //    // [Step 3-1] 사용 불가 엘리베이터 여부 확인
            //    // 설명: 전체 엘리베이터를 순회해서 하나라도 사용 불가 상태면 플래그 설정
            //    // 하나 발견됐으면 더 볼 필요 없음

            //    var notAGVMode = elevators.FirstOrDefault(r => r.mode == nameof(ElevatorMode.NOTAGVMODE)
            //                                            || r.mode == nameof(ElevatorMode.AGVMODE_CHANGING_NOTAGVMODE)
            //                                            || r.state == nameof(ElevatorState.PROTOCOLERROR)
            //                                            || r.state == nameof(ElevatorState.DISCONNECT));

            //    // [Step 3-2] 사용 불가 엘리베이터가 있으면 Job 필터링
            //    if(notAGVMode!=null)
            //    {
            //        // 설명: 미할당 Job을 두 그룹으로 분류
            //        // - sameFloorJobs: 같은 층 이동 Job (처리 가능)
            //        // - crossFloorJobs: 층간 이동 Job (처리 불가)
            //        var crossFloorJobs = new List<Job>();
            //        var sameFloorJobs = new List<Job>();

            //        foreach (var job in unAssignedWorkerJobs)
            //        {
            //            // IsSameFloorJob() 메서드: Job의 출발지와 목적지가 같은 층인지 판단
            //            // true = 같은 층 → sameFloorJobs에 추가
            //            // false = 다른 층 → crossFloorJobs에 추가
            //            if (IsSameFloorJob(job))
            //            {
            //                sameFloorJobs.Add(job);
            //            }
            //            else
            //            {
            //                crossFloorJobs.Add(job);
            //            }
            //        }

            //        // 필터링 결과로 unAssignedWorkerJobs 업데이트
            //        // (= 이제부터는 같은 층 Job만 처리함)
            //        unAssignedWorkerJobs = sameFloorJobs;

            //        // [로그] 필터링 결과 기록 (디버깅용)
            //        if (crossFloorJobs.Count > 0)
            //        {
            //            EventLogger.Info($"[ASSIGN][FILTER][ELEVATOR-INACTIVE] Removed {crossFloorJobs.Count} cross-floor jobs.Remaining={unAssignedWorkerJobs.Count}");
            //        }
            //    }
            //}

            // [최종 방어 조건] 필터링 후에도 처리할 Job이 없으면 종료
            if (unAssignedWorkerJobs == null || unAssignedWorkerJobs.Count == 0)
            {
                //EventLogger.Info("[ASSIGN][NORMAL][NO-JOB] After EV filter, no assignable jobs.");
                return;
            }

            // ============================================================
            // [단계 4] 거리 기반 어사인 로직 수행
            // ============================================================
            // 설명: 남은 Job들을 워커에게 할당
            // 방식: distance() 메서드 호출 → 워커와 Job 간 거리 기반으로 최적 매칭
            distance(unAssignedWorkerJobs, batterySetting);

            // [LOG] 노멀 어사인 종료
            //EventLogger.Info("[ASSIGN][NORMAL][DONE] JobAssigned_Normal finished.");
        }

        /// <summary>
        /// IsSameFloorJob 메서드
        /// ============================================================
        /// [목적]
        /// 주어진 Job의 출발지와 목적지가 같은 층(Map)에 있는지 판단
        /// 
        /// [반환값]
        /// true  : 같은 층 → 엘리베이터 불필요
        /// false : 다른 층 → 엘리베이터 필요 (또는 Position 정보 부족)
        /// ============================================================
        /// </summary>
        private bool IsSameFloorJob(Job job)
        {
            bool reValue = false;

            // [Step 1] Job의 출발지 Position 조회
            // job.sourceId는 Position의 고유 ID (예: "POS001")
            var src = _repository.Positions.GetById(job.sourceId);

            // [Step 2] Job의 목적지 Position 조회
            var dst = _repository.Positions.GetById(job.destinationId);

            // [조건 1] Position 정보가 없으면 판단 불가 → false 반환
            // → 안전상 이런 Job은 필터링됨 (크로스플로어로 취급)
            if (src == null || dst == null)
            {
                reValue = false;
            }
            // [조건 2] 두 Position의 mapId가 같으면 같은 층 → true 반환
            // mapId: 층 정보 (예: "FLOOR_1", "FLOOR_2")
            else if (src.mapId == dst.mapId)
            {
                reValue = true;
            }

            return reValue;
        }

        /// <summary>
        /// firstJob 메서드 (현재 미사용 - 주석 참고)
        /// ============================================================
        /// [목적]
        /// UnAssignedWorkerJobs 목록에서 각 워커에게 1개씩 순차적으로 Job 할당
        /// (현재는 distance() 메서드로 대체됨)
        ///
        /// [규칙]
        ///   1) 워커는 Active + IDLE 상태만 대상
        ///   2) 이미 Job 이 할당된 워커는 건너뜀
        ///   3) Job 은 워커의 group 과 동일한 Job 중에서 선택
        ///      3-1) 우선: 지정 워커 Job (specifiedWorkerId == worker.id)
        ///      3-2) 없다면: 미지정 Job (specifiedWorkerId 비어있는 Job)
        ///   4) WorkerCondition / ChangeWaitDeleteMission 통과 시 Create_Mission 실행
        /// ============================================================
        /// </summary>
        private void firstJob(List<Job> unAssignedWorkerJobs, Battery batterySetting)
        {
            // [방어 조건] 입력 파라미터 유효성 검사
            if (unAssignedWorkerJobs == null || unAssignedWorkerJobs.Count == 0)
                return;

            if (batterySetting == null)
                return;

            // [Step 1] 활성 상태의 모든 워커 조회
            // Active = 연결된 상태 (Offline/Idle/Working 등 상태 무관)
            var workers = _repository.Workers.MiR_GetByActive();
            if (workers == null || workers.Count == 0)
                return;

            // [Step 2] 워커 순회: 각 워커에게 1개씩 Job 할당 시도
            foreach (var worker in workers)
            {
                // [조건 1] 워커가 IDLE(대기) 상태가 아니면 다음 워커로 넘어감
                // (이미 다른 Job 수행 중인 워커는 제외)
                if (worker.state != nameof(WorkerState.IDLE))
                    continue;

                // [조건 2] 이 워커에게 이미 할당된 Job이 있으면 건너뜀
                var runJob = _repository.Jobs.GetByAssignWorkerId(worker.id).FirstOrDefault();

                if (runJob != null)
                    continue;

                // [Step 3] 이 워커의 group과 동일한 미할당 Job만 필터링
                // group: "A", "B" 등 워커 그룹 분류
                var jobsByGroup = unAssignedWorkerJobs.Where(u => u.group == worker.group).ToList();

                if (jobsByGroup == null || jobsByGroup.Count == 0)
                    continue;

                Job job = null;

                // [Step 4] 지정 워커 Job 우선 선택
                // specifiedWorkerId가 이 워커 ID와 일치하는 Job 찾기
                job = jobsByGroup.FirstOrDefault(j => j.specifiedWorkerId == worker.id);

                // [Step 5] 지정 워커 Job이 없으면 미지정 Job 선택
                // specifiedWorkerId가 비어있는 Job (누구든 할 수 있는 Job)
                if (job == null)
                {
                    job = jobsByGroup.FirstOrDefault(j => IsInvalid(j.specifiedWorkerId));
                }

                // [Step 6] 그래도 Job이 없으면 이 워커는 할당할 Job이 없음
                if (job == null)
                    continue;

                // [Step 7] 워커가 이 Job을 수행할 수 있는지 조건 검사
                // - 배터리 충분한가?
                // - 미들웨어 사용 중이면 Idle 상태인가?
                // - 자재 관련 조건 만족하는가?
                if (!WorkerCondition(job, worker, batterySetting))
                    continue;

                // [Step 8] 기존 WAIT/CHARGE 미션 정리
                // (새 Job을 시작하기 전에 대기/충전 미션 제거)
                if (job.subType != nameof(JobSubType.WAIT) && job.subType != nameof(JobSubType.CHARGE))
                {
                    ChangeWaitDeleteJob(worker, "[ASSIGN][NORMAL][FIRSTJOB][ASSIGNED]");
                }

                // [Step 9] 최종 Job 할당 및 미션 생성 시도
                if (Create_Mission(job, worker))
                {
                    // 성공 로그
                    EventLogger.Info(
                        $"[ASSIGN][NORMAL][FIRSTJOB][ASSIGNED] " +
                        $"workerId={worker.id}, workerName={worker.name}, " +
                        $"jobId={job.guid}, jobType={job.type}, group={job.group}"
                    );

                    // [Step 10] 이 Job은 더 이상 미할당이 아니므로 리스트에서 제거
                    unAssignedWorkerJobs.Remove(job);
                }
                else
                {
                    // 실패 처리: Job 상태를 TERMINATED(취소) 처리
                    jobTerminateState_Change_Inited(job, "[ASSIGN][NORMAL][FIRSTJOB][NOTASSIGNED]");
                }
            }
        }

        /// <summary>
        /// distance 메서드
        /// ============================================================
        /// [목적]
        /// 미할당 Job을 활성 워커에게 거리 기반으로 매칭해서 할당
        ///
        /// [처리 방식]
        /// 1) Active Subscribe_Worker 조회
        /// 2) 이미 Job 수행 중인 워커 제외
        /// 3) Job을 지정 워커 / 비지정 워커로 분리:
        ///    - 지정 워커 Job : 워커 기준 가장 가까운 Job 선택
        ///    - 비지정 Job    : Job 기준 가장 가까운 워커 선택
        /// ============================================================
        /// </summary>
        private void distance(List<Job> unAssignedWorkerJobs, Battery batterySetting)
        {
            // ============================================================
            // [방어 단계] 입력 파라미터 유효성 검사
            // ============================================================
            if (unAssignedWorkerJobs == null || unAssignedWorkerJobs.Count == 0)
                return;
            if (batterySetting == null)
                return;

            // ============================================================
            // [단계 1] 활성 워커 목록 조회
            // ============================================================
            // Active = 온라인 상태 (IDLE, WORKING, CHARGING 등 모든 상태 포함)
            var workers = _repository.Workers.MiR_GetByActive();
            if (workers == null || workers.Count == 0)
            {
                //EventLogger.Info("[ASSIGN][NORMAL][DISTANCE], No active workers.");
                return;
            }

            // [선택사항] IDLE 상태만 필터링 (현재 주석 처리됨)
            // 필요시 아래 코드 활성화:
            //// IDLE 상태인 워커만 필터링
            //var idleWorkers = workers.Where(r => r.state == nameof(WorkerState.IDLE)).ToList();
            //
            //if (idleWorkers == null || idleWorkers.Count == 0)
            //{
            //    EventLogger.Info("[ASSIGN][NORMAL][DISTANCE], No IDLE workers.");
            //    return;
            //}
            //
            //// 작업 대상 워커 리스트는 idleWorkers 기준으로 사용
            //workers = idleWorkers;

            // ============================================================
            // [단계 2] 이미 Job 수행 중인 워커 제외
            // ============================================================
            // 설명: 
            // - CHARGE/WAIT Job은 제외 (이들은 언제든 중단 가능)
            // - TRANSPORT 등 실제 작업 중인 Job은 해당 워커 제외
            var runningJobs = _repository.Jobs.GetAll()
                .Where(r => r.assignedWorkerId != null)  // 할당된 Job만
                .ToList();

            foreach (var runJob in runningJobs)
            {
                // [조회] 이 Job을 수행 중인 워커 찾기
                var runJobWorker = workers.FirstOrDefault(r => r.id == runJob.assignedWorkerId);

                // [조건] 
                // - 워커가 존재하고 (runJobWorker != null)
                // - 해당 Job이 WAIT/CHARGE가 아닌 경우 (= 실제 작업 중)
                if (runJobWorker != null&& runJob.type != nameof(JobType.WAIT)&& runJob.type != nameof(JobType.CHARGE))
                {
                    // 이 워커는 이미 다른 작업 중이므로 할당 대상에서 제외
                    workers.Remove(runJobWorker);
                }
            }

            // [체크] 제외 후 남은 워커가 없으면 종료
            if (workers == null || workers.Count == 0)
            {
                //EventLogger.Info("[ASSIGN][NORMAL][DISTANCE], All workers are busy.");
                return;
            }

            // ============================================================
            // [단계 3] Job을 지정 워커 / 비지정 워커로 분리
            // ============================================================
            // 설명:
            // - findSpecifiedWorkerJobs: specifiedWorkerId가 설정된 Job (특정 워커 전용)
            // - findNotSpecifiedWorkerJobs: specifiedWorkerId가 비어있는 Job (누구든 가능)

            // [조회 1] 지정 워커 Job (IsInvalid()=false → 값이 있음)
            var findSpecifiedWorkerJobs = unAssignedWorkerJobs.Where(j => IsInvalid(j.specifiedWorkerId) == false).ToList();

            // [조회 2] 비지정 워커 Job (IsInvalid()=true → 값이 없음)
            var findNotSpecifiedWorkerJobs = unAssignedWorkerJobs.Where(j => IsInvalid(j.specifiedWorkerId) == true).ToList();

            // ============================================================
            // [단계 3-1] 지정 워커 Job 처리
            // ============================================================
            // 처리 방식: 워커 중심
            // - 각 워커에 대해 "이 워커에게 지정된 Job" 찾기
            // - 여러 개면 워커 위치 기준 가장 가까운 Job 선택
            if (findSpecifiedWorkerJobs != null && findSpecifiedWorkerJobs.Count > 0)
            {
                // [추적용] 이 루프에서 할당된 워커 목록
                // (나중에 비지정 Job에서 제외하기 위함)
                var assignedWorkers = new List<Worker>();

                foreach (var worker in workers)
                {
                    // [Step 1] 이 워커에게 지정된 Job 목록 추출
                    // specifiedWorkerId == worker.id 인 Job만
                    var specifiedWorkerJobs = findSpecifiedWorkerJobs.Where(r => r.specifiedWorkerId == worker.id).ToList();

                    if (specifiedWorkerJobs == null || specifiedWorkerJobs.Count == 0)
                        continue;

                    // [Step 2] 여러 개면 워커 위치 기준 가장 가까운 Job 선택
                    var job = SelectNearestJobForWorker(worker, specifiedWorkerJobs);

                    if (job == null)
                        continue;

                    // [Step 3] 워커가 이 Job을 수행할 수 있는지 조건 검사
                    if (!WorkerCondition(job, worker, batterySetting))
                        continue;

                    // [Step 4] 기존 WAIT 미션 정리
                    // (새 작업 시작 전에 대기/충전 미션 제거)
                    if (job.subType != nameof(JobSubType.WAIT) && job.subType != nameof(JobSubType.CHARGE))
                    {
                        ChangeWaitDeleteJob(worker, "[ASSIGN][NORMAL][DISTANCE][SPECIFIED][ASSIGNED]");
                    }

                    // [Step 5] 최종 Job 할당 및 미션 생성 시도
                    if (Create_Mission(job, worker))
                    {
                        // [성공 로그]
                        EventLogger.Info(
                            $"[ASSIGN][NORMAL][DISTANCE][SPECIFIED][ASSIGNED], " +
                            $"workerId={worker.id}, workerName={worker.name}, " +
                            $"jobName={job.name}, jobId={job.guid}, " +
                            $"jobType={job.type}, group={job.group}"
                        );

                        // [추적] 이 워커는 이제 할당됐으므로 리스트에 추가
                        assignedWorkers.Add(worker);
                    }
                    else
                    {
                        // [실패 처리] Job 상태를 TERMINATED(취소) 처리
                        jobTerminateState_Change_Inited(job,"[ASSIGN][NORMAL][DISTANCE][SPECIFIED][NOTASSIGNED]");
                    }
                }

                // [정리] 이미 할당된 워커는 비지정 Job 처리에서 제외
                foreach (var assignedWorker in assignedWorkers)
                {
                    workers.Remove(assignedWorker);
                }
            }

            // [체크] 지정 워커 Job 처리 후 남은 워커가 없으면 종료
            if (workers == null || workers.Count == 0)
            {
                //EventLogger.Info("[ASSIGN][NORMAL][DISTANCE], No workers left after specified jobs.");
                return;
            }

            // ============================================================
            // [단계 3-2] 비지정 워커 Job 처리
            // ============================================================
            // 처리 방식: Job 중심
            // - 각 Job에 대해 "가장 가까운 워커" 찾기
            // - group이 맞는 워커만 후보로 고려
            if (findNotSpecifiedWorkerJobs != null && findNotSpecifiedWorkerJobs.Count > 0)
            {
                foreach (var job in findNotSpecifiedWorkerJobs)
                {
                    // [Step 1] 이 Job의 group에 맞는 워커만 필터링
                    // (Job의 group == 워커의 group)
                    var candidates = workers.Where(w => w.group == job.group).ToList();

                    if (candidates == null || candidates.Count == 0)
                        continue;

                    // [Step 2] 후보 워커 중 가장 가까운 워커 선택
                    // SelectNearestWorkerForJob():
                    // - Job의 출발지/목적지 기준 거리 계산
                    // - 최소 거리 워커 반환
                    var worker = SelectNearestWorkerForJob(candidates, job);
                    if (worker == null)
                        continue;

                    // [Step 3] 워커가 이 Job을 수행할 수 있는지 조건 검사
                    if (!WorkerCondition(job, worker, batterySetting))
                        continue;

                    // [Step 4] 기존 WAIT 미션 정리
                    if (job.subType != nameof(JobSubType.WAIT) && job.subType != nameof(JobSubType.CHARGE))
                    {
                        ChangeWaitDeleteJob(worker, "[ASSIGN][NORMAL][DISTANCE][UNSPECIFIED][ASSIGNED]");
                    }


                    // [Step 5] 최종 Job 할당 및 미션 생성 시도
                    if (Create_Mission(job, worker))
                    {
                        // [성공 로그]
                        EventLogger.Info(
                            $"[ASSIGN][NORMAL][DISTANCE][UNSPECIFIED][ASSIGNED], " +
                            $"workerId={worker.id}, workerName={worker.name}, " +
                            $"jobId={job.guid}, jobType={job.type}, " +
                            $"group={job.group}"
                        );
                    }
                    else
                    {
                        // [실패 처리] Job 상태를 TERMINATED(취소) 처리
                        jobTerminateState_Change_Inited(job,"[ASSIGN][NORMAL][DISTANCE][UNSPECIFIED][NOTASSIGNED]");
                    }
                }
            }
        }

        /// <summary>
        /// ChangeWaitDeleteJob 메서드
        /// ============================================================
        /// [목적]
        /// 워커가 현재 수행 중인 WAIT 또는 CHARGE Job이 있으면
        /// 새 Job 할당 전에 이를 취소(TERMINATE) 처리한다.
        ///
        /// [이유]
        /// - WAIT: "대기 위치에서 대기"하는 미션 (새 Job 있으면 즉시 취소)
        /// - CHARGE: "충전소에서 충전"하는 미션 (새 Job 있으면 즉시 취소)
        /// - 이 두 Job은 "우선순위 낮은 유지 작업"이므로 새 작업이 있으면 취소 OK
        /// ============================================================
        /// </summary>
        private bool ChangeWaitDeleteJob(Worker worker, string message)
        {
            bool reValue = false;

            // [조회] 이 워커의 미완료 Job 중:
            // - 상태가 WORKERASSIGNED 또는 INPROGRESS (= 할당되었거나 진행 중)
            // - 타입이 WAIT 또는 CHARGE (= 대기/충전)
            // - 아직 terminate되지 않음 (terminateState == null)
            //// 아직 취소/완료되지 않음
            var runjob = _repository.Jobs.GetByWorkerId(worker.id)
                .FirstOrDefault(r =>r.terminateState == null && (r.state == nameof(JobState.WORKERASSIGNED)|| r.state == nameof(JobState.INPROGRESS))
                    && (r.type == nameof(JobType.WAIT) || r.type == nameof(JobType.CHARGE))
                );

            // [조건] 해당 Job이 있으면 취소 처리
            if (runjob != null)
            {
                // jobTerminateState_Change_Inited():
                // Job의 상태를 다음처럼 변경:
                // - terminateState = "INITED" (취소 준비 중)
                // - terminator = 취소한 엔티티 정보
                // - terminatingAt = 취소 시간
                jobTerminateState_Change_Inited(runjob, message);

                reValue = true;
            }
            return reValue;
        }

        /// <summary>
        /// SelectNearestWorkerForJob 메서드
        /// ============================================================
        /// [목적]
        /// 주어진 Job에 대해 "가장 가까운 워커 1명"을 선택
        /// (Job 중심 접근: Job의 출발지/목적지 기준으로 거리 계산)
        ///
        /// [입력]
        /// - workers: 후보 워커 리스트 (같은 group 등 사전 필터링 완료)
        /// - job: 할당할 대상 Job
        ///
        /// [반환]
        /// - 선택된 워커 1명 (없으면 null)
        /// ============================================================
        /// </summary>
        private Worker SelectNearestWorkerForJob(List<Worker> workers, Job job)
        {
            // ============================================================
            // [방어 단계] 입력 파라미터 유효성 검사
            // ============================================================
            if (job == null)
            {
                EventLogger.Warn("[ASSIGN][NEAREST-WORKER][SKIP] job is null.");
                return null;
            }

            if (workers == null)
            {
                EventLogger.Warn(
                    $"[ASSIGN][NEAREST-WORKER][SKIP] workers list is null. jobId={job.guid}"
                );
                return null;
            }

            if (workers.Count == 0)
            {
                EventLogger.Info(
                    $"[ASSIGN][NEAREST-WORKER][SKIP] workers list is empty. jobId={job.guid}"
                );
                return null;
            }

            // [로그] 메서드 시작
            EventLogger.Info(
                $"[ASSIGN][NEAREST-WORKER][START] " +
                $"jobId={job.guid}, workerCount={workers.Count}, " +
                $"sourceId={job.sourceId}, destinationId={job.destinationId}"
            );

            // ============================================================
            // [단계 1] Job 기준 위치(Position) 선택
            // ============================================================
            // 설명:
            // - Job의 출발지 또는 목적지 Position을 기준으로 거리 계산
            // - 우선순위: sourceId (있으면) > destinationId
            Position position = null;

            // [조건 1] Job의 sourceId가 없으면 destinationId 기준
            if (IsInvalid(job.sourceId))
            {
                // 출발지 없음 → 목적지로 기준 위치 설정
                position = _repository.Positions.MiR_GetById(job.destinationId);
                if (position != null)
                {
                    EventLogger.Info(
                        $"[ASSIGN][NEAREST-WORKER][POSITION] " +
                        $"use DESTINATION as base. " +
                        $"jobId={job.guid}, destId={job.destinationId}, " +
                        $"positionName={position.name}, positionId={position.id}"
                    );
                }
            }
            // [조건 2] Job의 sourceId가 있으면 sourceId 기준
            else
            {
                position = _repository.Positions.MiR_GetById(job.sourceId);
                if (position != null)
                {
                    EventLogger.Info(
                        $"[ASSIGN][NEAREST-WORKER][POSITION] " +
                        $"use SOURCE as base. " +
                        $"jobId={job.guid}, sourceId={job.sourceId}, " +
                        $"positionName={position.name}, positionId={position.id}"
                    );
                }
            }

            // [체크] 기준 Position이 없으면 거리 계산 불가 → 워커 선택 불가
            if (position == null)
            {
                EventLogger.Warn(
                    $"[ASSIGN][NEAREST-WORKER][SKIP][POSITION-NOTFOUND] " +
                    $"jobId={job.guid}, sourceId={job.sourceId}, " +
                    $"destinationId={job.destinationId}"
                );
                return null;
            }

            // ============================================================
            // [단계 2] Position 기준 가장 가까운 워커 선택
            // ============================================================
            // 설명:
            // FindNearestWorker() 메서드:
            // - 모든 후보 워커와 기준 Position 간 거리 계산
            // - 거리 순으로 정렬된 워커 리스트 반환
            // - FirstOrDefault(): 가장 가까운 워커 1명 추출
            Worker selectedWorker = null;

            var nearestWorker = _repository.Workers.FindNearestWorker(workers, position).FirstOrDefault();

            if (nearestWorker != null)
            {
                selectedWorker = nearestWorker;
                EventLogger.Info(
                    $"[ASSIGN][NEAREST-WORKER][SELECT] " +
                    $"jobId={job.guid}, " +
                    $"workerId={selectedWorker.id}, workerName={selectedWorker.name}, " +
                    $"basePositionName={position.name}, basePositionId={position.id}"
                );
            }
            else
            {
                EventLogger.Info(
                    $"[ASSIGN][NEAREST-WORKER][SKIP][NO-NEAREST] " +
                    $"jobId={job.guid}, " +
                    $"basePositionName={position.name}, basePositionId={position.id}"
                );
            }

            return selectedWorker;
        }

        /// <summary>
        /// SelectNearestJobForWorker 메서드
        /// ============================================================
        /// [목적]
        /// 주어진 워커에 대해 "가장 가까운 Job 1개"를 선택
        /// (워커 중심 접근: 워커의 현재 위치 기준으로 거리 계산)
        ///
        /// [입력]
        /// - worker: 대상 워커
        /// - jobs: 후보 Job 리스트
        ///
        /// [반환]
        /// - 선택된 Job 1개 (없으면 null)
        /// ============================================================
        /// </summary>
        private Job SelectNearestJobForWorker(Worker worker, List<Job> jobs)
        {
            // ============================================================
            // [방어 단계] 입력 파라미터 유효성 검사
            // ============================================================
            if (worker == null)
            {
                EventLogger.Warn($"[ASSIGN][NEAREST-JOB][SKIP] worker is null.");
                return null;
            }

            if (jobs == null)
            {
                EventLogger.Warn(
                    $"[ASSIGN][NEAREST-JOB][SKIP] " +
                    $"jobs list is null. " +
                    $"workerName={worker.name}, workerId={worker.id}"
                );
                return null;
            }

            if (jobs.Count == 0)
            {
                EventLogger.Info(
                    $"[ASSIGN][NEAREST-JOB][SKIP] " +
                    $"jobs list is empty. " +
                    $"workerName={worker.name}, workerId={worker.id}"
                );
                return null;
            }

            //EventLogger.Info(
            //    $"[ASSIGN][NEAREST-JOB][START] " +
            //    $"workerName={worker.name}, workerId={worker.id}, jobCount={jobs.Count}"
            //);

            // ============================================================
            // [단계 1] 각 Job의 "기준 Position" 선택 및 매핑
            // ============================================================
            // 설명:
            // - 각 Job마다 거리 계산 기준이 될 Position 정하기
            // - Job.sourceId (있으면) > Job.destinationId
            // - 추후 매핑을 통해 "가장 가까운 Position"과 연결된 Job 찾기

            // [매핑 테이블] Job ID → 기준 Position
            var jobMainPositionMap = new Dictionary<string, Position>();

            // [후보 Position 목록] 거리 계산 대상
            var candidatePositions = new List<Position>();

            // [루프] 각 Job에 대해 기준 Position 결정
            foreach (var job in jobs)
            {
                Position mainPosition = null;

                // [조건 1] Job의 sourceId가 유효하면 sourceId 기준
                if (!IsInvalid(job.sourceId))
                {
                    mainPosition = _repository.Positions.MiR_GetById(job.sourceId);
                }
                // [조건 2] sourceId가 없으면 destinationId 기준
                else
                {
                    mainPosition = _repository.Positions.MiR_GetById(job.destinationId);
                }

                // [체크] Position 조회 실패 시 이 Job은 거리 후보에서 제외
                if (mainPosition == null)
                {
                    EventLogger.Warn(
                        $"[ASSIGN][NEAREST-JOB][SKIP-JOB-POSITION-NOTFOUND] " +
                        $"workerName={worker.name}, workerId={worker.id}, " +
                        $"jobId={job.guid}, sourceId={job.sourceId}, destId={job.destinationId}"
                    );
                    continue;
                }

                // [매핑] Job ID → 기준 Position 저장
                jobMainPositionMap[job.guid] = mainPosition;

                // [추가] 거리 계산 후보에 추가
                candidatePositions.Add(mainPosition);
            }

            // [체크] 후보 Position이 하나도 없으면 Job 선택 불가
            if (candidatePositions.Count == 0)
            {
                EventLogger.Info(
                    $"[ASSIGN][NEAREST-JOB][SKIP] " +
                    $"no candidate positions. " +
                    $"workerName={worker.name}, workerId={worker.id}"
                );
                return null;
            }

            //EventLogger.Info(
            //    $"[ASSIGN][NEAREST-JOB][CANDIDATE] " +
            //    $"workerName={worker.name}, workerId={worker.id}, " +
            //    $"candidatePositionCount={candidatePositions.Count}"
            //);

            // ============================================================
            // [단계 2] 워커 위치 기준 가장 가까운 Position 선택
            // ============================================================
            // 설명:
            // FindNearestWayPoint():
            // - 워커의 현재 위치(worker.position_X, worker.position_Y)에서
            // - candidatePositions까지의 거리 계산
            // - 거리 순으로 정렬된 Position 리스트 반환
            var nearestPosition = _repository.Positions
                .FindNearestWayPoint(worker, candidatePositions)
                .FirstOrDefault();

            if (nearestPosition == null)
            {
                EventLogger.Warn(
                    $"[ASSIGN][NEAREST-JOB][SKIP] " +
                    $"nearestPosition is null. " +
                    $"workerName={worker.name}, workerId={worker.id}"
                );
                return null;
            }

            //EventLogger.Info(
            //    $"[ASSIGN][NEAREST-JOB][NEAREST-POSITION] " +
            //    $"workerName={worker.name}, workerId={worker.id}, " +
            //    $"positionName={nearestPosition.name}, positionId={nearestPosition.id}"
            //);

            // ============================================================
            // [단계 3] "가장 가까운 Position"과 연결된 Job 찾기
            // ============================================================
            // 설명:
            // 방법 1 (우선): jobMainPositionMap을 이용한 직접 매핑
            // 방법 2 (대안): sourceId/destinationId 직접 비교 (매핑 실패 시)
            Job selectedJob = null;

            // [방법 1] 매핑 테이블을 통한 직접 매칭
            foreach (var pair in jobMainPositionMap)
            {
                var jobId = pair.Key;
                var positionInMap = pair.Value;

                // 매핑된 Position이 "가장 가까운 Position"과 일치하면 매칭
                if (positionInMap.id == nearestPosition.id)
                {
                    // 이 Job ID에 해당하는 Job 객체 찾기
                    foreach (var job in jobs)
                    {
                        if (job.guid == jobId)
                        {
                            selectedJob = job;
                            break;
                        }
                    }

                    if (selectedJob != null)
                    {
                        break;  // 찾았으므로 루프 종료
                    }
                }
            }

            // [방법 2-1] 방법 1 실패 시: sourceId로 다시 시도
            if (selectedJob == null)
            {
                foreach (var job in jobs)
                {
                    if (job.sourceId == nearestPosition.id)
                    {
                        selectedJob = job;
                        break;
                    }
                }
            }

            // [방법 2-2] 방법 2-1 실패 시: destinationId로 다시 시도
            if (selectedJob == null)
            {
                foreach (var job in jobs)
                {
                    if (job.destinationId == nearestPosition.id)
                    {
                        selectedJob = job;
                        break;
                    }
                }
            }

            // [최종 체크] 그래도 못 찾으면 선택 불가
            if (selectedJob == null)
            {
                EventLogger.Info(
                    $"[ASSIGN][NEAREST-JOB][SKIP][NO-MATCH-JOB] " +
                    $"workerName={worker.name}, workerId={worker.id}, " +
                    $"nearestPositionId={nearestPosition.id}"
                );
                return null;
            }

            // [성공 로그]
            EventLogger.Info(
                $"[ASSIGN][NEAREST-JOB][SELECT] " +
                $"workerName={worker.name}, workerId={worker.id}, " +
                $"jobName={selectedJob.name}, jobId={selectedJob.guid}, " +
                $"sourceId={selectedJob.sourceId}, destId={selectedJob.destinationId}"
            );

            return selectedJob;
        }

        /// <summary>
        /// WorkerCondition 메서드
        /// ============================================================
        /// [목적]
        /// 주어진 워커가 주어진 Job을 수행할 수 있는지
        /// 다양한 조건을 검사한다.
        ///
        /// [검사 항목]
        /// - 배터리: 최소 배터리 % 이상인가?
        /// - 미들웨어: 사용 중이면 IDLE 상태인가?
        /// - 자재: DROPONLY라면 자재가 없어야 하고, 그 외는 자재가 있어야 함
        /// ============================================================
        /// </summary>
        private bool WorkerCondition(Job job, Worker worker, Battery battery)
        {
            bool Condition = true;

            // [1단계] Job 타입별 조건 검사
            // (TRANSPORT 계열 Job들만 상세 검사, 나머지는 OK)
            switch (job.type)
            {
                case nameof(JobType.TRANSPORT):
                case nameof(JobType.MANUALTRANSPORT):
                case nameof(JobType.TRANSPORT_SLURRY_SUPPLY):
                case nameof(JobType.TRANSPORT_SLURRY_RECOVERY):
                case nameof(JobType.TRANSPORT_CHEMICAL_RECOVERY):
                case nameof(JobType.TRANSPORT_CHEMICAL_SUPPLY):

                    // [조건 1] 배터리 충전도 확인
                    // worker.batteryPercent < battery.minimum 이면 작업 불가
                    if (worker.batteryPercent < battery.minimum)
                    {
                        Condition = false;
                    }

                    // [조건 2] 미들웨어 상태 확인
                    // (미들웨어: 로봇에 부착된 추가 장비)
                    if (worker.isMiddleware == true)
                    {
                        var middleware = _repository.Middlewares.GetByWorkerId(worker.id);
                        if (middleware != null)
                        {
                            // [2-1] 미들웨어가 IDLE 상태여야 작업 가능
                            if (middleware.state != nameof(MiddlewareState.IDLE))
                            {
                                Condition = false;
                            }

                            // [2-2] 자재(Carrier) 상태 확인
                            var carrier = _repository.Carriers.GetByWorkerId(worker.id).FirstOrDefault();

                            // DROPONLY: 자재를 내려놓기만 함
                            // → 미리 자재가 있어야 함 (carrier != null이면 OK)
                            if (job.subType == nameof(JobSubType.DROPONLY) && carrier != null)
                            {
                                Condition = false;  // 이상 조건: DROPONLY인데 자재가 없음
                            }
                            // 그 외: PICK 또는 일반 TRANSPORT
                            // → 자재가 없어야 함 (carrier == null이어야 OK)
                            else if (carrier != null)
                            {
                                Condition = false;  // 이미 자재를 가지고 있으므로 새 작업 불가
                            }
                        }
                        else
                        {
                            // 미들웨어 객체가 없으면 상태 확인 불가 → 작업 불가
                            Condition = false;
                        }
                    }
                    break;
                    // [기타 타입]
                    // CHARGE, WAIT 등 다른 Job 타입은 특별한 조건 없음 → Condition = true 유지
            }

            return Condition;
        }

        /// <summary>
        /// template_SingleMission 메서드
        /// ============================================================
        /// [목적]
        /// 단일 타입의 미션 템플릿을 조회해서 미션 생성 큐에 추가
        ///
        /// [입력]
        /// - job: 대상 Job
        /// - position: 미션이 일어날 Position
        /// - worker: 미션을 수행할 워커
        /// - seq: 현재 시퀀스 번호 (다음 미션의 시작 번호)
        /// - type: 미션 타입 (예: "MOVE")
        /// - subtype: 미션 서브타입 (예: "STOPOVERMOVE")
        ///
        /// [반환]
        /// - 다음 시퀀스 번호 (seq + 1)
        /// ============================================================
        /// </summary>
        private int template_SingleMission(
            Job job,
            Position position,
            Worker worker,
            int seq,
            string type,
            string subtype
        )
        {
            lock (_lock)
            {
                // [Step 1] 해당 type/subtype 미션 템플릿 조회
                var template = _repository.MissionTemplates_Single
                    .GetByType_SubType(type, subtype);

                // [Step 2] 템플릿이 있으면 미션 생성 큐에 추가
                if (template != null)
                {
                    // Mapping: DB 템플릿 객체를 API 형식으로 변환
                    var missionTemplate = _mapping.MissionTemplates.Create(template);

                    // Queue: 미션 생성 이벤트를 큐에 추가
                    // (별도 스레드가 이 큐를 처리해서 DB 저장)
                    _Queue.Create_Mission(job, missionTemplate, position, worker, seq);

                    // [Step 3] 시퀀스 증가 (다음 미션용)
                    seq++;
                }
            }
            return seq;
        }

        /// <summary>
        /// template_GroupMission 메서드
        /// ============================================================
        /// [목적]
        /// 그룹 미션 템플릿을 조회해서 그룹에 속한 모든 미션을 생성
        ///
        /// [입력]
        /// - job: 대상 Job
        /// - position: 미션이 일어날 Position
        /// - worker: 미션을 수행할 워커
        /// - seq: 현재 시퀀스 번호
        /// - templateGroup: 그룹 이름 (예: "TRANSPORTPICK", "ELEVATORSOURCE")
        ///
        /// [반환]
        /// - 다음 시퀀스 번호 (seq + 그룹 내 미션 개수)
        /// ============================================================
        /// </summary>
        private int template_GroupMission(
            Job job,
            Position position,
            Worker worker,
            int seq,
            string templateGroup
        )
        {
            lock (_lock)
            {
                // [Step 1] 해당 그룹에 속한 모든 미션 템플릿 조회
                // 예: "TRANSPORTPICK" 그룹이면:
                // - MOVE to PICK position
                // - PICKUP
                // - MOVE from PICK position
                // 등의 여러 미션이 포함됨
                var Templates = _repository.MissionTemplates_Group
                    .GetByGroup(templateGroup)
                    .OrderBy(r => r.seq);  // 시퀀스 순서대로 정렬

                // [Step 2] 그룹의 각 미션을 순회해서 큐에 추가
                foreach (var template in Templates)
                {
                    // Mapping: DB 템플릿을 API 형식으로 변환
                    var missionTemplate = _mapping.MissionTemplates.Create(template);

                    // Queue: 미션 생성 이벤트를 큐에 추가
                    _Queue.Create_Mission(job, missionTemplate, position, worker, seq);

                    // [Step 3] 시퀀스 증가 (다음 미션용)
                    seq++;
                }
            }
            return seq;
        }
    }
}