using Common.Models.Jobs;

namespace JOB.Services
{
    public partial class SchedulerService
    {
        private void PositionControl()
        {
            PositionOccupied();
        }

        private void PositionOccupied()
        {
            // 0) Positions 전체 로드
            var positions = _repository.Positions.MiR_GetAll();
            if (positions == null || positions.Count == 0)
                return;

            // 1) 점유 후보 PositionId 모으기
            var occupiedPositionIds = new List<string>();

            // 1-1) MOVE Mission 중 Run 상태인 것들의 target 파라미터
            var moveMissions = _repository.Missions.GetAll()
                .Where(m => m.type == nameof(MissionType.MOVE))
                .ToList();

            var runMissions = _repository.Missions.GetByRunMissions(moveMissions).ToList();

            var runMissionTargetIds = _repository.Missions.GetParametas(runMissions)
                .Where(p => p.key == "target")
                .Select(p => p.value)
                .ToList();

            // 1-2) OrderId 없는 Job(진행중)의 destinationId
            var notOrderJobPositionIds = _repository.Jobs.GetAll()
                .Where(j => j.orderId == null && j.state != nameof(JobState.COMPLETED))
                .Select(j => j.destinationId)
                .ToList();

            // 1-3) Subscribe_Worker 점유
            var workerOccupied = workerPositionOccupied();

            occupiedPositionIds.AddRange(runMissionTargetIds);
            occupiedPositionIds.AddRange(notOrderJobPositionIds);

            if (workerOccupied != null && workerOccupied.Count > 0) occupiedPositionIds.AddRange(workerOccupied);

            // (선택) 공백 제거 정도는 해두면 안전
            occupiedPositionIds = occupiedPositionIds.Where(x => string.IsNullOrWhiteSpace(x) == false).ToList();

            // 2) 전체 Positions를 돌면서
            //    "있으면 true / 없으면 false" 판단 후 update

            foreach (var pos in positions)
            {
                if (pos == null) continue;
                if (string.IsNullOrWhiteSpace(pos.id)) continue;

                // 2-1) 목록에 있으면 true / 없으면 false (FirstOrDefault 스타일)
                var found = occupiedPositionIds.FirstOrDefault(x => x == pos.id);
                bool shouldBeOccupied = false;

                if (found != null) shouldBeOccupied = true;
                else shouldBeOccupied = false;

                // 2-2) Hold 로직: false로 내리려는 경우에만 적용
                // A_task가 마지막에 올린 점유를 일정시간 유지시키기
                // 전제: pos.occupiedHoldUntil(DateTime?)가 존재해야 함.
                // 필드가 없다면 이 블록을 주석 처리하세요.
                // holdUntil이 있고, 아직 만료 전이면 false로 덮어쓰기 금지
                if (pos.occupiedHoldTime != null && pos.occupiedHoldTime > DateTime.Now)
                {
                    // 점유 유지(스킵)
                    continue;
                }

                // 2-3) (권장) 변경이 있을 때만 DB 업데이트
                if (pos.isOccupied == shouldBeOccupied)
                    continue;

                // 2-5) 업데이트
                updateOccupied(pos, shouldBeOccupied, 0.5, "PositionOccupied");


            }
        }

