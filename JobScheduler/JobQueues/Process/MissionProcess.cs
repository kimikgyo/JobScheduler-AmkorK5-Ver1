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
            while (QueueStorage.AddTryDequeueJobMission(out var cmd))
            {
                int missionsequence = 1;
                foreach (var missionTemplate in jobTemplate.missionTemplates)
                {
                    var mission = new Mission
                    {
                        orderId = orderId,
                        jobId = job.guid,
                        guid = Guid.NewGuid().ToString(),
                        carrierId = carrierId,
                        name = missionTemplate.name,
                        service = missionTemplate.service,
                        type = missionTemplate.type,
                        subType = missionTemplate.subType,
                        sequence = missionsequence,
                        isLocked = missionTemplate.isLook,
                        sequenceChangeCount = 0,
                        retryCount = 0,
                        state = nameof(MissionState.INIT),
                        specifiedWorkerId = job.specifiedWorkerId,
                        assignedWorkerId = job.assignedWorkerId,
                        createdAt = DateTime.Now,
                        updatedAt = null,
                        finishedAt = null,
                        sequenceUpdatedAt = null,
                    };

                    if (missionTemplate.name == null)
                    {
                        switch (mission.subType)
                        {
                            case nameof(MissionSubType.SOURCEMOVE):
                                mission.name = sourceName;
                                break;

                            case nameof(MissionSubType.DESTINATIONMOVE):
                                mission.name = destinationName;
                                break;
                        }
                    }
                    else
                    {
                        mission.name = missionTemplate.name;
                    }

                    foreach (var parameta in missionTemplate.parameters)
                    {
                        Parameter param = missionParameter(missionTemplate, job, parameta, drumKeyCode, sourcelinkedFacility, destinatiolinkedFacility);
                        if (param != null)
                        {
                            mission.parameters.Add(param);
                        }
                    }
                    mission.linkedFacility = linkedFacility(mission.subType, sourcelinkedFacility, destinatiolinkedFacility);
                    mission.preReports = missionTemplate.preReports;
                    mission.postReports = missionTemplate.postReports;
                    mission.parametersJson = JsonSerializer.Serialize(mission.parameters);
                    mission.preReportsJson = JsonSerializer.Serialize(mission.preReports);
                    mission.postReportsJson = JsonSerializer.Serialize(mission.postReports);
                    missionsequence++;
                    _repository.Missions.Add(mission);
                    _repository.MissionHistorys.Add(mission);
                    _mqttQueue.MqttPublishMessage(TopicType.mission, TopicSubType.status, _mapping.Missions.MqttPublish(mission));
                }
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

        private Parameter missionParameter(MissionTemplate_Single missionTemplate, Job job, Parameter parameta, string drumKeyCode
                                         , string sourcelinkedFacility, string destinatiolinkedFacility)
        {
            Parameter param = null;
            if (parameta.value == null)
            {
                switch (parameta.key)
                {
                    case "target":
                        if (missionTemplate.subType == nameof(MissionSubType.SOURCEMOVE))
                        {
                            param = new Parameter
                            {
                                key = parameta.key,
                                value = job.sourceId,
                            };
                        }
                        else if (missionTemplate.subType == nameof(MissionSubType.DESTINATIONMOVE))
                        {
                            param = new Parameter
                            {
                                key = parameta.key,
                                value = job.destinationId,
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
    }
}
