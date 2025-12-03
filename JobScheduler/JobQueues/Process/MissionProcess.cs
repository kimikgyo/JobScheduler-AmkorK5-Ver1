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
        public void Add_Mission()
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
                    state = nameof(MissionState.INIT),
                    specifiedWorkerId = cmd.job.specifiedWorkerId,
                    assignedWorkerId = cmd.job.assignedWorkerId,
                    createdAt = DateTime.Now,
                    updatedAt = null,
                    finishedAt = null,
                    sequenceUpdatedAt = null,
                };

                switch (mission.type)
                {
                    case nameof(MissionType.MOVE):
                        if (cmd.position != null)
                        {
                            mission.name = cmd.position.name;
                        }
                        else
                        {
                            mission.name = cmd.missionTemplate.name;
                        }
                        break;

                    default:
                        mission.name = cmd.missionTemplate.name;
                        break;
                }

                foreach (var parameta in cmd.missionTemplate.parameters)
                {
                    Parameter param = missionParameter(cmd.missionTemplate, cmd.job, cmd.position, parameta, cmd.job.drumKeyCode, cmd.job.sourcelinkedFacility, cmd.job.destinationlinkedFacility);
                    if (param != null)
                    {
                        mission.parameters.Add(param);
                    }
                }
                mission.linkedFacility = linkedFacility(mission.subType, cmd.job.sourcelinkedFacility, cmd.job.destinationlinkedFacility);
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

        private string linkedFacility(string missionSubType, string sourcelinkedFacility, string destinatiolinkedFacility)
        {
            string reValue = null;
            switch (missionSubType)
            {
                case nameof(MissionSubType.SOURCEMOVE):
                case nameof(MissionSubType.SOURCEACTION):
                case nameof(MissionSubType.SOURCESTOPOVERMOVE):
                    reValue = sourcelinkedFacility;
                    break;

                case nameof(MissionSubType.DESTINATIONMOVE):
                case nameof(MissionSubType.DESTINATIONACTION):
                case nameof(MissionSubType.DESTINATIONSTOPOVERMOVE):
                    reValue = destinatiolinkedFacility;
                    break;
            }

            return reValue;
        }

        private Parameter missionParameter(MissionTemplate missionTemplate, Job job, Position position, Parameter parameta, string drumKeyCode
                                         , string sourcelinkedFacility, string destinatiolinkedFacility)
        {
            Parameter param = null;
            if (IsInvalid(parameta.value))
            {
                switch (parameta.key)
                {
                    case "target":
                        if (position != null)
                        {
                            if (missionTemplate.type == nameof(MissionType.MOVE))
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

                        if (missionTemplate.subType == nameof(MissionSubType.SOURCEACTION))
                        {
                            param = new Parameter
                            {
                                key = parameta.key,
                                value = sourcelinkedFacility
                            };
                        }
                        else if (missionTemplate.subType == nameof(MissionSubType.DESTINATIONACTION))
                        {
                            param = new Parameter
                            {
                                key = parameta.key,
                                value = destinatiolinkedFacility
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
                        //출발지 Position 있는경우
                        var Sourceposition = _repository.Positions.MiR_GetById(job.sourceId);
                        if (Sourceposition != null)
                        {
                            var map = _repository.Maps.GetBymapId(Sourceposition.mapId);
                            if (map != null)
                            {
                                param = new Parameter
                                {
                                    key = parameta.key,
                                    value = $"{map.name}"
                                };
                            }
                        }
                        else
                        {
                            //출발지 Position이 없는경우
                            var worker = _repository.Workers.GetById(job.specifiedWorkerId);
                            if (worker != null)
                            {
                                var map = _repository.Maps.GetBymapId(worker.mapId);
                                if (map != null)
                                {
                                    param = new Parameter
                                    {
                                        key = parameta.key,
                                        value = $"{map.name}"
                                    };
                                }
                            }
                        }
                        break;

                    case "DestinationFloor":

                        var Destposition = _repository.Positions.MiR_GetById(job.destinationId);
                        if (Destposition != null)
                        {
                            var map = _repository.Maps.GetBymapId(Destposition.mapId);
                            if (map != null)
                            {
                                param = new Parameter
                                {
                                    key = parameta.key,
                                    value = $"{map.name}"
                                };
                            }
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