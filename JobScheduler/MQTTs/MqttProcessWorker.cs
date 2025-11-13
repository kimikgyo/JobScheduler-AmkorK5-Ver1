using Common.DTOs.Bases;
using Common.DTOs.Jobs;
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
                                var state = JsonSerializer.Deserialize<MqttSubscribeDtoWorkerStatus>(subscribe.Payload!);
                                _mapping.Workers.MqttUpdateState(worker, state);
                                mapAndPositionOccupied(worker);
                                _repository.Workers.Update(worker);
                                break;

                            case nameof(TopicSubType.mission):
                                var missionStateDto = JsonSerializer.Deserialize<MqttSubscribeDtoMission>(subscribe.Payload!);
                                var mission = _repository.Missions.GetById(missionStateDto.acsMissionId);
                                if (mission != null)
                                {
                                    string missionstate = missionStateDto.state.Replace(" ", "").ToUpper();

                                    if(missionstate != nameof(MissionState.COMPLETED))
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
       
        private void mapAndPositionOccupied(Worker worker)
        {
            var map = _repository.Maps.GetByName(worker.mapName);
            if (map != null)
            {
                worker.mapId = map.mapId;
            }
            var position = _repository.Positions.MiR_GetByPosValue(worker.position_X, worker.position_Y, worker.mapId).FirstOrDefault();
            if (position != null)
            {
                updateOccupied(position, true);
                if (position.id != worker.PositionId)
                {
                    worker.PositionId = position.id;
                    worker.PositionName = position.name;
                }
            }
            else
            {
                worker.PositionId = null;
                worker.PositionName = null;
            }
        }

        private void updateOccupied(Position position, bool flag)
        {
            if (position.isOccupied != flag)
            {
                position.isOccupied = flag;
                _repository.Positions.update(position);
            }
        }
    }
}