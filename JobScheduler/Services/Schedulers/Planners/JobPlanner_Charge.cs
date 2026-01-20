using Common.Models.Jobs;

namespace JOB.Services
{
    public partial class SchedulerService
    {
        public void ChargeJobs()
        {
            // ------------------------------------------------------------
            // 1) 배터리 설정값 로드
            //    - 충전 정책(최소 배터리, 스위칭 기준, 시작/종료 배터리)을 한 번만 읽어온다.
            // ------------------------------------------------------------
            var batterySetting = _repository.Battery.GetAll();   // 단일 설정 객체라고 가정

            if (batterySetting == null)
            {
                // 배터리 설정이 없으면 충전 정책을 적용할 수 없으므로 로직 종료
                EventLogger.Info("[CHARGE][BUILD] battery setting not found → exit");
                return;
            }

            double minimum = batterySetting.minimum;         // Job을 받을 수 있는 최소 배터리
            double crossCharge = batterySetting.crossCharge; // 스위칭 기준 배터리
            double chargeStart = batterySetting.chargeStart; // 충전 시작 기준 배터리
            double chargeEnd = batterySetting.chargeEnd;     // 충전 종료 기준 배터리

            //EventLogger.Info("[CHARGE][BUILD] start charge decision");

            // ------------------------------------------------------------
            // 2) 충전기(Charger) Position 필터링
            //    조건:
            //      - subType == CHARGE
            //      - hasCharger == true
            //      - isEnabled == true
            //    ※ GetAll() 결과가 null 일 수 있으므로 방어 코드 포함
            // ------------------------------------------------------------

            // 2-1) 전체 포지션 목록 로드
            // 충전용 포지션
            // 실제 충전기 존재
            // 사용 가능 상태
            var chargerPositions = _repository.Positions.GetAll().Where(r => r.subType == nameof(PositionSubType.CHARGE) && r.nodeType == nameof(NodeType.CHARGER)
                                                                     && r.isEnabled == true).ToList();

            // 2-3) 충전기가 하나도 없으면 이번 사이클은 아무 일도 하지 않고 종료
            if (chargerPositions == null || chargerPositions.Count == 0)
            {
                //EventLogger.Info("[CHARGE][BUILD] no available chargers (subType=CHARGE) → exit");
                return;
            }

            // ------------------------------------------------------------
            // 3-2) 충전 중인 Subscribe_Worker 목록 만들기
            //      - 기준:
            //        1) _repository.Jobs.GetByWorkerId(workerId) 로 Job 목록 조회
            //        2) Job.type == CHARGE 또는 Job.subType == CHARGE
            //        3) Job.state == JobState.INPROGRESS
            //      - 위 조건을 만족하는 Job 이 하나라도 있으면 "현재 충전 중"으로 판단
            // ------------------------------------------------------------
            var workers = _repository.Workers.MiR_GetByActive();
            List<Worker> chargingWorkers = new List<Worker>();

            foreach (var worker in workers)
            {
                if (worker == null)
                {
                    continue;
                }

                bool isCharging = IsWorkerCharging(worker.id);
                if (isCharging)
                {
                    chargingWorkers.Add(worker);
                }
            }
            // ------------------------------------------------------------
            // 3-1) IDLE 상태 Subscribe_Worker 목록 만들기
            //      - Subscribe_Worker.state == WorkerState.IDLE 인 로봇만 선별
            //      - 실제로 새 충전 Job을 받을 수 있는 후보들
            // ------------------------------------------------------------
            var idleWorkers = workers.Where(r => r.state == nameof(WorkerState.IDLE)).ToList();

            // ------------------------------------------------------------
            // 4) 이번 사이클에서 이미 Job 계획된 Subscribe_Worker / 충전기를 추적하기 위한 Set
            //    - 중복 Job 생성 방지 목적
            // ------------------------------------------------------------
            List<string> workerPlanned = new List<string>();   // CHARGE/WAIT Job 계획된 Subscribe_Worker Id
            List<string> chargerPlanned = new List<string>();  // 이번 사이클에 할당된 충전기 Position Id

            // ------------------------------------------------------------
            // 5) 세부 단계 처리 (아직 내부 로직은 미구현 상태)
            //    - 아래 함수들 안에서 실제 Job 생성( JobType.CHARGE / JobType.WAIT )을 진행할 예정
            // ------------------------------------------------------------
            // 5-1) 전용 충전기(linkedRobotId) 처리
            HandleReservedChargerChargeStart(idleWorkers, chargingWorkers, chargerPositions, chargeStart, workerPlanned, chargerPlanned);

            // 5-2) 일반 충전기 충전 시작 처리
            HandleNormalChargerChargeStart(idleWorkers, chargingWorkers, chargerPositions, minimum, chargeStart, workerPlanned, chargerPlanned);

            // 5-3) 스위칭(crossCharge) 처리
            HandleCrossCharge(idleWorkers, chargingWorkers, chargerPositions, crossCharge, chargeEnd, workerPlanned, chargerPlanned);

            // 5-4) 스위칭 없이 단순 충전 완료 처리
            HandleChargeCompleteWithoutCross(chargingWorkers, chargerPositions, chargeEnd, workerPlanned);

            //EventLogger.Info("[CHARGE][BUILD] finish charge decision cycle");
        }

