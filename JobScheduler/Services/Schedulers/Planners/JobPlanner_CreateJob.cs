using Common.Models;
using Common.Models.Jobs;

namespace JOB.Services
{
    public partial class SchedulerService
    {
        private readonly object _lock = new object();

        private void Create_Job(string group, string orderId, string type, string subtype, string carrierId, int priority, string drumKeyCode
                           , string sourceId, string sourceName, string sourcelinkedFacility
                           , string destinationId, string destinationName, string destinationlinkedFacility
                           , string specifiedWorkerId)
        {
            lock (_lock)
            {
                var job = new Job
                {
                    guid = Guid.NewGuid().ToString(),
                    group = group,
                    name = $"{sourceName}to{destinationName}",
                    orderId = orderId,
                    type = type,
                    subType = subtype,
                    sequence = 0,
                    carrierId = carrierId,
                    drumKeyCode = drumKeyCode,
                    priority = priority,
                    sourceId = sourceId,
                    sourceName = sourceName,
                    sourcelinkedFacility = sourcelinkedFacility,
                    destinationId = destinationId,
                    destinationName = destinationName,
                    destinationlinkedFacility = destinationlinkedFacility,
                    isLocked = false,
                    state = nameof(JobState.INIT),
                    specifiedWorkerId = specifiedWorkerId,
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
                _mqttQueue.MqttPublishMessage(TopicType.job, nameof(TopicSubType.status), _mapping.Jobs.Publish(job));

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
                        _mqttQueue.MqttPublishMessage(TopicType.order, nameof(TopicSubType.status), _mapping.Orders.Publish(order));
                    }
                }
            }
        }
    }
}