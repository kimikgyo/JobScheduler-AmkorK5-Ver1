using Common.Models;
using Common.Models.Bases;
using Common.Models.Jobs;
using Common.Models.Queues;
using Common.Templates;
using System.Text.Json;

namespace JOB.JobQueues
{
    public partial class QueueProcess
    {
        public void AddJob_Mission()
        {
            while (QueueStorage.AddTryDequeueJobMission(out var cmd))
            {
                var orderId = cmd.orderId;
                var carrierId = cmd.carrierId;
                var drumKeyCode = cmd.drumKeyCode;
                var priority = cmd.priority;
                var sourceId = cmd.sourceId;
                var destinationId = cmd.destinationId;
                var specifiedWorkerId = cmd.specifiedWorkerId;
                var assignedWorkerId = cmd.assignedWorkerId;
                var jobTemplate = cmd.jobTemplate;
                var sourceName = cmd.sourceName;
                var destinationName = cmd.destinationName;
                var sourcelinkedFacility = cmd.sourcelinkedFacility;
                var destinatiolinkedFacility = cmd.destinationlinkedFacility;
                var job = new Job
                {
                    guid = Guid.NewGuid().ToString(),
                    group = jobTemplate.group,
                    name = $"{sourceName}to{destinationName}",
                    orderId = orderId,
                    type = jobTemplate.type,
                    subType = jobTemplate.subType,
                    sequence = 0,
                    carrierId = carrierId,
                    drumKeyCode = drumKeyCode,
                    priority = priority,
                    sourceId = sourceId,
                    sourceName = sourceName,
                    sourcelinkedFacility = sourcelinkedFacility,
                    destinationId = destinationId,
                    destinationName = destinationName,
                    destinationlinkedFacility = destinatiolinkedFacility,
                    isLocked = jobTemplate.isLocked,
                    state = nameof(JobState.INIT),
                    specifiedWorkerId = specifiedWorkerId,
                    assignedWorkerId = assignedWorkerId,
                    createdAt = DateTime.Now,
                    updatedAt = null,
                    finishedAt = null,
                    terminationType = null,
                    terminateState = null,
                    terminator = null,
                    terminatingAt = null,
                    terminatedAt = null
                };

                List<Mission> _Createmissions = new List<Mission>();
                int missionsequence = 1;
                foreach (var missionTemplate in jobTemplate.missionTemplates)
                {
                    var mission = new Mission
                    {
                        orderId = orderId,
                        jobId = job.guid,
                        guid = Guid.NewGuid().ToString(),
                        carrierId = carrierId,
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
                    _Createmissions.Add(mission);
                    missionsequence++;
                }

                if (job != null && _Createmissions.Count > 0)
                {
                    create(orderId, jobTemplate, job, _Createmissions);
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

        private void create(string orderId, JobTemplate jobTemplate, Job job, List<Mission> missions)
        {
            _repository.Jobs.Add(job);
            _repository.JobHistorys.Add(job);
            _mqttQueue.MqttPublishMessage(TopicType.job, TopicSubType.status, _mapping.Jobs.MqttPublish(job));

            foreach (var mission in missions)
            {
                _repository.Missions.Add(mission);
                _repository.MissionHistorys.Add(mission);
                _mqttQueue.MqttPublishMessage(TopicType.mission, TopicSubType.status, _mapping.Missions.MqttPublish(mission));
            }

            if (orderId != null)
            {
                var order = _repository.Orders.GetByIdAndTypeAndSubType(orderId, jobTemplate.type, jobTemplate.subType);
                if (order != null)
                {
                    order.state = nameof(OrderState.Waiting);
                    order.stateCode = OrderState.Waiting;
                    order.updatedAt = DateTime.Now;
                    _repository.Orders.Update(order);
                    _repository.OrderHistorys.Add(order);
                    _mqttQueue.MqttPublishMessage(TopicType.order, TopicSubType.status, _mapping.Orders.MqttPublish(order));
                }
            }
        }

        private Parameter missionParameter(MissionTemplate missionTemplate, Job job, Parameter parameta, string drumKeyCode
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

        public void RemoveJob_Mission()
        {
            while (QueueStorage.RemoveTryDequeueJobMission(out var cmd))
            {
                var target = cmd.job;
                var finishedAt = cmd.finishedAt;
                // TransactionScope 사용하면 DB를 Open상태로 계속 열어두어서 DataBaseConnectTimeOut진행됨
                //using (var trxScope = new TransactionScope())
                //{
                var job = _repository.Jobs.GetByid(target.guid);
                if (job != null)
                {
                    foreach (var mission in _repository.Missions.GetByJobId(job.guid))
                    {
                        if (mission.finishedAt == null)
                        {
                            mission.finishedAt = finishedAt;
                        }
                        _repository.MissionHistorys.Add(mission);
                        _repository.MissionFinishedHistorys.Add(mission);
                        _repository.Missions.Remove(mission);
                    }
                    job.finishedAt = finishedAt;
                    _repository.JobHistorys.Add(job);
                    _repository.JobFinishedHistorys.Add(job);
                    _repository.Jobs.Remove(job);
                }

                //trxScope.Complete();
                //}
            }
        }
    }
}