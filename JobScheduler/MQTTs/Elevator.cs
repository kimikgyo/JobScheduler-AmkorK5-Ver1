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
        public void Elevator()
        {
            while (QueueStorage.MqttTryDequeueSubscribeElevator(out MqttSubscribeMessageDto subscribe))
            {
                try
                {
                    //Console.WriteLine(string.Format("Process Message: [{0}] {1} at {2:yyyy-MM-dd HH:mm:ss,fff}", subscribe.topic, subscribe.Payload, subscribe.Timestamp));

                    switch (subscribe.subType)
                    {
                        //case nameof(TopicSubType.state):
                        //    var state = JsonSerializer.Deserialize<MqttSubscribeDtoWorkerStatus>(subscribe.Payload!);
                        //    _mapping.Workers.MqttUpdateState(worker, state);
                        //    mapAndPositionOccupied(worker);
                        //    _repository.Workers.Update(worker);
                        //    break;

                        case nameof(TopicSubType.mission):
                            var missionStateDto = JsonSerializer.Deserialize<MqttSubscribeDtoMission>(subscribe.Payload!);
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
                catch (Exception ex)
                {
                    LogExceptionMessage(ex);
                }
            }
        }
    }
}