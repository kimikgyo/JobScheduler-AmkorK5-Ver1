using Common.DTOs.MQTTs.Missions;
using Common.DTOs.MQTTs.Workers;
using Common.Models;
using Common.Models.Jobs;
using Common.Models.Queues;
using System.Text.Json;

namespace JOB.MQTTs
{
    public partial class MqttProcess
    {
        public void Worker()
        {
            while (QueueStorage.MqttTryDequeueSubscribeWorker(out MqttSubscribeMessageDto subscribe))
            {
                try
                {
                    var worker = _repository.Workers.MiR_GetById(subscribe.id);
                    if (worker != null)
                    {
                        //Console.WriteLine(string.Format("Process Message: [{0}] {1} at {2:yyyy-MM-dd HH:mm:ss,fff}", subscribe.topic, subscribe.Payload, subscribe.Timestamp));

                        switch (subscribe.subType)
                        {
                            case nameof(TopicSubType.state):
                                var state = JsonSerializer.Deserialize<Subscribe_WorkerStatusDto>(subscribe.Payload!);
                                _mapping.Workers.MqttUpdateState(worker, state);
                                PositionOccupied(worker);
                                _repository.Workers.Update(worker);
                                break;

                            case nameof(TopicSubType.mission):
                                var missionStateDto = JsonSerializer.Deserialize<Subscribe_MissionDto>(subscribe.Payload!);
                                var mission = _repository.Missions.GetById(missionStateDto.acsMissionId);
                                if (mission != null)
                                {
                                    string missionstate = missionStateDto.state.Replace(" ", "").ToUpper();

                                    if (missionstate != nameof(MissionState.COMPLETED))
                                    {
                                        updateStateMission(mission, missionstate, true);
                                    }
                                    else
                                    {
                                        updateStateMission(mission, missionstate);
                                    }
                                }
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogExceptionMessage(ex);
                }
            }
        }



        private void PositionOccupied(Worker worker)
        {
            var positions = _repository.Positions.MiR_GetByPosValue(worker.position_X, worker.position_Y, worker.mapId).ToList();

            if (positions == null || positions.Count == 0)
            {
                if (worker.PositionId != null)
                {
                    var position = _repository.Positions.GetById(worker.PositionId);
                    if (position != null)
                    {
                        updateOccupied(position, false, 0);
                    }
                    worker.PositionId = null;
                    worker.PositionName = null;
                    _repository.Workers.Update(worker);

                }
            }
            else
            {
                foreach (var position in positions)
                {
                    updateOccupied(position, true, 0);

                    if (position.id != worker.PositionId)
                    {
                        worker.PositionId = position.id;
                        worker.PositionName = position.name;
                        _repository.Workers.Update(worker);

                    }
                }
            }
        }

        private void updateOccupied(Position position, bool flag, double holdTime)
        {
            if (position.isOccupied != flag)
            {
                position.isOccupied = flag;
                position.occupiedHoldTime = DateTime.Now.AddSeconds(holdTime);
                _repository.Positions.update(position);
            }
        }
    }
}