        /// <summary>
        /// 특정 Subscribe_Worker 가 "현재 충전 중"인지 판단하는 함수
        /// - 조건:
        ///   1) _repository.Jobs.GetByWorkerId(workerId)를 통해 해당 Worker의 Job 전체를 가져온다.
        ///   2) 그 Job 중에서
        ///        - type == CHARGE 또는 subType == CHARGE
        ///        - state == INPROGRESS
        ///      조건을 모두 만족하는 Job 이 하나라도 있으면 "충전 중"으로 판단.
        /// </summary>
        private bool IsWorkerCharging(string workerId)
        {
            // 1) WorkerId 유효성 확인
            if (string.IsNullOrEmpty(workerId))
            {
                return false;
            }

            // 2) 해당 Worker에게 할당된 모든 Job 조회
            var allJobs = _repository.Jobs.GetByWorkerId(workerId);
            if (allJobs == null || allJobs.Count == 0)
            {
                return false;
            }

            // 3) 충전 중 Job(INPROGRESS) 필터링
            var jobs = allJobs.Where(j => j != null && j.state != nameof(JobState.COMPLETED)
                                    && (j.type == nameof(JobType.CHARGE) || j.subType == nameof(JobSubType.CHARGE))).ToList();

            // 4) 충전 Job 존재 여부 판단
            if (jobs == null || jobs.Count == 0)
            {
                return false;
            }

            // 5) (디버그 목적) 실제 충전 중인 Job 로그 — 필요한 경우 주석 해제
            //foreach (var job in jobs)
            //{
            //    EventLogger.Info(
            //        "[CHARGE][CHECK] worker is charging: workerId=" + workerId +
            //        ", jobId=" + job.guid +
            //        ", type=" + job.type +
            //        ", subType=" + job.subType +
            //        ", state=" + job.state
            //    );
            //}

            return true;
        }

