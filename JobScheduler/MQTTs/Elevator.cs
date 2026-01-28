using Common.DTOs.MQTTs.Elevator;
using Common.DTOs.MQTTs.Missions;
using Common.DTOs.MQTTs.UI;
using Common.Models;
using Common.Models.Bases;
using Common.Models.Jobs;
using Common.Models.Queues;
using System.Text.Json;

namespace JOB.MQTTs
{
    public partial class MqttProcess
    {
        public void Subscribe_Elevator()
        {
            while (QueueStorage.MqttTryDequeueSubscribeElevator(out MqttSubscribeMessageDto subscribe))
            {
                try
                {
                    //Console.WriteLine(string.Format("Process Message: [{0}] {1} at {2:yyyy-MM-dd HH:mm:ss,fff}", subscribe.topic, subscribe.Payload, subscribe.Timestamp));
                    switch (subscribe.subType)
                    {
                        case nameof(TopicSubType.status):
                            var elevator = _repository.Elevator.GetById(subscribe.id);
                            var status = JsonSerializer.Deserialize<Subscribe_ElevatorStatusDto>(subscribe.Payload!);
                            if (elevator == null)
                            {
                                var create = _mapping.Elevators.MqttCreateElevator(status);
                                _repository.Elevator.Add(create);
                            }
                            else
                            {
                                string state = status.state.Replace(" ", "").ToUpper();
                                string mode = status.mode.Replace(" ", "").ToUpper();
                                updateStateElevator(elevator, state, mode);
                            }

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

                        case nameof(TopicSubType.request):
                            var requestDto = JsonSerializer.Deserialize<Subscribe_UIDto>(subscribe.Payload!);
                            var elevatorParam = requestDto.parameters.FirstOrDefault(r => r.key.ToUpper() == "LINKEDFACILITY");
                            var requsetParam = requestDto.parameters.FirstOrDefault(r => r.key.ToUpper() == "MODECHANGE");
                            if (elevatorParam != null && requsetParam != null)
                            {
                                var elevatorGetById = _repository.Elevator.GetById(elevatorParam.value);
                                if (elevatorGetById != null)
                                {
                                    createModechangeMission(elevatorParam, requsetParam);
                                    updateModeChange(elevatorGetById, requsetParam.value);

                                    var message = new Publish_UIDto
                                    {
                                        id = Guid.NewGuid().ToString(),
                                        requestid = requestDto.id,
                                        resultvalue = "OK"
                                    };
                                    _mqttQueue.MqttPublishMessage(TopicType.ui, nameof(TopicSubType.response), message);
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

        private void createModechangeMission(Parameter parameter1, Parameter parameter2)
        {
            var createmission = new Mission
            {
                orderId = null,
                jobId = null,
                guid = Guid.NewGuid().ToString(),
                carrierId = null,
                name = "ElevatorModeChange",
                service = nameof(Service.ELEVATOR),
                type = nameof(MissionType.ACTION),
                subType = nameof(MissionSubType.MODECHANGE),
                sequence = 0,
                isLocked = true,
                sequenceChangeCount = 0,
                retryCount = 0,
                state = nameof(MissionState.WAITING),
                specifiedWorkerId = null,
                assignedWorkerId = null,
                createdAt = DateTime.Now,
                updatedAt = null,
                finishedAt = null,
                sequenceUpdatedAt = null,
            };

            createmission.parameters.Add(parameter1);
            createmission.parameters.Add(parameter2);
            createmission.parametersJson = JsonSerializer.Serialize(createmission.parameters);
            _repository.Missions.Add(createmission);
        }

        private void updateModeChange(Elevator elevator, string requestValue)
        {
            elevator.modeChangeRequest = requestValue;
            _repository.Elevator.Update(elevator);
        }

        private void updateStateElevator(Elevator elevator, string state, string mode)
        {
            if (elevator.modeChangeRequest == state)
            {
                elevator.modeChangeRequest = null;
                _repository.Elevator.Update(elevator);
            }

            if (elevator.state != state || elevator.mode != mode)
            {
                elevator.state = state;
                elevator.mode = mode;

                if (elevator.modeChangeRequest == state)
                {
                    elevator.modeChangeRequest = null;
                }

                _repository.Elevator.Update(elevator);
            }
        }
    }
}