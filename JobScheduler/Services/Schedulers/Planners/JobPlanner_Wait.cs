using Common.Models.Jobs;

namespace JOB.Services
{
    public partial class SchedulerService
    {
        /// <summary>
        /// WAIT 이동 1회 처리 함수
        /// ------------------------------------------------------------
        /// [목적]
        /// - 단 1회 판단/처리만 수행한다. (Retry/While 없음)
        /// - 외부 Loop(스케줄러)가 주기적으로 이 함수를 호출하는 구조를 전제로 한다.
        ///
        /// [처리 흐름]
        /// 1) Active Worker 목록 조회
        /// 2) 각 Worker별 조건 검사
        ///    - Invalid 상태 스킵
        ///    - 진행 중 Job 있으면 스킵
        ///    - IDLE 아니면 스킵
        ///    - 이미 WAIT 위치면 스킵
        /// 3) WAIT 목적지 선택 (전용 → 공용)
        /// 4) CreateWaitJob() 호출하여 Queue 등록
        /// </summary>
        public void WaitJobs()
        {
            // ------------------------------------------------------------
            // 0) 데이터 조회
            // ------------------------------------------------------------
            var workers = _repository.Workers.MiR_GetByActive();    // worker 정보
            var batterySetting = _repository.Battery.GetAll();      // 배터리 정보

            // 방어: worker 없으면 종료
            if (workers == null || workers.Count == 0)
            {
                //EventLogger.Warn($"[WAIT][LOOP][SKIP] no active workers");
                return;
            }

            // 방어: 배터리 설정 없으면 종료
            if (batterySetting == null)
            {
                EventLogger.Error($"[WAIT][LOOP][STOP] batterySetting is null");
                return;
            }

            // ------------------------------------------------------------
            // 1) Worker 순회
            // ------------------------------------------------------------
            foreach (var worker in workers)
            {
                // 방어: worker null 스킵
                if (worker == null)
                {
                    EventLogger.Warn($"[WAIT][LOOP][SKIP] worker is null in workers list");
                    continue;
                }

                // --------------------------------------------------------
                // 1-1) worker 상태가 처리 불가면 스킵
                // --------------------------------------------------------
                if (IsInvalid(worker.state))
                {
                    EventLogger.Warn($"[WAIT][CHECK][SKIP] invalid worker state: workerId={worker.id}, workerName={worker.name}, state={worker.state}");
                    continue;
                }

                // --------------------------------------------------------
                // 1-2) 기존 로직 유지:
                //      "할당 안 된 Job"이 존재 + 배터리 충분 → WAIT 이동 불필요
                // --------------------------------------------------------
                var jobFindNotAssignedWorker =
                    _repository.Jobs.GetAll()
                        .FirstOrDefault(j => j.group == worker.group && IsInvalid(j.assignedWorkerId));

                if (jobFindNotAssignedWorker != null && worker.batteryPercent > batterySetting.minimum)
                {
                    //EventLogger.Warn($"[WAIT][CHECK][SKIP] unassigned job exists and battery ok: workerId={worker.id}, workerName={worker.name}, battery={worker.batteryPercent}");
                    continue;
                }

                // --------------------------------------------------------
                // 1-3) worker 진행중 Job 존재하면 스킵
                // --------------------------------------------------------
                var jobFindAssignedWorker = _repository.Jobs.GetByWorkerId(worker.id).FirstOrDefault();
                if (jobFindAssignedWorker != null)
                {
                    //EventLogger.Warn($"[WAIT][CHECK][SKIP] worker already has job: workerId={worker.id}, workerName={worker.name}, jobGuid={jobFindAssignedWorker.guid}");
                    continue;
                }

                // --------------------------------------------------------
                // 1-4) IDLE 상태만 WAIT 이동 대상
                // --------------------------------------------------------
                if (worker.state != nameof(WorkerState.IDLE))
                {
                    //EventLogger.Warn($"[WAIT][CHECK][SKIP] worker not idle: workerId={worker.id}, workerName={worker.name}, state={worker.state}");
                    continue;
                }

                // --------------------------------------------------------
                // 1-5) 이미 WAIT 포지션이면 스킵
                // --------------------------------------------------------
                var waitPositionOccupieds = _repository.Positions.MiR_GetIsOccupied(null, nameof(PositionSubType.WAIT));
                var nowAtWait = waitPositionOccupieds?.FirstOrDefault(w => w.id == worker.PositionId);

                if (nowAtWait != null)
                {
                    //EventLogger.Info($"[WAIT][CHECK][SKIP] worker already at wait position: workerId={worker.id}, workerName={worker.name}, posId={worker.PositionId}");
                    continue;
                }

                // --------------------------------------------------------
                // 1-6) WAIT 목적지 선택 (전용 → 공용)
                // --------------------------------------------------------
                var destPosition = FindWaitPositionForWorker(worker, worker.group, worker.mapId);
                if (destPosition == null)
                {
                    EventLogger.Warn($"[WAIT][FIND][SKIP] no wait destination available: workerId={worker.id}, workerName={worker.name}");
                    continue;
                }

                // --------------------------------------------------------
                // 1-7) WAIT Job 생성 요청 (Queue 등록)
                // --------------------------------------------------------
                bool created = CreateWaitJob(worker, destPosition);

                // CreateWaitJob 내부에 성공/실패 로그가 이미 있으므로,
                // 여기서는 실패 시에만 추가 로그를 찍어도 된다.
                if (!created)
                {
                    EventLogger.Error($"[WAIT][LOOP][FAIL] create wait job failed: workerId={worker.id}, workerName={worker.name}, waitPOSId={destPosition.id}, waitPOSName={destPosition.name}");
                    continue;
                }

                // 운영 정책에 따라:
                // - 한 번에 한 로봇만 WAIT Job 만들고 break 할지
                // - 여러 로봇 동시에 만들지 결정 가능
                // 여기서는 기존처럼 "가능한 로봇은 모두 생성" 구조 유지
            }
        }

