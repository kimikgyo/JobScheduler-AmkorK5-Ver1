using Common.Models.Jobs;

namespace JOB.Services
{
    public partial class SchedulerService
    {
        public void ElevatorPolicy()
        {
            CancelCrossFloorJobsWhenElevatorDown("NO1");
            ElevatorModeChange();
        }

        /// <summary>
        /// 엘리베이터가 비활성(elevatorActive=false)일 때,
        /// "층이 다른 Job(cross-floor)" 중에서
        /// - terminator == null (아직 종료 처리 안됨)
        /// - state != INPROGRESS (이미 주행/실행 중이 아닌 것만)
        /// 을 전부 CANCEL 처리(terminateState=INITED)한다.
        /// </summary>
        private void CancelCrossFloorJobsWhenElevatorDown(string elevatorNo)
        {
            // 0) 엘리베이터 상태 확인
            bool elevatorActive = _repository.Elevator.Active(elevatorNo);
            if (elevatorActive) return;

            // 1) 취소 대상 Job 후보 수집
            var allJobs = _repository.Jobs.GetAll();
            if (allJobs == null || !allJobs.Any()) return;

            // ※ "층이 다른 Job" 판정은 기존에 사용 중인 IsSameFloorJob(job)을 그대로 사용
            //    IsSameFloorJob(job) == false  => cross-floor
            var cancelTargets = allJobs
                .Where(job =>
                    job != null &&
                    job.terminator == null &&
                    job.state != nameof(JobState.INPROGRESS) &&
                    IsSameFloorJob(job) == false)
                .ToList();

            if (cancelTargets.Count == 0)
                return;

            // 2) 일괄 Cancel 처리
            //    - message는 로그 태그용 (원하는 포맷으로 바꿔도 됨)
            foreach (var job in cancelTargets)
            {
                // 안전상 null 재확인
                if (job == null) continue;

                // Cancel 처리 (terminateState=INITED / terminator / terminationType / terminatedAt 업데이트)
                jobTerminateState_Change_Inited(
                    job,
                    message: $"[ELEVATOR][{elevatorNo}][DOWN][CANCEL] Cross-floor job canceled because elevator is unavailable."
                );
            }

            // 3) 요약 로그
            EventLogger.Info($"[ELEVATOR][{elevatorNo}][DOWN][CANCEL] totalCanceled={cancelTargets.Count}");
        }