        /// <summary>
        /// 전용 충전기(linkedRobotId)가 설정된 충전기를 처리하는 함수
        /// - 충전기 Position.linkedRobotId 에 특정 WorkerId 가 지정된 경우,
        ///   해당 Subscribe_Worker 전용 충전기로 간주한다.
        /// - 조건:
        ///   1) 충전기: subType == CHARGE, hasCharger == true, isEnabled == true (이미 필터링된 상태)
        ///   2) linkedRobotId 가 비어있지 않은 충전기만 대상
        ///   3) linkedRobotId 와 일치하는 Subscribe_Worker 가 idleWorkers 에 있어야 한다.
        ///   4) Subscribe_Worker 배터리 <= chargeStart 일 때만 CHARGE Job 생성
        ///   5) chargerPositions 중 해당 충전기가 isOccupied == false 여야 한다.
        ///   6) 같은 사이클 내에서 이미 Job 계획된 Worker/Charger 는 workerPlanned, chargerPlanned 로 중복 방지
        /// </summary>
        private void HandleReservedChargerChargeStart(List<Worker> idleWorkers, List<Worker> chargingWorkers, List<Position> chargerPositions
                                                        , double chargeStart, List<string> workerPlanned, List<string> chargerPlanned)
        {
            // 방어 코드: 입력 컬렉션이 null 인 경우 빈 리스트로 간주
            if (idleWorkers == null || idleWorkers.Count == 0) return;

            if (chargerPositions == null) return;

            if (chargingWorkers == null) return;

            if (workerPlanned == null) return;

            if (chargerPlanned == null) return;

            // ------------------------------------------------------------
            // 1) linkedRobotId 가 설정된 "전용 충전기" 목록 필터링
            //    - linkedRobotId 가 null/빈 문자열이 아닌 충전기만 대상으로 한다.
            // ------------------------------------------------------------
            var reservedChargers = chargerPositions.Where(c => c != null && !string.IsNullOrEmpty(c.linkedRobotId)).ToList();

            if (reservedChargers == null || reservedChargers.Count == 0)
            {
                // 전용 충전기가 하나도 없으면 바로 리턴
                //EventLogger.Info("[CHARGE][RESERVED] no reserved chargers (linkedRobotId set) → skip");
                return;
            }

            List<Worker> candidateWorkers = new List<Worker>();

            foreach (var idleWorker in idleWorkers)
            {
                var chargingWorker = chargingWorkers.FirstOrDefault(r => r.id == idleWorker.id);
                if (chargingWorker == null)
                {
                    candidateWorkers.Add(idleWorker);
                }
            }

            // ------------------------------------------------------------
            // 2) 전용 충전기 하나씩 검사
            //    - 각 충전기에 대해:
            //      1) 이미 이번 사이클에서 사용 계획된 충전기인지 확인
            //      2) linkedRobotId 와 매칭되는 IDLE Subscribe_Worker 찾기
            //      3) Subscribe_Worker 배터리 / 충전기 점유 상태 / 그룹 조건 확인
            //      4) 조건을 만족하면 CHARGE Job 생성
            // ------------------------------------------------------------
            foreach (var chargerPosition in reservedChargers)
            {
                if (chargerPosition == null)
                {
                    EventLogger.Warn("[CHARGE][RESERVED][SKIP] chargerPosition null detected");
                    continue;
                }

                // 2-1) 이 충전기가 이미 다른 Worker에게 할당된 경우 스킵
                //      - 중복 할당 방지
                if (chargerPlanned.Contains(chargerPosition.id))
                {
                    EventLogger.Warn($"[CHARGE][RESERVED][SKIP] charger already planned in cycle: chargerPOSName= {chargerPosition.name}, chargerId= {chargerPosition.id}");
                    continue;
                }
                // 2-2) linkedRobotId 와 일치하는 IDLE Subscribe_Worker 찾기
                //      - 같은 그룹인 Subscribe_Worker 만 대상 (충전기 group == Subscribe_Worker.group)
                var targetWorker = candidateWorkers.FirstOrDefault(w => w != null && w.id == chargerPosition.linkedRobotId);

                if (targetWorker == null)
                {
                    // 전용 충전기에 매칭되는 IDLE Subscribe_Worker 가 없으면 스킵
                    //EventLogger.Warn($"[CHARGE][RESERVED][SKIP] no idle worker for reserved charger: chargerPOSName={chargerPosition.name}, chargerId={chargerPosition.id}" +
                    //                 $", linkedWorkerId={chargerPosition.linkedRobotId}");
                    continue;
                }

                // 2-3) 이번 사이클에서 이미 Job 이 계획된 Subscribe_Worker 인지 확인
                if (workerPlanned.Contains(targetWorker.id))
                {
                    // 한 사이클에 동일 Worker에게 중복으로 CHARGE/WAIT Job 을 만들지 않기 위한 방어
                    EventLogger.Warn($"[CHARGE][RESERVED][SKIP] worker already planned in cycle: workerId={targetWorker.id}, workerName={targetWorker.name}");
                    continue;
                }

                // 2-4) 배터리 조건 확인
                //      - 전용 충전기는 "충전 시작 기준 배터리 이하"일 때만 충전 Job 생성
                if (targetWorker.batteryPercent > chargeStart)
                {
                    //EventLogger.Warn($"[CHARGE][RESERVED][SKIP] battery higher than chargeStart: workerId={targetWorker.id}, workerName={targetWorker.name}" +
                    //                 $", battery={targetWorker.batteryPercent}, chargeStart={chargeStart}%");
                    continue;
                }

                // 2-5) 충전기 점유 상태 확인
                //      - isOccupied == true 이면 다른 로봇이 이미 점유 중이므로 스킵
                if (chargerPosition.isOccupied)
                {
                    EventLogger.Warn($"[CHARGE][RESERVED][SKIP] charger occupied: chargerId={chargerPosition.id}, chargerName={chargerPosition.name}" +
                                     $", linkedWorkerId={chargerPosition.linkedRobotId}");
                    continue;
                }

                // --------------------------------------------------------
                // 3) 모든 조건을 만족하므로, 전용 충전기용 CHARGE Job 생성
                //    - Job 생성 로직은 프로젝트 Job 모델에 맞게 구현해야 한다.
                //    - 아래 CreateReservedChargeJob 헬퍼에서 Job 을 구성하고
                //      _repository.Jobs 에 추가하는 패턴으로 작성한다.
                // --------------------------------------------------------

                bool chargeJob = CreateChargeJob(targetWorker, chargerPosition, ChargeCreateType.Reserved);

                if (chargeJob == false)
                {
                    // Job 생성에 실패한 경우 (예외 방지용)
                    EventLogger.Error($"[CHARGE][RESERVED][ERROR] failed to create charge job: workerName={targetWorker.name}, workerId={targetWorker.id}" +
                                      $", chargerPOSName={chargerPosition.name}, chargerId={chargerPosition.id}");
                    continue;
                }

                // 이번 사이클에서 이 Subscribe_Worker / Charger 는 이미 처리되었음을 기록
                if (!workerPlanned.Contains(targetWorker.id))
                {
                    workerPlanned.Add(targetWorker.id);
                }

                if (!chargerPlanned.Contains(chargerPosition.id))
                {
                    chargerPlanned.Add(chargerPosition.id);
                }

                // 최종 할당 로그
                EventLogger.Info($"[CHARGE][RESERVED][ASSIGN] workerId={targetWorker.id}, workerName={targetWorker.name}, group={targetWorker.group}" +
                                 $", battery={targetWorker.batteryPercent}, chargerPositionId={chargerPosition.id}, chargerName={chargerPosition.name}" +
                                 $", linkedWorkerId={chargerPosition.linkedRobotId}, jobType={nameof(JobType.CHARGE)}, jobSubType={nameof(JobSubType.CHARGE)}"
);
            }
        }

