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
            var Positions = _repository.Positions.MiR_GetAll();

            var OccupiedPositionIds = new List<string>();

            // 사용안함
            //var OccupiedPositionIds = new List<string>();
            //var InitOrders = _repository.Orders.GetAll().Where(o => o.state == nameof(OrderState.INIT)).ToList();
            //OccupiedPositionIds.AddRange(InitOrders.Where(m => IsInvalid(m.sourceId) == false).Select(m => m.sourceId).ToList());
            //OccupiedPositionIds.AddRange(InitOrders.Where(m => IsInvalid(m.destinationId) == false).Select(m => m.destinationId).ToList());

            var moveMissions = _repository.Missions.GetAll().Where(m => m.type == nameof(MissionType.MOVE)).ToList();
            var runMission = _repository.Missions.GetByRunMissions(moveMissions).ToList();
            var runMissionPositionId = _repository.Missions.GetParametas(runMission).Select(r => r.value).ToList();
            var NotOrderJobPositionNames = _repository.Jobs.GetAll().Where(m => m.orderId == null && (m.state == nameof(JobState.WORKERASSIGNED) || m.state == nameof(JobState.INIT))).Select(r => r.destinationId).ToList();
            OccupiedPositionIds.AddRange(runMissionPositionId);
            OccupiedPositionIds.AddRange(NotOrderJobPositionNames);
            OccupiedPositionIds.AddRange(workerPositionOccupied());

            foreach (var Position in Positions)
            {
                //사용안함
                //var OccupiedPosition = OccupiedPositionIds.FirstOrDefault(x => Position.id == x);
                var OccupiedPositionId = OccupiedPositionIds.FirstOrDefault(x => Position.id == x);
                if (OccupiedPositionId == null)
                {
                    updateOccupied(Position, false);
                }
                else
                {
                    updateOccupied(Position, true);
                }
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