        /// <summary>
        /// Worker가 이동할 WAIT 포지션을 결정한다.
        /// 우선순위:
        /// 1) 지정 WAIT 포지션
        ///    - 조건: subType == WAIT, isEnabled == true, isOccupied == false
        ///    - 그리고 linkedRobotId == worker.id 로 지정된 경우
        /// 2) 지정 WAIT가 없으면 가까운 WAIT 포지션
        ///    - 같은 그룹/같은 층(mapId) 범위에서 후보를 모은 뒤
        ///    - Worker 현재 좌표와의 거리(제곱거리)로 가장 가까운 포지션 선택
        ///
        /// 공통 조건(모든 후보에 적용):
        /// - subType == WAIT
        /// - isEnabled == true
        /// - isOccupied == false
        /// - group == group
        /// - mapId == mapId
        /// </summary>
        private Position FindWaitPositionForWorker(Worker worker, string group, string mapId)
        {
            // ------------------------------------------------------------
            // 0) 방어 코드
            // ------------------------------------------------------------
            if (worker == null)
            {
                EventLogger.Warn($"[WAIT][FIND][SKIP] worker is null");
                return null;
            }

            if (string.IsNullOrEmpty(group))
            {
                EventLogger.Warn($"[WAIT][FIND][SKIP] group is empty: workerId={worker.id}, workerName={worker.name}");
                return null;
            }

            if (string.IsNullOrEmpty(mapId))
            {
                EventLogger.Warn($"[WAIT][FIND][SKIP] mapId is empty: workerId={worker.id}, workerName={worker.name}");
                return null;
            }

            // ------------------------------------------------------------
            // 1) 전체 Position 로드
            // ------------------------------------------------------------
            var allPositions = _repository.Positions.GetAll();
            if (allPositions == null)
            {
                EventLogger.Error($"[WAIT][FIND][ERROR] positions repository returned null: workerId={worker.id}, workerName={worker.name}");
                return null;
            }

            // ------------------------------------------------------------
            // 2) WAIT 후보 필터링
            //    - 같은 그룹/같은 층(mapId) 범위에서만 찾는다.
            //    - 사용 가능(isEnabled) + 미점유(!isOccupied) 조건
            // ------------------------------------------------------------
            var waitCandidates = allPositions.Where(p => p != null && p.subType == nameof(PositionSubType.WAIT) && p.isEnabled == true && p.isOccupied == false && p.group == group).ToList();

            if (waitCandidates == null || waitCandidates.Count == 0)
            {
                EventLogger.Warn($"[WAIT][FIND][SKIP] no available wait positions: workerId={worker.id}, workerName={worker.name}, group={group}");
                return null;
            }

            // ------------------------------------------------------------
            // 3) 지정 WAIT 포지션 우선
            //    - linkedRobotId 가 worker.id 인 WAIT 포지션이 있으면 그걸 사용
            // ------------------------------------------------------------
            var specifiedWait = waitCandidates.FirstOrDefault(p => p.linkedRobotId == worker.id);

            if (specifiedWait != null)
            {
                EventLogger.Info($"[WAIT][FIND] specified wait selected: workerId={worker.id}, workerName={worker.name}, waitPOSId={specifiedWait.id}, waitPOSName={specifiedWait.name}" +
                                 $", mapId={specifiedWait.mapId}");
                return specifiedWait;
            }

            // ------------------------------------------------------------
            // 4) 가까운 WAIT 선택 같은층
            //    - linkedRobotId 가 worker.id 인 WAIT 포지션이 있으면 그걸 사용
            // ------------------------------------------------------------

            var sameMapId = _repository.Positions.FindNearestWayPoint(worker, waitCandidates).FirstOrDefault(p => p.mapId == mapId);
            if (sameMapId != null)
            {
                EventLogger.Info($"[WAIT][FIND] sameMapId wait selected: workerId={worker.id}, workerName={worker.name}, waitPOSId={sameMapId.id}, waitPOSName={sameMapId.name}" +
                               $", mapId={sameMapId.mapId}");

                return sameMapId;
            }

            // ------------------------------------------------------------
            // 5) 가까운 WAIT 선택 다른층
            // ------------------------------------------------------------
            var AnotherMapId = _repository.Positions.FindNearestWayPoint(worker, waitCandidates).FirstOrDefault(p => p.mapId != mapId);
            if (AnotherMapId == null)
            {
                EventLogger.Warn($"[WAIT][FIND][SKIP] AnotherMap wait list empty: workerId={worker.id}, workerName={worker.name}, group={group}, mapId={mapId}");
                return null;
            }
            else
            {
                EventLogger.Info($"[WAIT][FIND] AnotherMap wait selected: workerId={worker.id}, workerName={worker.name}, waitPOSId={AnotherMapId.id}, waitPOSName={AnotherMapId.name}, mapId={AnotherMapId.mapId}");
                return AnotherMapId;
            }
        }

