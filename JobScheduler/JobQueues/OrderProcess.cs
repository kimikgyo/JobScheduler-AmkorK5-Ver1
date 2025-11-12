using Common.Models;
using Common.Models.Jobs;
using Common.Models.Queues;
using Data.Interfaces;
using JOB.Mappings.Interfaces;
using JOB.MQTTs.Interfaces;

namespace JOB.JobQueues
{
    public partial class QueueProcess
    {
        private readonly IUnitOfWorkRepository _repository;
        private readonly IUnitofWorkMqttQueue _mqttQueue;
        private readonly IUnitOfWorkMapping _mapping;

        public QueueProcess(IUnitOfWorkRepository repository, IUnitofWorkMqttQueue mqttQueue, IUnitOfWorkMapping mapping)
        {
            _repository = repository;
            _mqttQueue = mqttQueue;
            _mapping = mapping;
        }

        public void AddOrder()
        {
            while (QueueStorage.AddTryDequeueOrder(out var cmd))
            {
                var addOrder = cmd.AddRequestOrder;

                var order = new Order
                {
                    id = addOrder.id,
                    type = addOrder.type,
                    subType = addOrder.subType,
                    sourceId = addOrder.sourceId,
                    destinationId = addOrder.destinationId,
                    carrierId = addOrder.carrierId,
                    drumKeyCode = addOrder.drumKeyCode,
                    orderedBy = addOrder.orderedBy,
                    orderedAt = addOrder.orderedAt,
                    priority = addOrder.priority,
                    state = nameof(OrderState.Queued),
                    stateCode = OrderState.Queued,
                    specifiedWorkerId = addOrder.specifiedWorkerId,
                    assignedWorkerId = null,
                    createdAt = DateTime.Now,
                    updatedAt = null,
                    finishedAt = null
                };
                // TransactionScope 사용하면 DB를 Open상태로 계속 열어두어서 DataBaseConnectTimeOut진행됨
                //using (var trxScope = new TransactionScope())
                //{
                _repository.Orders.Add(order);
                _repository.OrderHistorys.Add(order);
                _mqttQueue.MqttPublishMessage(TopicType.order, TopicSubType.status, _mapping.Orders.MqttPublish(order));
                //trxScope.Complete();
                //}
            }
        }

        public void RemoveOrder()
        {
            while (QueueStorage.RemoveTryDequeueOrder(out var cmd))
            {
                var target = cmd.orderTarget;
                var finishedAt = cmd.finishedAt;
                // TransactionScope 사용하면 DB를 Open상태로 계속 열어두어서 DataBaseConnectTimeOut진행됨

                //using (var trxScope = new TransactionScope())
                //{
                var job = _repository.Jobs.GetByOrderId(target.id, target.type, target.subType);
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
                target.finishedAt = finishedAt;
                _repository.OrderHistorys.Add(target);
                _repository.OrderFinishedHistorys.Add(target);
                _repository.Orders.Remove(target);
                //trxScope.Complete();
                //}
            }
        }
    }
}