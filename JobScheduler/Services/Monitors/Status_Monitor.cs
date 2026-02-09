using Common.Models.Jobs;

namespace JOB.Services
{
    public partial class SchedulerService
    {
        
        private void StatusChangeControl()
        {
            inProgressControl();
            cancelAbortCompleteControl();
            jobCompleteControl();
            MissionCompleteControl();
        }

        /// <summary>
        /// inProgressControl 메서드
        /// ============================================================
        /// [목적]
        /// 미션 중 하나라도 로봇에게 실제로 전달된 상태면
        /// 해당 Job과 Order의 상태를 INPROGRESS로 변경
        ///
        /// [상태 전이]
        /// COMMANDREQUESTCOMPLETED (또는 SKIPPED)
        ///          ↓
        ///   Job/Order: INPROGRESS
        ///
        /// [의미]
        /// - 로봇이 미션을 받았다 = Job이 실제로 진행 중
        /// - Order도 함께 Transferring 상태로 변경
        /// ============================================================
        /// </summary>
        private void inProgressControl()
        {
            // [주석] 설명: "미션 중 하나의 미션이라도 로봇에게 전달되어 있을 경우,
            // Job 및 Order 상태를 변경한다."

            // ============================================================
            // [Step 1] 상태 COMMANDREQUESTCOMPLETED 또는 SKIPPED인 미션 조회
            // ============================================================
            // 설명:
            // - COMMANDREQUESTCOMPLETED: 로봇에게 미션 명령 전송 완료
            // - SKIPPED: 미션이 건너뛰어짐 (예: 엘리베이터 부재 시)
            // - 이 두 상태는 "로봇이 미션을 인지했다"는 의미
            foreach (var mission in _repository.Missions.GetAll()
                .Where(m => m.state == nameof(MissionState.COMMANDREQUESTCOMPLETED)
                         || m.state == nameof(MissionState.SKIPPED))
                .ToList())
            {
                // ============================================================
                // [Step 2] 이 미션이 속한 Job 조회
                // ============================================================
                var job = _repository.Jobs.GetByid(mission.jobId);

                // [체크] Job이 존재하는지 확인
                if (job != null)
                {
                    // ============================================================
                    // [Step 3] Job 상태를 INPROGRESS로 업데이트
                    // ============================================================
                    // 파라미터:
                    // - job: 대상 Job
                    // - nameof(JobState.INPROGRESS): 변경할 상태
                    // - job.terminateState: 기존 종료 상태 유지
                    // - job.terminationType: 기존 종료 타입 유지 (CANCEL/ABORT)
                    // - job.terminator: 기존 종료자 유지
                    // - true: DB 업데이트 플래그 (true=즉시 저장)
                    updateStateJob(
                        job,
                        nameof(JobState.INPROGRESS),
                        job.terminateState,
                        job.terminationType,
                        job.terminator,
                        true  // [중요] true = DB에 즉시 반영
                    );

                    // ============================================================
                    // [Step 4] 이 Job의 Order 상태도 함께 변경
                    // ============================================================
                    var order = _repository.Orders.GetByid(mission.orderId);

                    // [조건] Order가 존재하고, 아직 Transferring 상태가 아니면 변경
                    if (order != null && order.state != nameof(OrderState.Transferring))
                    {
                        // Order 상태를 Transferring으로 변경
                        // (= 고객이 주문한 작업이 이제 실제로 진행 중)
                        updateStateOrder(order, OrderState.Transferring, true);
                    }
                }
                // [만약 Job이 없으면]
                // - 이는 데이터 무결성 문제 (미션은 있는데 Job이 없음)
                // - 로그 기록 권장 (현재 코드에는 없음 - 개선 필요)
            }
        }