        /// <summary>
        /// WAIT Job 생성 요청 함수
        /// - Worker를 지정된 WAIT 포지션으로 이동시키는 Job을 Queue에 등록한다.
        /// - Queue 기반 구조이므로 JobId는 알 수 없으며,
        ///   성공/실패 여부만 bool 로 반환한다.
        /// </summary>
        private bool CreateWaitJob(Worker worker, Position waitPosition)
        {
            // ------------------------------------------------------------
            // 1) 방어 코드
            // ------------------------------------------------------------
            if (worker == null)
            {
                EventLogger.Error($"[WAIT][CREATE][ERROR] worker is null → job creation aborted");
                return false;
            }

            if (waitPosition == null)
            {
                EventLogger.Error($"[WAIT][CREATE][ERROR] wait position is null → job creation aborted");
                return false;
            }

            // ------------------------------------------------------------
            // 2) Queue 를 통한 WAIT Job 생성 요청
            // ------------------------------------------------------------
            try
            {
                _Queue.Create_Job(worker.group, null, nameof(JobType.WAIT), nameof(JobSubType.WAIT), null, 0, null
                                 , null, null, null
                                 , waitPosition.id, waitPosition.name, waitPosition.linkedFacility
                                 , worker.id);

                // --------------------------------------------------------
                // 3) 생성 요청 성공 로그
                // --------------------------------------------------------
                EventLogger.Info(
                    $"[WAIT][CREATE] enqueue wait job request: " +
                    $"workerId={worker.id}, workerName={worker.name}, group={worker.group}, " +
                    $"waitPositionId={waitPosition.id}, waitPositionName={waitPosition.name}, mapId={waitPosition.mapId}"
                );

                return true;
            }
            catch (Exception ex)
            {
                // --------------------------------------------------------
                // 4) 예외 발생 로그
                // --------------------------------------------------------
                EventLogger.Error(
                    $"[WAIT][CREATE][ERROR] exception while enqueue wait job: " +
                    $"workerId={worker.id}, workerName={worker.name}, waitPositionId={waitPosition.id}, " +
                    $"waitPositionName={waitPosition.name}, error={ex.Message}"
                );
                return false;
            }
        }
    }
}