        /// <summary>
        /// ELEVATOR 서비스의 MODECHANGE 미션을 "안전 조건"이 만족될 때만 실행한다.
        /// 안전 조건:
        ///  1) MODECHANGE 미션이 존재하고 WAITING 상태여야 한다.
        ///  2) INPROGRESS(진행중) Job이 "없으면" -> (정책상) 모드체인지 보내도 된다면 바로 전송 후 종료.
        ///  3) INPROGRESS Job이 "있으면"
        ///     - 모든 Job에 대해
        ///       a) 목적지 Position의 MapId가 존재해야 한다.
        ///       b) Worker의 MapId가 존재해야 한다.
        ///       c) 각 Job에서 목적지 MapId == Worker MapId 여야 한다. (같은 층/맵에서 움직이는 상황)
        ///       d) 모든 Job의 MapId가 서로 동일해야 한다.
        ///     - 위 조건 중 1개라도 깨지면 절대 모드체인지 미션을 보내면 안 된다.
        /// </summary>
        private void ElevatorModeChange()
        {
            // -----------------------------
            // 0) MODECHANGE 미션 조회
            // -----------------------------

            // 전체 미션 목록 조회
            var missions = _repository.Missions.GetAll();

            // 미션이 없으면 할 게 없음
            if (missions == null || missions.Count == 0) return;

            // ELEVATOR + ACTION + MODECHANGE 미션 1개를 찾음
            var mission = missions.FirstOrDefault(m =>
                m.service == nameof(Service.ELEVATOR) &&
                m.type == nameof(MissionType.ACTION) &&
                m.subType == nameof(MissionSubType.MODECHANGE));

            // MODECHANGE 미션이 없으면 종료
            if (mission == null) return;

            // 미션 상태가 WAITING이 아니면(이미 요청 중이거나 완료 등) 중복 실행 방지 위해 종료
            if (mission.state != nameof(MissionState.WAITING)) return;

            // -----------------------------
            // 1) 진행중(INPROGRESS) Job 조회
            // -----------------------------

            // terminator == null : 종료 처리되지 않은 Job
            // state == INPROGRESS : 현재 진행 중인 Job
            var jobs = _repository.Jobs.GetAll().Where(j => j.terminator == null && j.state == nameof(JobState.INPROGRESS)).ToList();

            // -----------------------------
            // 2) 진행중 Job이 하나도 없을 때 처리
            // -----------------------------
            // 정책: 진행중 Job이 없으면 모드체인지 보내도 되는 시나리오라면 여기서 바로 전송하고 종료.
            // (※ 만약 "진행중 Job이 없을 때는 보내면 안 된다"라면 -> 여기에서 return만 하면 됨)
            if (jobs == null || jobs.Count == 0)
            {
                // 엘리베이터 모드 변경 미션 전송 시도
                bool sent = ElevatorPostMission(mission);

                // 전송이 성공했으면, 미션 상태를 COMMANDREQUESTCOMPLETED로 변경
                if (sent)
                {
                    updateStateMission(mission, nameof(MissionState.COMMANDREQUESTCOMPLETED), true);
                }

                // ★중요: 아래 로직으로 내려가면 중복 전송될 수 있으므로 반드시 종료
                return;
            }

            // -----------------------------
            // 3) 안전 조건 검사 (핵심)
            // -----------------------------
            // 요구사항:
            //  - 여러 Job이 존재할 수 있음
            //  - 단 1개라도 목적지층(MapId)과 워커층(MapId)이 다르면 모드 변경 미션을 보내면 안 됨
            //
            // 구현:
            //  - baseMapId: 첫 번째 Job의 MapId를 기준으로 잡고,
            //    이후 모든 Job의 destMapId/workerMapId가 이 값과 완전히 같은지 검사

            string baseMapId = null; // 모든 Job의 "목적지 MapId" == "워커 MapId" == baseMapId 여야 함

            // 진행중인 Job을 모두 검사
            foreach (var job in jobs)
            {
                // job 자체가 null이면(비정상 데이터) 안전을 위해 모드체인지 금지 -> 종료
                if (job == null) return;

                // -----------------------------
                // 3-1) 목적지 Position에서 MapId 얻기
                // -----------------------------

                // 목적지 ID가 없으면 목적지층(맵)을 알 수 없으므로 안전 조건 불만족 -> 종료
                if (string.IsNullOrWhiteSpace(job.destinationId)) return;

                // Position 리포지토리에서 목적지 정보 조회
                var destPos = _repository.Positions.GetById(job.destinationId);

                // 목적지 Position이 없으면 안전 조건 불만족 -> 종료
                if (destPos == null) return;

                // 목적지 Position의 MapId (프로젝트 필드명: destPos.mapId)
                var destMapId = destPos.mapId;

                // 목적지 MapId가 비어있으면 비교 불가 -> 종료
                if (string.IsNullOrWhiteSpace(destMapId)) return;

                // -----------------------------
                // 3-2) Worker에서 MapId 얻기
                // -----------------------------

                // 진행중인데 워커가 미할당이면 "전부 동일" 조건 자체가 깨짐 -> 종료
                if (IsInvalid(job.assignedWorkerId)) return;

                // Worker 리포지토리에서 워커 정보 조회
                var worker = _repository.Workers.GetById(job.assignedWorkerId);

                // 워커 정보가 없으면 안전 조건 불만족 -> 종료
                if (worker == null) return;

                // 워커의 MapId (프로젝트 필드명: worker.mapId)
                var workerMapId = worker.mapId;

                // 워커 MapId가 비어있으면 비교 불가 -> 종료
                if (string.IsNullOrWhiteSpace(workerMapId)) return;

                // -----------------------------
                // 3-3) "각 Job에서 목적지층(MapId) == 워커층(MapId)" 검사
                // -----------------------------
                // 한 Job이라도 목적지층과 워커층이 다르면,
                // 지금 엘리베이터 모드 변경을 보내면 다른 층/맵 Job과 충돌할 수 있으므로 절대 보내면 안 됨.
                if (destMapId != workerMapId) return;

                // -----------------------------
                // 3-4) "모든 Job이 동일 MapId(baseMapId)를 공유" 검사
                // -----------------------------

                // 첫 번째 유효 Job이면 기준 MapId 설정
                if (baseMapId == null)
                {
                    baseMapId = destMapId; // destMapId == workerMapId 이므로 무엇을 넣어도 동일
                }
                else
                {
                    // 기준과 다른 MapId가 하나라도 발견되면 조건 불만족 -> 종료
                    if (baseMapId != destMapId) return;
                }
            }

            // -----------------------------
            // 4) 모든 안전 조건 통과 -> MODECHANGE 전송
            // -----------------------------
            // 여기까지 도달했다는 것은:
            //  - 모든 Job이 (destMapId == workerMapId)를 만족했고
            //  - 모든 Job의 MapId가 baseMapId로 동일했다는 뜻
            // => 모드 변경 미션을 보내도 안전

            bool commandRequest = ElevatorPostMission(mission);

            // 전송 성공 시 미션 상태 변경
            if (commandRequest)
            {
                updateStateMission(mission, nameof(MissionState.COMMANDREQUESTCOMPLETED), true);
            }
        }
    }
}