        /// <summary>
        /// cancelAbortCompleteControl 메서드
        /// ============================================================
        /// [목적]
        /// 취소(CANCEL) 또는 중단(ABORT) 처리 중인 Job이
        /// 모든 미션을 완료했을 때 최종 상태를 COMPLETED로 확정
        ///
        /// [상태 전이]
        /// TerminateState: INITED → EXECUTING → COMPLETED
        ///
        /// [프로세스]
        /// 1) TerminateState가 INITED/EXECUTING인 Job 조회
        /// 2) 해당 Job의 모든 미션이 완료/스킵되었는지 확인
        /// 3) 모두 완료되었으면:
        ///    - TerminateState를 COMPLETED로 변경
        ///    - Job 상태를 CANCELCOMPLETED 또는 ABORTCOMPLETED로 변경
        ///    - Order 제거
        /// ============================================================
        /// </summary>
        private void cancelAbortCompleteControl()
        {
            // ============================================================
            // [Step 1] 취소/중단 상태인 Job 조회
            // ============================================================
            // terminateState 조건:
            // - INITED: 취소 요청 초기 상태
            // - EXECUTING: 미션 취소 진행 중
            // - COMPLETED: 취소 완료 (이미 완료된 것도 확인하기 위함)
            var cancelAbortJobs = _repository.Jobs.GetAll()
                .Where(j => (j.terminateState == nameof(TerminateState.INITED))
                         || (j.terminateState == nameof(TerminateState.EXECUTING))
                         || (j.terminateState == nameof(TerminateState.COMPLETED)))
                .ToList();

            // [루프] 각 취소/중단 Job에 대해 처리
            foreach (var cancelAbortJob in cancelAbortJobs)
            {
                // ============================================================
                // [Step 2] 이 Job에 속한 모든 미션 조회
                // ============================================================
                // 미션은 여러 개일 수 있음 (예: MOVE, PICK, DROP 등)
                var missions = _repository.Missions.GetByJobId(cancelAbortJob.guid);

                // ============================================================
                // [Step 3] 아직 진행 중인 미션이 있는지 확인
                // ============================================================
                // GetByRunMissions(): EXECUTING, WAITING 등 "진행 중"인 미션만 필터링
                // (= COMPLETED, SKIPPED 미션 제외)
                var runMission = _repository.Missions.GetByRunMissions(missions);

                // ============================================================
                // [조건] 진행 중인 미션이 없으면 (= 모든 미션 완료)
                // ============================================================
                if (runMission == null || runMission.Count == 0)
                {
                    // [추가 조건] 아직 COMPLETED 상태가 아닌 경우만 처리
                    if (cancelAbortJob.terminateState != nameof(TerminateState.COMPLETED))
                    {
                        // ============================================================
                        // [Step 4] 취소/중단 타입에 따라 최종 상태 결정
                        // ============================================================
                        switch (cancelAbortJob.terminationType)
                        {
                            // [경우 1] CANCEL (사용자가 취소한 경우)
                            case nameof(TerminateType.CANCEL):
                                // 취소 상태를 COMPLETED로 변경
                                cancelAbortJob.terminateState = nameof(TerminateState.COMPLETED);
                                // 취소 완료 시간 기록
                                cancelAbortJob.terminatedAt = DateTime.Now;
                                // Job 최종 상태를 CANCELCOMPLETED로 변경
                                updateStateJob(
                                    cancelAbortJob,
                                    nameof(JobState.CANCELCOMPLETED),  // Job 상태
                                    cancelAbortJob.terminateState,      // 종료 상태: COMPLETED
                                    cancelAbortJob.terminationType,     // 종료 타입: CANCEL
                                    cancelAbortJob.terminator,          // 취소자 정보
                                    true  // DB 업데이트 플래그
                                );
                                break;

                            // [경우 2] ABORT (시스템이 중단한 경우)
                            case nameof(TerminateType.ABORT):
                                // 중단 상태를 COMPLETED로 변경
                                cancelAbortJob.terminateState = nameof(TerminateState.COMPLETED);
                                // 중단 완료 시간 기록
                                cancelAbortJob.terminatedAt = DateTime.Now;
                                // Job 최종 상태를 ABORTCOMPLETED로 변경
                                updateStateJob(
                                    cancelAbortJob,
                                    nameof(JobState.ABORTCOMPLETED),   // Job 상태
                                    cancelAbortJob.terminateState,      // 종료 상태: COMPLETED
                                    cancelAbortJob.terminationType,     // 종료 타입: ABORT
                                    cancelAbortJob.terminator,          // 중단자 정보
                                    true  // DB 업데이트 플래그
                                );
                                break;
                        }
                    }

                    // ============================================================
                    // [Step 5] Order/Job을 큐에서 제거 처리
                    // ============================================================
                    var order = _repository.Orders.GetByid(cancelAbortJob.orderId);

                    if (order != null)
                    {
                        // [경우 1] Order가 존재하면:
                        // - Order 상태를 None으로 변경 (= 종료됨)
                        updateStateOrder(order, OrderState.None);
                        // - Order 제거 이벤트를 큐에 추가
                        // (_Queue: 비동기 처리 큐, DateTime.Now: 제거 시간)
                        _Queue.Remove_Order(order, DateTime.Now);
                    }
                    else
                    {
                        // [경우 2] Order가 없으면 Job만 제거
                        // - Job 제거 이벤트를 큐에 추가
                        _Queue.Remove_Job(cancelAbortJob, DateTime.Now);
                    }
                }
                // [만약 아직 진행 중인 미션이 있으면]
                // - 아무것도 하지 않고 다음 루프에서 다시 확인
                // - (모든 미션이 완료될 때까지 대기)
            }
        }

