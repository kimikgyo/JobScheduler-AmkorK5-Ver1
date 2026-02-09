using Common.DTOs.Rests.Elevator;
using Common.Models.Bases;
using Common.Models.Jobs;

namespace JOB.Services
{
    public partial class SchedulerService
    {
        public void ElevatorPolicy()
        {
            CancelCrossFloorJobsWhenElevatorDown();
            HandleChangingToNotAgvMode();
            HandleChangingToAgvMode();
        }

        /// <summary>
        /// [목적]
        /// - 엘리베이터가 "사용 불가 상태" (NOTAGVMODE / NOTAGVMODE_CHANGING_AGVMODE / PROTOCOLERROR) 일 때,
        /// - "층이 다른 Job(cross-floor)" 중 아직 종료처리 안 되었고(terminator==null),
        /// - 현재 실행중이 아닌(state != INPROGRESS) Job만 골라서,
        /// - 그 Job이 실제로 해당 엘리베이터를 사용하는지(미션 파라미터 linkedFacility == elevatorId) 확인 후
        /// - CANCEL 처리(terminateState=INITED) 한다.
        ///
        /// [왜 linkedFacility 체크가 필요한가?]
        /// - cross-floor Job이라고 해서 항상 "모든 엘리베이터"를 쓰는 것은 아니다.
        /// - 특정 엘리베이터(예: 1호기)만 DOWN인데, 다른 엘리베이터(예: 2호기) 쓰는 Job까지 취소되면 안 된다.
        /// - 따라서 Job -> Missions -> Parameters에서 linkedFacility 값이 "다운된 엘리베이터 id"와 동일한 Job만 취소한다.
        /// </summary>
        private void CancelCrossFloorJobsWhenElevatorDown()
        {
            // ============================================================
            // 1) 엘리베이터 전체 조회
            // ============================================================
            var elevators = _repository.Elevator.GetAll();

            // 엘리베이터 데이터가 없으면 할 일이 없음
            if (elevators == null || elevators.Count == 0) return;

            // ============================================================
            // 2) "사용 불가 엘리베이터" 목록 추출
            //    - mode가 NOTAGVMODE
            //    - mode가 NOTAGVMODE_CHANGING_AGVMODE
            //    - 또는 state가 PROTOCOLERROR
            // ============================================================
            var elevatorNotActives = elevators
                .Where(e => e != null && (
                       e.mode == nameof(ElevatorMode.NOTAGVMODE)
                    || e.mode == nameof(ElevatorMode.AGVMODE_CHANGING_NOTAGVMODE)
                    || e.state == nameof(ElevatorState.PROTOCOLERROR)
                    || e.state == nameof(ElevatorState.DISCONNECT)
                ))
                .ToList();

            // 다운된 엘리베이터가 하나도 없으면 취소할 이유가 없음
            if (elevatorNotActives == null || elevatorNotActives.Count == 0) return;

            // ============================================================
            // 3) Job 전체 조회 후, 취소 후보(cross-floor)만 추출
            //    조건:
            //    - allJobs != null && allJobs.Count > 0 : 조회 결과가 있어야 함
            //    - job != null : 컬렉션 내 null 방어
            //    - terminator == null : 아직 terminate(취소/완료) 처리 안 된 Job만 대상
            //    - state != COMPLETED && state != CANCELCOMPLETED : 이미 종료된 Job 제외
            //    - IsSameFloorJob(job) == false : 같은 층 Job이 아닌 경우만(= cross-floor)
            //      (※ 필요 시 추가 조건: state != INPROGRESS : 실행 중 Job은 안전상 제외)
            //      (※ 필요 시 추가 조건: IsSameFloorJob(job) == true : 같은층 Job일 경우에만)
            // ============================================================

            // 메모리 에서 "전체 Job" 목록을 가져온다.
            var allJobs = _repository.Jobs.GetAll();

            // 안전장치: Job 목록이 null 이거나, 개수가 0이면 더 처리할 게 없으므로 종료한다.
            if (allJobs == null || allJobs.Count == 0) return;

            // 필터링된 Job만 담을 리스트(= 조건을 통과한 Job 후보들)
            var jobs = new List<Job>();

            // 전체 Job을 하나씩 순회하면서 "취소/처리 대상"만 골라낸다.
            foreach (var job in allJobs)
            {
                // 1) null 방어: 리스트 안에 null 항목이 섞여있을 수 있으므로 스킵
                if (job == null) continue;

                // 2) 이미 종료(terminate) 처리된 Job은 제외
                //    terminator != null 이면 누군가가 종료자로 기록해둔 상태(취소/종료 처리 완료 또는 진행 중)라서 건드리지 않음
                if (job.terminator != null) continue;

                // 3) 이미 완료/취소완료된 Job은 제외
                //    - COMPLETED: 정상 완료
                //    - CANCELCOMPLETED: 취소 처리까지 완료된 상태
                if (job.state == nameof(JobState.COMPLETED) || job.state == nameof(JobState.CANCELCOMPLETED)) continue;
                //if (job.state != nameof(JobState.INPROGRESS)) continue;

                // 4) "같은 층 Job" 조건이 아니면 제외
                //    IsSameFloorJob(job) == false 면 이 로직의 대상이 아닌 Job(예: cross-floor 등)이므로 스킵
                if (!IsSameFloorJob(job)) continue;
                //if (IsSameFloorJob(job)) continue;

                //  위 조건을 모두 통과한 Job만 후보 리스트에 추가
                jobs.Add(job);
            }



            // 취소 대상이 없으면 종료
            if (jobs == null || jobs.Count == 0) return;

            // ============================================================
            // 4) 실제 취소 로직
            //    - 핵심: 각 Job이 "다운된 엘리베이터"를 쓰는지 확인해야 함
            //
            //    체크 방법:
            //    - Job 하나를 잡고,
            //    - 그 Job에 연결된 Missions를 조회한 뒤,
            //    - Missions의 parameters를 평탄화해서(SelectMany) linkedFacility를 찾고,
            //    - linkedFacility 값이 다운된 엘리베이터 id와 같다면 => 그 Job은 그 엘리베이터를 필요로 함
            //    - 그때만 Cancel 처리한다.
            // ============================================================
            foreach (var cancelTarget in jobs)
            {
                // --------------------------------------------
                // 4-1) Job -> Missions 조회
                // --------------------------------------------
                // cancelTarget.guid 로 해당 Job에 딸린 미션 목록을 가져온다.
                // (여기서 missions가 null/empty일 수 있으므로 반드시 방어)
                var missions = _repository.Missions.GetByJobId(cancelTarget.guid);
                if (missions == null || missions.Count == 0)
                {
                    // 미션이 없으면 이 Job이 엘리베이터를 쓰는지 판별 불가 => 취소하지 않음
                    continue;
                }

                // --------------------------------------------
                // 4-2) Missions -> Parameters 평탄화
                // --------------------------------------------
                // GetParametas(missions)는
                // - parameters가 null인 미션 제외
                // - 모든 미션의 parameters를 하나의 List<Parameter>로 합쳐서 반환한다.
                var parameters = _repository.Missions.GetParametas(missions);
                if (parameters == null || parameters.Count == 0)
                {
                    // 파라미터가 없으면 linkedFacility 판정 불가 => 취소하지 않음
                    continue;
                }

                // --------------------------------------------
                // 4-3) 다운된 엘리베이터 중, 이 Job이 사용하는 엘리베이터가 있는지 확인
                // --------------------------------------------
                Elevator matchedElevator = null;

                foreach (var elevatorNotActive in elevatorNotActives)
                {
                    if (elevatorNotActive == null) continue;

                    // linkedFacility 파라미터를 찾는다.
                    // 조건:
                    // - key == "linkedFacility"
                    // - value == elevatorNotActive.id
                    //
                    // hit != null 이면
                    //    이 Job은 "해당 엘리베이터"를 사용해야 한다는 뜻
                    //    (즉, 그 엘리베이터가 다운이면 이 Job을 취소해야 함)
                    var hit = parameters.FirstOrDefault(p =>
                        p != null
                        && p.key == "linkedFacility"
                        && p.value == elevatorNotActive.id
                    );

                    if (hit != null)
                    {
                        matchedElevator = elevatorNotActive;

                        // 하나라도 매칭되면 더 볼 필요 없음
                        break;
                    }
                }

                // --------------------------------------------
                // 4-4) 매칭 없으면 취소하면 안 됨
                // --------------------------------------------
                //  다운된 엘리베이터를 "실제로" 사용하는 Job만 취소해야 하므로,
                //    매칭이 없으면 continue로 넘어간다.
                if (matchedElevator == null)
                    continue;

                // --------------------------------------------
                // 4-5) Cancel 처리 (terminateState=INITED)
                // --------------------------------------------
                // 여기서 jobTerminateState_Change_Inited()가 내부적으로
                // terminator / terminatedAt / terminationType / terminateState 등을 세팅한다고 가정.
                jobTerminateState_Change_Inited(
                    cancelTarget,
                    message: $"[ELEVATOR][{matchedElevator.id}][DOWN]"
                );
            }

            // ============================================================
            // 5) 요약 로그(선택)
            // - 전체 몇 개를 캔슬했는지 세고 싶으면,
            //   위에서 카운트 변수(totalCanceled++)를 추가해도 됨
            // ============================================================
            // EventLogger.Info($"[ELEVATOR][DOWN][CANCEL] done");
        }

