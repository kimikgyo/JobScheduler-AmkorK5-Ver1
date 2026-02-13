using Common.DTOs.MQTTs.Missions;
using Common.Models;
using Common.Models.Jobs;
using Common.Models.Queues;
using System.Text.Json;

namespace JOB.MQTTs
{
    public partial class MqttProcess
    {
        public void Subscribe_Trffic()
        {
            while (QueueStorage.MqttTryDequeueSubscribeTraffic(out MqttSubscribeMessageDto subscribe))
            {
                try
                {
                    switch (subscribe.subType)
                    {
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
                catch (Exception ex)
                {
                    LogExceptionMessage(ex);
                }
            }
        }
    }
}