        /// <summary>
        /// [목적]
        /// - 현재 접속(Connect)된 모든 Worker(로봇)에 대해,
        ///   로봇 좌표(worker.position_X/Y)가 점유 반경 안에 들어온 Position 후보들을 찾고,
        ///   그 중 "가장 가까운 1개"를 선택하여
        ///   1) worker.PositionId / worker.PositionName 업데이트
        ///   2) 점유된 Position(DB id) 리스트를 반환한다.
        ///
        /// [핵심 정책]
        /// - Position 참조는 "DB 문서 id"로 통일한다. (picked.id 사용)
        /// - 후보 Position이 여러 개일 경우 "가장 가까운 1개"만 선택한다.
        ///   (중복 점유/깜빡임 방지)
        ///
        /// [디버깅 포인트]
        /// - workers.Count가 기대대로 나오는지 (연결된 로봇 수)
        /// - MiR_GetByPosValue(...)가 후보를 몇 개 반환하는지 (positions.Count)
        /// - picked가 어떤 position인지 (picked.id, picked.name, picked.x/y)
        /// - worker.PositionId가 왜/언제 바뀌는지 (이전값 vs 새값)
        /// - candidates가 0개로 튀는 순간이 있는지 (좌표 흔들림/occR/rough 설정 문제)
        /// </summary>
        private List<string> workerPositionOccupied()
        {
            // ------------------------------------------------------------
            // 0) 반환할 점유 포지션(DB id) 목록
            // ------------------------------------------------------------
            var positionIds = new List<string>();

            // ------------------------------------------------------------
            // 1) 현재 연결된 Worker(로봇) 목록 가져오기
            // ------------------------------------------------------------
            var workers = _repository.Workers.MiR_GetByConnect();

            // [디버깅] workers가 null이면 아예 연결된 로봇을 못 가져온 것
            // EventLogger.Warn("[workerPositionOccupied] workers == null");
            if (workers == null) return positionIds;

            // [디버깅] 연결 로봇 수 확인
            // EventLogger.Info($"[workerPositionOccupied] workers.Count={workers.Count}");

            // ------------------------------------------------------------
            // 2) 각 Worker(로봇)에 대해 점유 포지션 계산
            // ------------------------------------------------------------
            foreach (var worker in workers)
            {
                if (worker == null) continue;

                // [디버깅] 로봇 기본 정보 확인
                // EventLogger.Info($"[workerPositionOccupied] workerId={worker.id}, mapId={worker.mapId}, pos=({worker.position_X},{worker.position_Y}), prevPosId={worker.PositionId}");

                // --------------------------------------------------------
                // 2-1) 로봇 좌표 기준 "점유 후보 Position" 검색
                //      - MiR_GetByPosValue 내부에서:
                //        (1) rough 사각형 컷 -> (2) 원형 반경(occR) 판정
                // --------------------------------------------------------
                var positions = _repository.Positions
                    .MiR_GetByPosValue(worker.position_X, worker.position_Y, worker.mapId)
                    // WORK 노드는 점유 판단에서 제외(정책)
                    .Where(r => r != null && r.nodeType != nameof(NodeType.WORK))
                    .ToList();

                // [디버깅] 후보 개수 확인
                // EventLogger.Info($"[workerPositionOccupied] workerId={worker.id} candidates.Count={(positions == null ? -1 : positions.Count)}");

                // --------------------------------------------------------
                // 2-2) 후보가 여러 개면 "가장 가까운 1개"만 선택
                //
                // ※ FindNearestWayPoint는 거리순으로 정렬된 리스트를 반환하고,
                //    FirstOrDefault()로 가장 가까운 1개를 선택한다.
                // --------------------------------------------------------
                Position picked = null;

                if (positions != null && positions.Count > 0)
                {
                    // (주의) OrderBy 정렬이 들어가므로, 후보가 많으면 비용이 커질 수 있음
                    //        하지만 후보가 보통 1~3개 수준이면 문제 없음.
                    picked = _repository.Positions.FindNearestWayPoint(worker, positions).FirstOrDefault();
                }

                // --------------------------------------------------------
                // 2-3) picked가 없으면: 로봇이 어떤 Position도 점유하지 않는 상태
                //      -> Worker.PositionId를 해제(null)로 만들고 DB 업데이트
                // --------------------------------------------------------
                if (picked == null)
                {
                    // [디버깅] 후보가 없어서 해제되는 상황인지 확인
                    // EventLogger.Warn($"[workerPositionOccupied] workerId={worker.id} picked==null -> release. prevPosId={worker.PositionId}");

                    // 기존에 저장된 점유 포지션이 있다면 해제 처리
                    if (!string.IsNullOrWhiteSpace(worker.PositionId))
                    {
                        worker.PositionId = null;
                        worker.PositionName = null;

                        // ✅ 변경 있을 때만 DB 업데이트
                        _repository.Workers.Update(worker);

                        // [디버깅] 해제 업데이트 확인
                        // EventLogger.Info($"[workerPositionOccupied] workerId={worker.id} worker.PositionId cleared");
                    }

                    continue; // 다음 worker로
                }

                // --------------------------------------------------------
                // 2-4) picked가 있으면: 이 로봇은 picked Position을 점유한다고 판단
                //      -> 반환 목록에 DB id 추가
                // --------------------------------------------------------
                // (중요) DB 문서 id로 통일
                positionIds.Add(picked.id);

                // [디버깅] 어떤 포지션을 선택했는지 확인
                // EventLogger.Info($"[workerPositionOccupied] workerId={worker.id} picked: id={picked.id}, name={picked.name}, pos=({picked.x},{picked.y})");

                // --------------------------------------------------------
                // 2-5) Worker.PositionId가 바뀌었을 때만 Worker 업데이트
                //      -> 매 MQTT 주기마다 불필요한 DB write 방지
                // --------------------------------------------------------
                if (!string.Equals(worker.PositionId, picked.id, StringComparison.Ordinal))
                {
                    var prevId = worker.PositionId;

                    worker.PositionId = picked.id;
                    worker.PositionName = picked.name;

                    _repository.Workers.Update(worker);

                    // [디버깅] 변경 업데이트 확인
                    // EventLogger.Info($"[workerPositionOccupied] workerId={worker.id} PositionId changed: {prevId} -> {picked.id}");
                }
                else
                {
                    // [디버깅] 동일해서 업데이트 안 하는 케이스 확인
                    // EventLogger.Info($"[workerPositionOccupied] workerId={worker.id} PositionId same -> skip update");
                }
            }

            // ------------------------------------------------------------
            // 3) (권장) 중복 제거 + 공백 제거
            // - Distinct() 사용 X
            // - HashSet 사용 X
            // - List + FirstOrDefault로 "이미 있으면 스킵" 방식
            // ------------------------------------------------------------
            var cleaned = new List<string>();

            foreach (var id in positionIds)
            {
                // 1) 공백/Null 제거
                if (string.IsNullOrWhiteSpace(id)) continue;

                // 2) 이미 들어있는지 검사 (FirstOrDefault 스타일)
                //    - found가 null이면 아직 없는 값
                var found = cleaned.FirstOrDefault(x => x == id);

                // 3) 이미 있으면 스킵, 없으면 추가
                if (found != null) continue;

                cleaned.Add(id);
            }

            // 4) positionIds를 정리된 리스트로 교체
            positionIds = cleaned;

            return positionIds;
        }


    }
}