        /// <summary>
        /// [목적]
        /// - 엘리베이터 목록 중 mode == "AGVMODE_CHANGING_NOTAGVMODE" 인 것이 있을 때만
        ///   후속 처리(해당 엘리베이터 사용중 Job 여부 확인 -> 조건 만족 시 Job 취소 후 NOTAGVMODE 변경 PATCH)를 시작한다.
        ///
        /// [정책(수정 반영)]
        /// - mode == AGVMODE_CHANGING_NOTAGVMODE 인 엘리베이터 E에 대해:
        ///   1) E를 사용하는 INPROGRESS Job이 있는지 찾는다.
        ///   2) 그 Job의 엘리베이터 단계(subType) 중 아래 목록에 해당하는 것들을 검사한다.
        ///      - 하나라도 WAITING이 아니면 => 절대 취소/모드변경 하면 안 됨(스킵)
        ///      - 전부 WAITING이면 => Job을 취소(terminate inited)한 뒤, NOTAGVMODE 변경 PATCH를 보낸다.
        ///
        /// [엘리베이터 단계 목록]
        ///  ELEVATORWAITMOVE
        ///  ELEVATORSOURCEFLOOR
        ///  ELEVATORENTERMOVE
        ///  ELEVATORENTERDOORCLOSE
        ///  ELEVATORDESTINATIONFLOOR
        ///  SWITCHINGMAP
        ///  ELEVATOREXITMOVE
        ///  ELEVATOREXITDOORCLOSE
        /// </summary>
        private void HandleChangingToNotAgvMode()
        {
            // ============================================================
            // 0) 엘리베이터 목록 조회
            // ============================================================
            var elevators = _repository.Elevator.GetAll();
            if (elevators == null || elevators.Count == 0) return;

            // ============================================================
            // 1) [시작 조건] AGVMODE_CHANGING_NOTAGVMODE 엘리베이터가 있는지 먼저 체크
            //    - 하나도 없으면 이 함수는 할 일이 없음(바로 종료)
            // ============================================================
            var hasChanging = elevators.FirstOrDefault(e => e != null && e.mode == nameof(ElevatorMode.AGVMODE_CHANGING_NOTAGVMODE));
            if (hasChanging == null) return;

            EventLogger.Info("[HandleChangingToNotAgvMode][BEGIN] found AGVMODE_CHANGING_NOTAGVMODE");

            // ============================================================
            // 2) 엘리베이터 서비스 API 조회
            // ============================================================
            var serviceApi = _repository.ServiceApis.GetAll()
                .FirstOrDefault(r => r != null && r.type == nameof(Service.ELEVATOR));

            if (serviceApi == null)
            {
                EventLogger.Warn("[HandleChangingToNotAgvMode][API_IsNull] service=ELEVATOR");
                return;
            }

            // ============================================================
            // 3) 진행중(INPROGRESS) Job 목록 조회
            // ============================================================
            var allJobs = _repository.Jobs.GetAll();
            if (allJobs == null) return;

            var inprogressJobs = allJobs
                .Where(j => j != null && j.terminator == null && j.state == nameof(JobState.INPROGRESS))
                .ToList();

            // ============================================================
            // 4) 실제 처리: mode == AGVMODE_CHANGING_NOTAGVMODE 인 엘리베이터만 골라서 수행
            // ============================================================
            foreach (var elevator in elevators)
            {
                if (elevator == null) continue;

                if (elevator.mode != nameof(ElevatorMode.AGVMODE_CHANGING_NOTAGVMODE))
                    continue;

                // ------------------------------------------------------------
                // 4-1) 해당 엘리베이터를 사용하는 진행중 Job 중에서
                //      "엘리베이터 관련 단계가 전부 WAITING인 Job"만 취소 대상으로 잡는다.
                //
                //      - 하나라도 WAITING이 아닌 단계가 있으면 => 해당 Job은 취소하면 안 됨
                //      - 전부 WAITING이면 => 취소 가능
                // ------------------------------------------------------------
                List<Job> cancelableJobs = new List<Job>(); // ✅ 전부 WAITING인 Job만 담는다
                bool hasBlockedJob = false;                 // ✅ 하나라도 WAITING이 아닌 Job이 있으면 true (전체 차단 정책이면 사용)

                foreach (var job in inprogressJobs)
                {
                    if (job == null) continue;

                    // Job에 연결된 미션 조회
                    var missions0 = _repository.Missions.GetByJobId(job.guid);
                    if (missions0 == null || missions0.Count == 0) continue;

                    // 완료/취소 제외
                    var missions = missions0
                        .Where(m => m != null
                                 && m.state != nameof(MissionState.COMPLETED)
                                 && m.state != nameof(MissionState.CANCELED))
                        .ToList();

                    if (missions.Count == 0) continue;

                    // Missions -> Parameters 평탄화
                    var parameters = _repository.Missions.GetParametas(missions);
                    if (parameters == null || parameters.Count == 0) continue;

                    // linkedFacility == elevator.id 인 Job만 "이 엘리베이터 사용 Job"으로 판단
                    var hit = parameters.FirstOrDefault(p => p != null && p.key == "linkedFacility" && p.value == elevator.id);
                    if (hit == null) continue;

                    // ✅ 엘리베이터 단계 목록 중 "전부 WAITING인지" 검사
                    bool hasAnyElevatorStep = false;
                    bool allElevatorStepsAreWaiting = true;

                    foreach (var m in missions)
                    {
                        if (m == null) continue;

                        bool isElevatorStep =
                            m.subType == "ELEVATORWAITMOVE" ||
                            m.subType == "ELEVATORSOURCEFLOOR" ||
                            m.subType == "ELEVATORENTERMOVE" ||
                            m.subType == "ELEVATORENTERDOORCLOSE" ||
                            m.subType == "ELEVATORDESTINATIONFLOOR" ||
                            m.subType == "SWITCHINGMAP" ||
                            m.subType == "ELEVATOREXITMOVE" ||
                            m.subType == "ELEVATOREXITDOORCLOSE";

                        if (!isElevatorStep) continue;

                        hasAnyElevatorStep = true;

                        // 하나라도 WAITING이 아니면 취소 불가
                        if (m.state != nameof(MissionState.WAITING))
                        {
                            allElevatorStepsAreWaiting = false;
                            break;
                        }
                    }

                    // 엘리베이터 단계가 하나도 없으면 안전하게 취소 대상에서 제외
                    if (!hasAnyElevatorStep) continue;

                    // 전부 WAITING이면 취소 가능
                    if (allElevatorStepsAreWaiting)
                    {
                        cancelableJobs.Add(job);
                    }
                    else
                    {
                        // WAITING이 아닌 단계가 하나라도 존재 -> 취소/모드변경하면 안 됨
                        hasBlockedJob = true;

                        // “아래 목록중 하나라도 Wait과 다른게 있으면 취소하면 안된다” 조건을 강하게 적용하려면
                        // 여기서 즉시 break하고, 아래에서 elevator 단위로 continue(스킵)하면 됨.
                        break;
                    }
                }

                // ------------------------------------------------------------
                // 4-2) 하나라도 WAITING이 아닌 엘리베이터 단계가 발견되면
                //      -> 취소/모드변경 절대 금지 (스킵)
                // ------------------------------------------------------------
                if (hasBlockedJob)
                {
                    EventLogger.Info($"[HandleChangingToNotAgvMode][SKIP] elevatorId={elevator.id} mode={elevator.mode} reason=ELEVATOR_STEP_NOT_WAITING");
                    continue;
                }

                // ------------------------------------------------------------
                // 4-3) 전부 WAITING인 Job이 있으면 -> Job을 취소한다
                // ------------------------------------------------------------
                if (cancelableJobs.Count > 0)
                {
                    foreach (var job in cancelableJobs)
                    {
                        if (job == null) continue;

                        // Job 취소(terminateState=INITED) - 너 프로젝트 취소 함수로 유지/교체
                        jobTerminateState_Change_Inited(job, message: $"[ELEVATOR][{elevator.id}][ALL_WAITING_CANCEL]");
                    }
                }

                // ------------------------------------------------------------
                // 4-4) 취소가 끝났으면 -> NOTAGVMODE 변경 PATCH 전송
                //      (너가 고정하겠다고 한 Patch 코드는 그대로 사용)
                // ------------------------------------------------------------
                var Request_Patch = new Request_Patch_ElevatorDto
                {
                    elevatorId = elevator.id,
                    action = "APPLY",
                    targetMode = nameof(ElevatorMode.NOTAGVMODE)
                };

                var modeChangePatch = serviceApi.Api.Patch_Elevator_ModeChange_Async(Request_Patch).Result;
                if (modeChangePatch != null)
                {
                    //[조건5] 상태코드 200~300 까지는 완료 처리
                    if (modeChangePatch.statusCode >= 200 && modeChangePatch.statusCode < 300)
                    {
                        EventLogger.Info($"[PatchModeChange][ELEVATOR][Success], Message = {modeChangePatch.statusText}, ElevatorId = {Request_Patch.elevatorId}, Action = {Request_Patch.action}" +
                                         $", TargetMode = {Request_Patch.targetMode}");
                    }
                    else EventLogger.Warn($"[PatchModeChange][ELEVATOR][Success], Message = {modeChangePatch.statusText}, ElevatorId = {Request_Patch.elevatorId}, Action = {Request_Patch.action}" +
                                         $", TargetMode = {Request_Patch.targetMode}");
                }
                else EventLogger.Warn($"[PatchModeChange][ELEVATOR][APIResponseIsNull]  ElevatorId = {Request_Patch.elevatorId}, Action = {Request_Patch.action}" +
                                         $", TargetMode = {Request_Patch.targetMode}");
            }

            //EventLogger.Info("[HandleChangingToNotAgvMode][END]");
        }