        /// <summary>
        /// 일반 충전기(CHARGE, linkedRobotId 없음)에 대해 충전 Job을 생성하는 함수
        /// - 전용 충전기가 아닌 충전기들을 대상으로 한다.
        /// - 조건:
        ///   1) chargerPosition.linkedRobotId 가 비어 있어야 한다. (전용 충전기 제외)
        ///   2) 충전기 isOccupied == false 여야 한다.
        ///   3) 같은 그룹(worker.group == charger.group) 이고 같은 층(worker.mapId == charger.mapId) 인 IDLE Subscribe_Worker 대상
        ///   4) Subscribe_Worker 배터리 <= chargeStart 인 경우에만 충전 Job 생성
        ///   5) 같은 사이클 내에서 이미 Job 이 계획된 Worker/Charger 는 workerPlanned / chargerPlanned 로 중복 방지
        /// </summary>
        private void HandleNormalChargerChargeStart(List<Worker> idleWorkers, List<Worker> chargingWorkers, List<Position> chargerPositions
                                                  , double minimum, double chargeStart, List<string> workerPlanned, List<string> chargerPlanned)
        {
            // 방어 코드: null 이면 빈 리스트로 대체
            if (idleWorkers == null || idleWorkers.Count == 0) return;

            if (chargerPositions == null) return;

            if (chargingWorkers == null) return;

            if (workerPlanned == null) return;

            if (chargerPlanned == null) return;

            // minimum 값은 "일반 Job 수신 최소 배터리" 정책으로,
            // 여기서는 충전 Job 생성에는 직접 사용하지 않고,
            // 필요 시 정책을 확장할 수 있도록 파라미터만 유지한다.

            // ------------------------------------------------------------
            // 1) 일반 충전기 목록 필터링
            //    - linkedRobotId 가 비어 있는 충전기만 대상
            // 전용 충전기 제외
            // ------------------------------------------------------------
            var normalChargers = chargerPositions.Where(c => c != null && string.IsNullOrEmpty(c.linkedRobotId)).ToList();

            if (normalChargers == null || normalChargers.Count == 0)
            {
                EventLogger.Info($"[CHARGE][NORMAL] no normal chargers (linkedRobotId empty) → skip");
                return;
            }

            List<Worker> candidateWorkers = new List<Worker>();

            foreach (var idleWorker in idleWorkers)
            {
                var chargingWorker = chargingWorkers.FirstOrDefault(r => r.id == idleWorker.id);
                if (chargingWorker == null)
                {
                    candidateWorkers.Add(idleWorker);
                }
            }
            // ------------------------------------------------------------
            // 2) 일반 충전기 하나씩 검사
            //    - 각 충전기에 대해:
            //      1) 이미 이번 사이클에 사용된 충전기인지 확인
            //      2) 같은 그룹/층의 IDLE Subscribe_Worker 중에서 배터리가 낮고 chargeStart 이하인 Subscribe_Worker 찾기
            //      3) 충전기 점유 상태 확인
            //      4) 조건을 만족하면 CHARGE Job 생성 요청
            // ------------------------------------------------------------
            foreach (var charger in normalChargers)
            {
                if (charger == null)
                {
                    EventLogger.Warn($"[CHARGE][NORMAL][SKIP] charger position null detected");
                    continue;
                }

                // 2-1) 이번 사이클에서 이미 사용된 충전기인지 확인
                if (chargerPlanned.Contains(charger.id))
                {
                    EventLogger.Warn($"[CHARGE][NORMAL][SKIP] charger already planned in cycle: chargerId={charger.id}, chargerPOSName={charger.name}");
                    continue;
                }

                // 2-2) 충전기 점유 상태 확인
                if (charger.isOccupied)
                {
                    //EventLogger.Warn($"[CHARGE][NORMAL][SKIP] charger occupied: chargerId={charger.id}, chargerPOSName={charger.name}");
                    continue;
                }

                // 2-3) 이 충전기와 같은 그룹/층이면서,
                //      - IDLE 상태
                //      - 이번 사이클에 아직 Job 이 계획되지 않았고
                //      - 배터리가 chargeStart 이하인 Subscribe_Worker 후보 목록 생성
                // 배터리 낮은 순으로 정렬 (가장 급한 로봇 우선)

                candidateWorkers = candidateWorkers.Where(w => w != null && !workerPlanned.Contains(w.id) && w.group == charger.group
                                                     && w.batteryPercent <= chargeStart).OrderBy(w => w.batteryPercent).ToList();

                if (candidateWorkers.Count == 0)
                {
                    //EventLogger.Info(
                    //    $"[CHARGE][NORMAL][SKIP] no idle worker matched: chargerPOSId={charger.id}, chargerPOSName={charger.name}, " +
                    //    $"group={charger.group}, mapId={charger.mapId}, chargeStart={chargeStart}%"
                    //);
                    continue;
                }

                // 2-4) 가장 배터리가 낮은 Subscribe_Worker 선택
                var targetWorker = candidateWorkers.FirstOrDefault();
                if (targetWorker == null)
                {
                    EventLogger.Warn($"[CHARGE][NORMAL][SKIP] first candidate worker is null: chargerPOSId={charger.id}, chargerPOSName={charger.name}");
                    continue;
                }

                // --------------------------------------------------------
                // 3) 일반 충전기용 CHARGE Job 생성 요청
                // --------------------------------------------------------
                bool created = CreateChargeJob(targetWorker, charger, ChargeCreateType.Normal);

                if (!created)
                {
                    EventLogger.Error($"[CHARGE][NORMAL][ERROR] failed to enqueue charge job: workerId={targetWorker.id}, workerName={targetWorker.name}, chargerPOSId={charger.id}" +
                                      $", chargerPOSName={charger.name}");
                    continue;
                }

                // 3-1) 이번 사이클에 처리 완료된 Subscribe_Worker / Charger 기록 (중복 방지)
                if (!workerPlanned.Contains(targetWorker.id))
                {
                    workerPlanned.Add(targetWorker.id);
                }

                if (!chargerPlanned.Contains(charger.id))
                {
                    chargerPlanned.Add(charger.id);
                }

                // 3-2) 최종 할당 로그
                EventLogger.Info(
                    $"[CHARGE][NORMAL][ASSIGN] workerId={targetWorker.id}, workerName={targetWorker.name}, group={targetWorker.group}" +
                    $", battery={targetWorker.batteryPercent}, chargerPositionId={charger.id}, chargerName={charger.name}" +
                    $", mapId={charger.mapId}, jobType={nameof(JobType.CHARGE)}, jobSubType={nameof(JobSubType.CHARGE)}"
                );
            }
        }

