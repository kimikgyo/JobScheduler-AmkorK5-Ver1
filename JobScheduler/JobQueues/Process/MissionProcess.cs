using Common.Models;
using Common.Models.Bases;
using Common.Models.Jobs;
using Common.Models.Queues;
using Common.Templates;
using System.Text.Json;

namespace JOB.JobQueues.Process
{
    public partial class QueueProcess
    {
        public void Create_Mission()
        {
            while (QueueStorage.Add_Mission_TryDequeue(out var cmd))
            {
                var mission = new Mission
                {
                    orderId = cmd.job.orderId,
                    jobId = cmd.job.guid,
                    guid = Guid.NewGuid().ToString(),
                    carrierId = cmd.job.carrierId,
                    name = cmd.missionTemplate.name,
                    service = cmd.missionTemplate.service,
                    type = cmd.missionTemplate.type,
                    subType = cmd.missionTemplate.subType,
                    sequence = cmd.seq,
                    isLocked = cmd.missionTemplate.isLook,
                    sequenceChangeCount = 0,
                    retryCount = 0,
                    state = nameof(MissionState.WORKERASSIGNED),
                    specifiedWorkerId = cmd.job.specifiedWorkerId,
                    assignedWorkerId = cmd.worker.id,
                    createdAt = DateTime.Now,
                    updatedAt = null,
                    finishedAt = null,
                    sequenceUpdatedAt = null,
                };

                mission.name = missionName_linkedFacility(cmd.missionTemplate, cmd.position).name;
                mission.linkedFacility = missionName_linkedFacility(cmd.missionTemplate, cmd.position).linkedFacility;

                foreach (var parameta in cmd.missionTemplate.parameters)
                {
                    Parameter param = missionParameter(cmd.missionTemplate, cmd.position, parameta, cmd.job.drumKeyCode);
                    if (param != null)
                    {
                        mission.parameters.Add(param);
                    }
                }

                mission.preReports = cmd.missionTemplate.preReports;
                mission.postReports = cmd.missionTemplate.postReports;
                mission.parametersJson = JsonSerializer.Serialize(mission.parameters);
                mission.preReportsJson = JsonSerializer.Serialize(mission.preReports);
                mission.postReportsJson = JsonSerializer.Serialize(mission.postReports);
                _repository.Missions.Add(mission);
                _repository.MissionHistorys.Add(mission);
                _mqttQueue.MqttPublishMessage(TopicType.mission, TopicSubType.status, _mapping.Missions.Publish(mission));
            }
        }

        private (string name, string linkedFacility) missionName_linkedFacility(MissionTemplate missionTemplate, Position position)
        {
            string name = null;
            string linkedFacility = null;
            if (position != null)
            {
                name = position.name;
                linkedFacility = position.linkedFacility;
            }
            else
            {
                name = missionTemplate.name;
                linkedFacility = null;
            }
            return (name, linkedFacility);
        }

        private Parameter missionParameter(MissionTemplate missionTemplate, Position position, Parameter parameta, string drumKeyCode)
        {
            Parameter param = null;
            if (IsInvalid(parameta.value))
            {
                switch (parameta.key)
                {
                    case "target":
                        if (position != null
                             && missionTemplate.subType != nameof(MissionTemplateSubType.ELEVATORWAITMOVE)
                             && missionTemplate.subType != nameof(MissionTemplateSubType.ELEVATORENTERMOVE)
                             && missionTemplate.subType != nameof(MissionTemplateSubType.ELEVATOREXITMOVE)
                             && missionTemplate.subType != nameof(MissionTemplateSubType.SWITCHINGMAP)
                             && missionTemplate.subType != nameof(MissionTemplateSubType.RIGHTTURN))
                        {
                            param = new Parameter
                            {
                                key = parameta.key,
                                value = position.id,
                            };
                        }
                        else
                        {
                            param = new Parameter
                            {
                                key = parameta.key,
                                value = parameta.value,
                            };
                        }
                        break;

                    case "targetlevel":
                        var batterysetting = _repository.Battery.GetAll();
                        param = new Parameter
                        {
                            key = parameta.key,
                            value = batterysetting.chargeEnd.ToString(),
                        };
                        break;

                    case "linkedFacility":
                        if (position != null)
                        {
                            param = new Parameter
                            {
                                key = parameta.key,
                                value = position.linkedFacility
                            };
                        }
                        else
                        {
                            param = new Parameter
                            {
                                key = parameta.key,
                                value = parameta.value,
                            };
                        }
                        break;

                    case "linkedArea":
                        if (position != null)
                        {
                            param = new Parameter
                            {
                                key = parameta.key,
                                value = position.linkedArea
                            };
                        }
                        else
                        {
                            param = new Parameter
                            {
                                key = parameta.key,
                                value = parameta.value,
                            };
                        }
                        break;

                    case "drumKeyCode":
                        param = new Parameter
                        {
                            key = parameta.key,
                            value = drumKeyCode
                        };
                        break;

                    case "SourceFloor":
                    case "DestinationFloor":
                        var map = _repository.Maps.GetBymapId(position.mapId);
                        if (map != null)
                        {
                            param = new Parameter
                            {
                                key = parameta.key,
                                value = $"{map.name}"
                            };
                        }
                        break;
                }
            }
            else
            {
                param = new Parameter
                {
                    key = parameta.key,
                    value = parameta.value,
                };
            }
            return param;
        }

        private bool IsInvalid(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                || value.ToUpper() == "NULL"
                || value.ToUpper() == "STRING";
        }
    }
}