        /// <summary>
        /// jobCompleteControl 메서드
        /// ============================================================
        /// [목적]
        /// Job의 모든 미션이 완료/스킵되었을 때
        /// Job과 Order를 최종적으로 COMPLETED 상태로 변경
        ///
        /// [상태 전이]
        /// INPROGRESS → COMPLETED
        ///
        /// [조건]
        /// - Job 상태: INPROGRESS (현재 진행 중)
        /// - 모든 미션: COMPLETED 또는 SKIPPED (완료됨)
        /// - 미션이 하나라도 다른 상태면: Job은 COMPLETED 안 됨
        /// ============================================================
        /// </summary>
        private void jobCompleteControl()
        {
            // ============================================================
            // [Step 1] INPROGRESS 상태인 Job 조회
            // ============================================================
            // 설명: 현재 실행 중인 Job만 필터링
            foreach (var job in _repository.Jobs.GetAll()
                .Where(x => x.state == nameof(JobState.INPROGRESS)))
            {
                // ============================================================
                // [Step 2] 이 Job에 속한 모든 미션 조회
                // ============================================================
                var missions = _repository.Missions.GetByJobId(job.guid);

                // [방어 조건] 미션이 없으면 건너뜀
                if (missions == null || missions.Count == 0)
                    continue;

                // ============================================================
                // [Step 3] 완료되지 않은 미션이 있는지 확인
                // ============================================================
                // FirstOrDefault(): 다음 조건을 만족하는 "첫 번째" 미션 찾기:
                // - state != COMPLETED (완료 아님)
                // - state != SKIPPED (건너뜀 아님)
                // → 즉, EXECUTING, WAITING, COMMANDREQUEST 등의 미션이 있는지 확인
                var mission = missions.FirstOrDefault(s =>
                    s.state != nameof(MissionState.COMPLETED)
                    && s.state != nameof(MissionState.SKIPPED));

                // ============================================================
                // [조건] 미완료 미션이 없으면 (= 모든 미션 완료)
                // ============================================================
                if (mission == null)
                {
                    // ============================================================
                    // [Step 4] Order 조회
                    // ============================================================
                    var order = _repository.Orders.GetByid(job.orderId);

                    // [경우 1] Order가 존재하는 경우
                    if (order != null)
                    {
                        // [4-1] Job 상태를 COMPLETED로 변경
                        updateStateJob(
                            job,
                            nameof(JobState.COMPLETED),  // 상태: 완료
                            job.terminateState,          // 기존 종료 상태 유지
                            job.terminationType,         // 기존 종료 타입 유지
                            job.terminator               // 기존 종료자 유지
                                                         // [주의] 5번째 파라미터 없음 = DB 업데이트 안 함 (또는 기본값)
                        );

                        // [4-2] Order 상태를 None으로 변경 (= 종료)
                        updateStateOrder(order, OrderState.None);

                        // [4-3] Order 제거 이벤트를 큐에 추가
                        // (실제 DB 삭제는 별도 스레드에서 비동기 처리)
                        _Queue.Remove_Order(order, DateTime.Now);
                    }
                    // [경우 2] Order가 없는 경우 (단독 Job)
                    else
                    {
                        // [4-1] Job 상태를 COMPLETED로 변경
                        updateStateJob(
                            job,
                            nameof(JobState.COMPLETED),  // 상태: 완료
                            job.terminateState,          // 기존 종료 상태 유지
                            job.terminationType,         // 기존 종료 타입 유지
                            job.terminator               // 기존 종료자 유지
                        );

                        // [4-2] Job 제거 이벤트를 큐에 추가
                        _Queue.Remove_Job(job, DateTime.Now);
                    }
                }
                // [만약 아직 미완료 미션이 있으면]
                // - Job은 INPROGRESS 상태 유지
                // - 다음 루프에서 다시 확인
            }
        }