        private void HandleChangingToAgvMode()
        {
            // ============================================================
            // 0) 엘리베이터 목록 조회
            // ============================================================
            var elevators = _repository.Elevator.GetAll();
            if (elevators == null || elevators.Count == 0) return;

            // ============================================================
            // 1) [시작 조건] NOTAGVMODE_CHANGING_AGVMODE 엘리베이터가 있는지 먼저 체크
            //    - 하나도 없으면 이 함수는 할 일이 없음(바로 종료)
            // ============================================================
            var hasChanging = elevators.FirstOrDefault(e => e != null && e.mode == nameof(ElevatorMode.NOTAGVMODE_CHANGING_AGVMODE));
            if (hasChanging == null) return;

            // 여기까지 왔다는 것은:
            // - 최소 1대 이상 "NOTAGVMODE_CHANGING_AGVMODE" 상태의 엘리베이터가 존재한다는 의미
            // => 이제 후속 처리 시작
            EventLogger.Info("[HandleChangingToAgvMode][BEGIN] found NOTAGVMODE_CHANGING_AGVMODE");

            // ============================================================
            // 2) 엘리베이터 서비스 API 조회
            // ============================================================
            var serviceApi = _repository.ServiceApis.GetAll().FirstOrDefault(r => r != null && r.type == nameof(Service.ELEVATOR));

            if (serviceApi == null)
            {
                EventLogger.Warn("[HandleChangingToAgvMode][API_IsNull] service=ELEVATOR");
                return;
            }

            // ============================================================
            // 3) 실제 처리: NOTAGVMODE_CHANGING_AGVMODE 인 엘리베이터만 골라서 AGVMODE로 PATCH
            //    - 이 루틴은 Job/Mission 확인 없이 "상태가 changing이면 바로 요청"한다는 정책을 반영
            // ============================================================
            foreach (var elevator in elevators)
            {
                if (elevator == null) continue;

                // 이 함수는 "NOTAGVMODE_CHANGING_AGVMODE" 인 엘리베이터만 처리한다.
                if (elevator.mode != nameof(ElevatorMode.NOTAGVMODE_CHANGING_AGVMODE))
                    continue;

                var Request_Patch = new Request_Patch_ElevatorDto
                {
                    elevatorId = elevator.id,
                    action = "APPLY",
                    targetMode = nameof(ElevatorMode.AGVMODE)
                };

                // ============================================================
                //  사용자 요청: 아래 Patch 결과 처리 코드는 "그대로" 사용
                // ============================================================
                var modeChangePatch = serviceApi.Api.Patch_Elevator_ModeChange_Async(Request_Patch).Result;
                if (modeChangePatch != null)
                {
                    //[조건5] 상태코드 200~300 까지는 완료 처리
                    if (modeChangePatch.statusCode >= 200 && modeChangePatch.statusCode < 300)
                    {
                        EventLogger.Info($"[PatchModeChange][ELEVATOR][Success], Message = {modeChangePatch.statusText}, ElevatorId = {Request_Patch.elevatorId}, Action = {Request_Patch.action}" +
                                         $", TargetMode = {Request_Patch.targetMode}");
                    }
                    else EventLogger.Warn($"[PatchModeChange][ELEVATOR][Success], Message = {modeChangePatch.statusText}, ElevatorId = {Request_Patch.elevatorId}, Action = {Request_Patch.action}" +
                                         $", TargetMode = {Request_Patch.targetMode}");
                }
                else EventLogger.Warn($"[PatchModeChange][ELEVATOR][APIResponseIsNull]  ElevatorId = {Request_Patch.elevatorId}, Action = {Request_Patch.action}" +
                                         $", TargetMode = {Request_Patch.targetMode}");
            }

            EventLogger.Info("[HandleChangingToAgvMode][END]");
        }
    }
}