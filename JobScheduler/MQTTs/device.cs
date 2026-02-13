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
        public void Subscribe_Iot()
        {
            while (QueueStorage.MqttTryDequeueSubscribeIot(out MqttSubscribeMessageDto subscribe))
            {
                try
                {
                    if (subscribe.id == "mission" && subscribe.subType == "report")
                    {
                        var missionStateDto = JsonSerializer.Deserialize<Subscribe_MissionDto>(subscribe.Payload!);
                        var mission = _repository.Missions.GetById(missionStateDto.acsMissionId);
                        if (mission != null)
                        {
                            string missionstate = missionStateDto.state.Replace(" ", "").ToUpper();

                            if (missionstate != nameof(MissionState.COMPLETED))
                            {
                                updateStateMission(mission, missionstate,"[MQTT][IOT]", true);
                            }
                            else
                            {
                                updateStateMission(mission, missionstate, "[MQTT][IOT]");
                            }
                        }
                    }

                    //Console.WriteLine(string.Format("Process Message: [{0}] {1} at {2:yyyy-MM-dd HH:mm:ss,fff}", subscribe.topic, subscribe.Payload, subscribe.Timestamp));

                    //switch (subscribe.subType)
                    //{
                    //    case nameof(TopicSubType.state):
                    //        var state = JsonSerializer.Deserialize<Subscribe_WorkerStatusDto>(subscribe.Payload!);
                    //        break;

                    //    case nameof(TopicSubType.mission):

                    //        break;
                    //}
                }
                catch (Exception ex)
                {
                    LogExceptionMessage(ex);
                }
            }
        }
    }
}