        /// <summary>
        /// MissionCompleteControl 메서드
        /// ============================================================
        /// [목적]
        /// 고아 미션(jobId == null, 즉 Job이 없는 미션) 정리 및 제거
        ///
        /// [왜 필요한가?]
        /// - 정상적으로는 모든 미션이 Job에 속해야 함
        /// - 데이터 동기화 오류 등으로 인해 Job 없는 미션이 발생 가능
        /// - 이런 고아 미션이 DB에 쌓이면 메모리/성능 문제 발생
        ///
        /// [처리]
        /// - COMPLETED 또는 SKIPPED 상태인 고아 미션만 삭제
        /// - 진행 중인 미션은 삭제하지 않음 (조사 필요)
        /// ============================================================
        /// </summary>
        private void MissionCompleteControl()
        {
            // ============================================================
            // [Step 1] JobId가없는 미션 조회
            // ============================================================
            // 조건: jobId == null (어떤 Job에도 속하지 않은 미션)
            var missions = _repository.Missions.GetAll()
                .Where(r => r.jobId == null)
                .ToList();

            // [방어 조건] JobId가없는 미션이 없으면 종료
            if (missions.Count == 0 || missions == null)
                return;

            // ============================================================
            // [Step 2] 각 JobId가없는 미션 처리
            // ============================================================
            foreach (var mission in missions)
            {
                // [조건] COMPLETED 또는 SKIPPED 상태인 미션만 삭제
                // (= 이미 완료되었으므로 DB에 둘 필요 없음)
                if (mission.state == nameof(MissionState.COMPLETED)
                    || mission.state == nameof(MissionState.SKIPPED))
                {
                    // ============================================================
                    // [Step 3] 메모리 에서 미션 삭제
                    // ============================================================
                    // _repository.Missions.Remove(): 즉시 DB에서 삭제
                    // (대기열이 아니라 즉시 삭제)
                    _repository.Missions.Remove(mission);

                    // [로그 추천] 
                    // EventLogger.Info($"[MISSION][ORPHAN][DELETED] missionId={mission.guid}");
                    // (현재 코드에는 없음 - 디버깅 시 추가 권장)
                }
                // [만약 JobId가없는 미션이 EXECUTING 상태면]
                // - 이는 비정상 상황 (Job이 없는데 미션이 진행 중)
                // - 로그 기록 및 조사 필요
                // - EventLogger.Error($"[MISSION][ORPHAN][EXECUTING] missionId={mission.guid}");
            }
        }
    }
}