        /// <summary>
        /// 충전(CHARGE) Job 생성 요청 공용 함수
        /// - 전용 / 일반 충전기 구분은 createType 으로 처리
        /// - 실제 Job 생성은 Queue에서 처리
        /// - 리턴값:
        ///   true  : Queue에 Job 생성 요청 성공
        ///   false : 파라미터 오류 또는 예외 발생
        /// </summary>
        private bool CreateChargeJob(Worker worker, Position chargerPosition, ChargeCreateType createType)
        {
            // ------------------------------------------------------------
            // 1) 기본 방어 코드
            // ------------------------------------------------------------
            if (worker == null)
            {
                EventLogger.Error(
                    $"[CHARGE][{createType}][CREATE][ERROR] worker is null → job creation aborted"
                );
                return false;
            }

            if (chargerPosition == null)
            {
                EventLogger.Error(
                    $"[CHARGE][{createType}][CREATE][ERROR] charger position is null → job creation aborted"
                );
                return false;
            }
            // ------------------------------------------------------------
            // 2) 층(mapId) 다르면 엘리베이터 상태 확인 (추가)
            // ------------------------------------------------------------
            bool isCrossMap = !string.IsNullOrEmpty(worker.mapId)
                              && !string.IsNullOrEmpty(chargerPosition.mapId)
                              && worker.mapId != chargerPosition.mapId;

            if (isCrossMap)
            {
                bool elevatorActive = _repository.Elevator.Active("NO1");
                if (elevatorActive == false)
                {
                    EventLogger.Warn(
                        $"[CHARGE][{createType}][ELEVATOR][SKIP] elevator inactive → cannot send cross-map charge. " +
                        $"workerId={worker.id}, workerName={worker.name}, workerMapId={worker.mapId}, " +
                        $"chargerId={chargerPosition.id}, chargerName={chargerPosition.name}, chargerMapId={chargerPosition.mapId}"
                    );
                    return false; // 여기서 막아야 뒤 로직 진행 안 함
                }
            }

            // ------------------------------------------------------------
            // 3) Queue 를 통한 Job 생성 요청
            // ------------------------------------------------------------

            _Queue.Create_Job(worker.group, null, nameof(JobType.CHARGE), nameof(JobSubType.CHARGE), null, 0, null,
                             null, null, null, chargerPosition.id, chargerPosition.name, chargerPosition.linkedFacility
                             , worker.id);
            updateOccupied(chargerPosition, true, 0.5);

            // --------------------------------------------------------
            // 3) 생성 요청 성공 로그
            // --------------------------------------------------------
            EventLogger.Info(
                $"[CHARGE][{createType}][CREATE] enqueue charge job request: " +
                $"workerId={worker.id}, workerName={worker.name}, group={worker.group}, battery={worker.batteryPercent}, " +
                $"chargerPositionId={chargerPosition.id}, chargerName={chargerPosition.name}, mapId={chargerPosition.mapId}"
            );

            return true;
        }

