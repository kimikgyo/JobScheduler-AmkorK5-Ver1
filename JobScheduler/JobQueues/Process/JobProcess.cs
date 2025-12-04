using Common.Models;
using Common.Models.Jobs;
using Common.Models.Queues;
using Common.Templates;

namespace JOB.JobQueues.Process
{
    public partial class QueueProcess
    {
        public void Crate_Job()
        {
            while (QueueStorage.Add_Job_TryDequeue(out var cmd))
            {
                var job = new Job
                {
                    guid = Guid.NewGuid().ToString(),
                    group = cmd.groupId,
                    name = $"{cmd.sourceName}to{cmd.destinationName}",
                    orderId = cmd.orderId,
                    type = cmd.type,
                    subType = cmd.subtype,
                    sequence = 0,
                    carrierId = cmd.carrierId,
                    drumKeyCode = cmd.drumKeyCode,
                    priority = cmd.priority,
                    sourceId = cmd.sourceId,
                    sourceName = cmd.sourceName,
                    sourcelinkedFacility = cmd.sourcelinkedFacility,
                    destinationId = cmd.destinationId,
                    destinationName = cmd.destinationName,
                    destinationlinkedFacility = cmd.destinationlinkedFacility,
                    isLocked = false,
                    state = nameof(JobState.INIT),
                    specifiedWorkerId = cmd.specifiedWorkerId,
                    createdAt = DateTime.Now,
                    updatedAt = null,
                    finishedAt = null,
                    terminationType = null,
                    terminateState = null,
                    terminator = null,
                    terminatingAt = null,
                    terminatedAt = null
                };
                _repository.Jobs.Add(job);
                _repository.JobHistorys.Add(job);
                _mqttQueue.MqttPublishMessage(TopicType.job, TopicSubType.status, _mapping.Jobs.Publish(job));

                if (job.orderId != null)
                {
                    var order = _repository.Orders.GetByid(job.orderId);
                    if (order != null)
                    {
                        order.state = nameof(OrderState.Waiting);
                        order.stateCode = OrderState.Waiting;
                        order.updatedAt = DateTime.Now;
                        _repository.Orders.Update(order);
                        _repository.OrderHistorys.Add(order);
                        _mqttQueue.MqttPublishMessage(TopicType.order, TopicSubType.status, _mapping.Orders.Publish(order));
                    }
                }
            }
        }
        public void Remove_Job_Mission()
        {
            while (QueueStorage.Remove_Job_TryDequeue(out var cmd))
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