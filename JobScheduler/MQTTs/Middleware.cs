using Common.DTOs.MQTTs.Middlewares;
using Common.DTOs.MQTTs.Missions;
using Common.Models;
using Common.Models.Jobs;
using Common.Models.Queues;
using System.Text.Json;

namespace JOB.MQTTs
{
    public partial class MqttProcess
    {
        public void Subscribe_Middleware()
        {
            while (QueueStorage.MqttTryDequeueSubscribeMiddleware(out MqttSubscribeMessageDto subscribe))
            {
                try
                {
                    var middleware = _repository.Middlewares.GetByWorkerId(subscribe.id);
                    if (middleware != null)
                    {
                        //Console.WriteLine(string.Format("Process Message: [{0}] {1} at {2:yyyy-MM-dd HH:mm:ss,fff}", subscribe.topic, subscribe.Payload, subscribe.Timestamp));

                        switch (subscribe.subType)
                        {
                            case nameof(TopicSubType.state):
                                var state = JsonSerializer.Deserialize<Subscribe_MiddlewareStatusDto>(subscribe.Payload!);
                                _mapping.Middlewares.MqttUpdateState(middleware, state);
                                _repository.Middlewares.Update(middleware);
                                break;

                            case nameof(TopicSubType.mission):
                                var missionStateDto = JsonSerializer.Deserialize<Subscribe_MissionDto>(subscribe.Payload!);
                                var mission = _repository.Missions.GetById(missionStateDto.acsMissionId);
                                if (mission != null)
                                {
                                    string missionstate = missionStateDto.state.Replace(" ", "").ToUpper();

                                    if (missionstate != nameof(MissionState.COMPLETED))
                                    {
                                        updateStateMission(mission, missionstate, "[MQTT][Elevator]", true);
                                    }
                                    else
                                    {
                                        updateStateMission(mission, missionstate, "[MQTT][Elevator]");
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
    }
}