        /// <summary>
        /// 교차 충전(Cross Charge) 처리
        /// - 이미 충전 중인 Subscribe_Worker(충전기 점유)가 있고,
        ///   같은 그룹/같은 층(mapId)에 배터리가 더 낮은 IDLE Worker가 존재하면
        ///   충전 중 Worker가 crossCharge 이상일 때 충전을 양보한다.
        ///
        /// 처리:
        /// 1) 충전 중 Worker가 점유한 충전기(CHARGE 포지션)를 찾는다.
        /// 2) 같은 그룹/같은 층에서 배터리 낮은 IDLE Subscribe_Worker(<= crossCharge)를 찾는다.
        /// 3) 조건 만족 시
        ///    - 충전 중 Worker → WAIT Job 생성 (지정 WAIT 우선, 없으면 가까운 WAIT)
        ///    - 저배터리 Worker → CHARGE Job 생성 (같은 충전기로)
        ///
        /// 주의:
        /// - 전용 충전기(linkedRobotId 설정)는 다른 로봇으로 교체 불가
        /// - 한 사이클에 교차 충전은 1회만 수행(불안정 방지)
        /// </summary>
        private void HandleCrossCharge(List<Worker> idleWorkers, List<Worker> chargingWorkers, List<Position> chargerPositions, double crossCharge, double chargeEnd
                                     , List<string> workerPlanned, List<string> chargerPlanned)
        {
            // ------------------------------------------------------------
            // 0) 방어 코드
            // ------------------------------------------------------------
            if (idleWorkers == null || idleWorkers.Count == 0) return;

            if (chargerPositions == null) return;

            if (chargingWorkers == null) return;

            if (workerPlanned == null) return;

            if (chargerPlanned == null) return;

            // ------------------------------------------------------------
            // 1) 교차 충전 대상이 될 수 있는 "저배터리 IDLE Subscribe_Worker" 후보 생성
            //    - batteryPercent <= crossCharge
            //    - 이번 사이클에 이미 계획된 worker 제외
            //    - 배터리 낮은 순 정렬
            // ------------------------------------------------------------
            var lowBatteryIdleWorkers = idleWorkers.Where(w => w != null && !workerPlanned.Contains(w.id) && w.batteryPercent <= crossCharge).OrderBy(w => w.batteryPercent).ToList();

            if (lowBatteryIdleWorkers == null || lowBatteryIdleWorkers.Count == 0)
            {
                //EventLogger.Info($"[CHARGE][CROSS] no idle workers under crossCharge={crossCharge}% → skip");
                return;
            }

            // ------------------------------------------------------------
            // 2) 충전 중 Worker를 하나씩 확인하여 교차 충전 실행 여부 판단
            // ------------------------------------------------------------
            foreach (var chargingWorker in chargingWorkers)
            {
                if (chargingWorker == null)
                {
                    continue;
                }

                // 2-1) 이번 사이클에 이미 계획된 충전 중 Worker는 스킵
                if (workerPlanned.Contains(chargingWorker.id))
                {
                    EventLogger.Warn($"[CHARGE][CROSS][SKIP] charging worker already planned in cycle: workerId={chargingWorker.id}, workerName={chargingWorker.name}");
                    continue;
                }


                // 2-3) 충전 중 Worker가 crossCharge 이상 이고 충전 완료 배터리 이상이어야 양보 가능
                if (chargingWorker.batteryPercent < crossCharge || chargingWorker.batteryPercent < chargeEnd)
                {
                    continue;
                }

                // --------------------------------------------------------
                // 2-4) 충전 중 Worker가 점유한 충전기 포지션(CHARGE)을 찾는다.
                //      - Subscribe_Worker.PositionId(현재 점유 포지션) == ChargerPosition.id
                // --------------------------------------------------------

                var chargingMissions = _repository.Missions.GetByAssignedWorkerId(chargingWorker.id)
                                        .Where(r => r.subType == nameof(MissionSubType.CHARGERMOVE) || r.subType == nameof(MissionSubType.CHARGE)).ToList();
                var runChargingMission = _repository.Missions.GetByRunMissions(chargingMissions);

                if (runChargingMission == null)
                {
                    continue;
                }
                var runChargingParameta = _repository.Missions.GetParametas(runChargingMission).Where(r => r.key == "target").Select(r => r.value).FirstOrDefault();
                if (string.IsNullOrEmpty(runChargingParameta))
                {
                    continue;
                }

                var usingCharger = chargerPositions.FirstOrDefault(p => p.id == runChargingParameta);

                if (usingCharger == null)
                {
                    EventLogger.Warn($"[CHARGE][CROSS][SKIP] cannot find using charger by worker position: workerId={chargingWorker.id}, workerName={chargingWorker.name}" +
                                     $", workerPosId={chargingWorker.PositionId}");
                    continue;
                }

                // 2-5) 이번 사이클에 이미 이 충전기가 계획되었으면 스킵
                if (chargerPlanned.Contains(usingCharger.id))
                {
                    EventLogger.Warn($"[CHARGE][CROSS][SKIP] charger already planned in cycle: chargerPOSId={usingCharger.id}, chargerPOSName={usingCharger.name}");
                    continue;
                }

                // --------------------------------------------------------
                // 2-6) 같은 그룹/같은 층(mapId)에서 교차 충전 받을 저배터리 로봇 선택
                // --------------------------------------------------------
                var targetLowWorker = lowBatteryIdleWorkers.FirstOrDefault(w => w != null && w.group == usingCharger.group && w.mapId == usingCharger.mapId);

                if (targetLowWorker == null)
                {
                    // 이 충전기 기준으로 교차 충전 받을 로봇이 없음
                    continue;
                }

                // --------------------------------------------------------
                // 2-7) 전용 충전기(linkedRobotId) 정책 적용
                //      - linkedRobotId 가 설정된 충전기는 지정된 로봇만 사용 가능
                // --------------------------------------------------------
                if (!string.IsNullOrEmpty(usingCharger.linkedRobotId))
                {
                    if (usingCharger.linkedRobotId != targetLowWorker.id)
                    {
                        EventLogger.Warn($"[CHARGE][CROSS][SKIP] reserved charger cannot switch: chargerPOSName = {usingCharger.name}, chargerPOSId={usingCharger.id}" +
                                         $", linkedWorkerId={usingCharger.linkedRobotId}, targetWorkerId={targetLowWorker.id}");
                        continue;
                    }
                }

                // --------------------------------------------------------
                // 2-8) WAIT 포지션 선택 (지정 WAIT 우선, 없으면 가까운 WAIT)
                //      - WAIT 조건: subType=WAIT, isEnabled=true, isOccupied=false
                // --------------------------------------------------------
                //Position waitPosition = FindWaitPositionForWorker(chargingWorker, usingCharger.group, usingCharger.mapId);

                //if (waitPosition == null)
                //{
                //    EventLogger.Warn($"[CHARGE][CROSS][SKIP] no available wait position: workerId={chargingWorker.id}, workerName={chargingWorker.name}" +
                //                     $"usingChargerPOSName = {usingCharger.name}, group={usingCharger.group}, mapId={usingCharger.mapId}");
                //    continue;
                //}

                // --------------------------------------------------------
                // 3) 교차 충전 실행 (순서 중요)
                //    1) 충전 중 Subscribe_Worker → WAIT Job 생성
                //    2) 저배터리 Subscribe_Worker → CHARGE Job 생성 (같은 충전기)
                // --------------------------------------------------------

                // 3) 교차 충전 실행 직전 (가장 먼저 충전 미션 삭제)
                //bool deleted = DeleteChargingMissionOrStop(chargingWorker.id, "CHARGE][CROSS");
                //if (!deleted)
                //{
                //    // 삭제 실패면 이후 로직 진행 금지
                //    EventLogger.Error($"[CHARGE][CROSS][STOP] cannot proceed because mission delete failed: workerId={chargingWorker.id}, workerName={chargingWorker.name}");
                //    continue;
                //}

                //bool waitCreated = CreateWaitJob(chargingWorker, waitPosition);

                //if (!waitCreated)
                //{
                //    EventLogger.Error($"[CHARGE][CROSS][ERROR] failed to enqueue WAIT job: workerId={chargingWorker.id}, workerName={chargingWorker.name}, waitPOSId={waitPosition.id}" +
                //                     $", waitPOSName={waitPosition.name}");
                //    continue;
                //}

                //충전중인 Worker는 삭제Cancel 까지만 진행
                //WaitJob은 WaitJob생성루틴에서 진행
                ChangeWaitDeleteJob(chargingWorker, "[CHARGE][CROSS]");

                bool chargeCreated = CreateChargeJob(targetLowWorker, usingCharger, ChargeCreateType.Cross);

                if (!chargeCreated)
                {
                    EventLogger.Error($"[CHARGE][CROSS][ERROR] failed to enqueue CHARGE job: workerId={targetLowWorker.id}, workerName={targetLowWorker.name}, chargerPOSId={usingCharger.id}, chargerPOSName={usingCharger.name}");
                    continue;
                }

                // --------------------------------------------------------
                // 4) 이번 사이클 중복 방지 기록
                // --------------------------------------------------------
                if (!workerPlanned.Contains(chargingWorker.id))
                {
                    workerPlanned.Add(chargingWorker.id);
                }

                if (!workerPlanned.Contains(targetLowWorker.id))
                {
                    workerPlanned.Add(targetLowWorker.id);
                }

                if (!chargerPlanned.Contains(usingCharger.id))
                {
                    chargerPlanned.Add(usingCharger.id);
                }

                // --------------------------------------------------------
                // 5) 교차 충전 성공 로그
                // --------------------------------------------------------
                EventLogger.Info(
                    $"[CHARGE][CROSS][SWITCH] fromWorkerId={chargingWorker.id}, fromWorkerName={chargingWorker.name}, fromBattery={chargingWorker.batteryPercent}" +
                    $", toWorkerId={targetLowWorker.id}, toWorkerName={targetLowWorker.name}, toBattery={targetLowWorker.batteryPercent}" +
                    $", chargerPOSId={usingCharger.id}, chargerPOSName={usingCharger.name}, group={usingCharger.group}, mapId={usingCharger.mapId}" +
                    $", crossCharge={crossCharge}%, chargeEnd={chargeEnd}%"
                );

                // 한 사이클에 교차 충전은 1회만 수행
                return;
            }

            //EventLogger.Info($"[CHARGE][CROSS] no switch executed → finish");
        }

