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

            // 1-3) Worker 점유
            var workerOccupied = workerPositionOccupied();

            occupiedPositionIds.AddRange(runMissionTargetIds);
            occupiedPositionIds.AddRange(notOrderJobPositionIds);

            if (workerOccupied != null && workerOccupied.Count > 0)occupiedPositionIds.AddRange(workerOccupied);

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
                if (shouldBeOccupied == false)
                {
                    // holdUntil이 있고, 아직 만료 전이면 false로 덮어쓰기 금지
                    if (pos.occupiedHoldTime != null && pos.occupiedHoldTime > DateTime.Now)
                    {
                        // 점유 유지(스킵)
                        continue;
                    }
                }

                // 2-3) (권장) 변경이 있을 때만 DB 업데이트
                if (pos.isOccupied == shouldBeOccupied)
                    continue;

                updateOccupied(pos, shouldBeOccupied, 0);
            }
        }


        private List<string> workerPositionOccupied()
        {
            List<string> PositionIds = new List<string>();
            foreach (var worker in _repository.Workers.MiR_GetByConnect())
            {
                var positions = _repository.Positions.MiR_GetByPosValue(worker.position_X, worker.position_Y, worker.mapId).ToList();

                if (positions == null || positions.Count == 0)
                {
                    if (worker.PositionId != null)
                    {
                        worker.PositionId = null;
                        worker.PositionName = null;
                        _repository.Workers.Update(worker);
                    }
                }
                else
                {
                    foreach (var position in positions)
                    {
                        PositionIds.Add(position.id);
                        if (position.id != worker.PositionId)
                        {
                            worker.PositionId = position.id;
                            worker.PositionName = position.name;
                            _repository.Workers.Update(worker);
                        }
                    }
                }
            }
            return PositionIds;
        }
    }
}