        /// <summary>
        /// 스위칭(교차 충전) 없이 "충전 완료" 처리
        /// - 충전 중인 Worker가 chargeEnd 이상이면 충전을 종료하고 WAIT 위치로 이동시킨다.
        /// - WAIT 위치 선택 규칙:
        ///   1) 지정 WAIT( PositionSubType.WAIT + linkedRobotId == worker.id )가 있으면 우선
        ///   2) 없으면 같은 그룹/같은 층(mapId)에서 가장 가까운 WAIT
        ///   3) 공통 조건: isEnabled=true, isOccupied=false
        ///
        /// 주의:
        /// - Queue 기반이므로 JobId는 알 수 없고, CreateWaitJob의 성공/실패만 확인한다.
        /// - 이번 사이클 중복 생성 방지를 위해 workerPlanned 사용
        /// </summary>
        private void HandleChargeCompleteWithoutCross(List<Worker> chargingWorkers, List<Position> chargerPositions, double chargeEnd, List<string> workerPlanned)
        {
            // ------------------------------------------------------------
            // 0) 방어 코드
            // ------------------------------------------------------------

            if (chargingWorkers == null || chargingWorkers.Count == 0) return;

            if (chargerPositions == null) return;

            if (workerPlanned == null) return;

            // ------------------------------------------------------------
            // 1) 충전 중 Subscribe_Worker 순회
            // ------------------------------------------------------------
            foreach (var chargingWorker in chargingWorkers)
            {
                if (chargingWorker == null)
                {
                    continue;
                }

                // 1-1) 이번 사이클에 이미 계획된 Worker면 스킵
                if (workerPlanned.Contains(chargingWorker.id))
                {
                    EventLogger.Warn($"[CHARGE][COMPLETE][SKIP] worker already planned in cycle: workerId={chargingWorker.id}, workerName={chargingWorker.name}");
                    continue;
                }

                // 1-2) chargeEnd 이상이 아니면 아직 충전 완료 처리 대상 아님
                if (chargingWorker.batteryPercent < chargeEnd)
                {
                    continue;
                }

                // --------------------------------------------------------
                // 2) 이 Worker가 실제로 점유 중인 충전기(CHARGE 포지션) 찾기
                //    - Subscribe_Worker.PositionId == chargerPosition.id
                // --------------------------------------------------------
                //Position usingCharger = null;

                //if (!string.IsNullOrEmpty(chargingWorker.PositionId))
                //{
                //    usingCharger = chargerPositions.FirstOrDefault(p => p != null && p.id == chargingWorker.PositionId && p.subType == nameof(PositionSubType.CHARGE)
                //                                               && p.isEnabled == true);
                //}

                //if (usingCharger == null)
                //{
                //    // 충전 중이라고 판단했지만 포지션 매칭이 안 되는 경우
                //    EventLogger.Warn(
                //        $"[CHARGE][COMPLETE][SKIP] cannot find using charger by worker position: workerId={chargingWorker.id}, workerName={chargingWorker.name}, workerPosId={chargingWorker.PositionId}"
                //    );
                //    continue;
                //}

                // --------------------------------------------------------
                // 3) WAIT 포지션 선택 (지정 WAIT 우선, 없으면 가까운 WAIT)
                //    - 같은 그룹/같은 층(mapId)에서만 찾는다.
                // --------------------------------------------------------
                //Position waitPosition = FindWaitPositionForWorker(chargingWorker, chargingWorker.group, chargingWorker.mapId);

                //if (waitPosition == null)
                //{
                //    EventLogger.Warn(
                //        $"[CHARGE][COMPLETE][SKIP] no available wait position: workerId={chargingWorker.id}, workerName={chargingWorker.name}, group={chargingWorker.group}, mapId={chargingWorker.mapId}"
                //    );
                //    continue;
                //}

                // --------------------------------------------------------
                // 4) WAIT Job 생성 요청
                // --------------------------------------------------------
                // 4) WAIT Job 생성 전에 충전 미션 삭제
                //    Wait Job 경우 Wait job에서 진행

                //bool waitCreated = CreateWaitJob(chargingWorker, waitPosition);

                //if (!waitCreated)
                //{
                //    EventLogger.Error(
                //        $"[CHARGE][COMPLETE][ERROR] failed to enqueue WAIT job: workerId={chargingWorker.id}, workerName={chargingWorker.name}, waitPOSId={waitPosition.id}, waitPOSName={waitPosition.name}"
                //    );
                //    continue;
                //}

                // --------------------------------------------------------
                // 5) 이번 사이클 중복 방지 기록
                // --------------------------------------------------------
                if (!workerPlanned.Contains(chargingWorker.id))
                {
                    workerPlanned.Add(chargingWorker.id);
                }

                // --------------------------------------------------------
                // 6) 충전 완료 처리 로그
                // --------------------------------------------------------
                bool delete = ChangeWaitDeleteJob(chargingWorker, "[CHARGE][COMPLETE]");
                if (delete)
                {
                    EventLogger.Info(
                        $"[CHARGE][COMPLETE] workerId={chargingWorker.id}, workerName={chargingWorker.name}, battery={chargingWorker.batteryPercent}, chargeEnd={chargeEnd}%"
                    );
                }
            }

            //EventLogger.Info($"[CHARGE][COMPLETE] finish");
        }

        /// <summary>
        /// 해당 Worker가 현재 충전(CHARGE) 중인 JobGuid를 찾는다.
        /// 기준:
        /// - _repository.Jobs.GetByWorkerId(workerId)
        /// - state == INPROGRESS
        /// - type == CHARGE 또는 subType == CHARGE
        /// </summary>
        private string GetActiveChargeJobGuid(string workerId)
        {
            if (string.IsNullOrEmpty(workerId))
            {
                return null;
            }

            List<Job> jobs = _repository.Jobs.GetByWorkerId(workerId);
            if (jobs == null || jobs.Count == 0)
            {
                return null;
            }

            Job chargeJob = jobs.Where(j => j != null && j.state == nameof(JobState.INPROGRESS) && (j.type == nameof(JobType.CHARGE) || j.subType == nameof(JobSubType.CHARGE))).FirstOrDefault();

            if (chargeJob == null)
            {
                return null;
            }

            return chargeJob.guid;
        }
    }
}