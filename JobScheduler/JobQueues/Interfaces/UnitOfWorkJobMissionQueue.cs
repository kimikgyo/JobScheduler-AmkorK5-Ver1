using Common.DTOs.Rests.Orders;
using Common.Models.Jobs;
using Common.Models.Queues;
using Data.Interfaces;
using JOB.JobQueues.Process;
using JOB.Mappings.Interfaces;
using JOB.MQTTs.Interfaces;

namespace JOB.JobQueues.Interfaces
{
    public class UnitOfWorkJobMissionQueue : IUnitOfWorkJobMissionQueue
    {
        private readonly QueueProcess _queueProcess;

        public UnitOfWorkJobMissionQueue(IUnitOfWorkRepository repository, IUnitofWorkMqttQueue mqttQueue, IUnitOfWorkMapping mapping)
        {
            _queueProcess = new QueueProcess(repository, mqttQueue, mapping);
        }

        public void Create_Order(Post_OrderDto post_OrderDto)
        {
            QueueStorage.Add_Order_Enqueue(new Add_Order
            {
                post_Order = post_OrderDto
            });
            _queueProcess.Add_Order();
        }

        public void Remove_Order(Order target, DateTime? finishedAt)
        {
            QueueStorage.Remove_Order_Enqueue(new Remove_Order
            {
                orderTarget = target,
                finishedAt = finishedAt,
            });
            _queueProcess.Remove_Order_Job_Mission();
        }

        public void Create_Job(string group, string orderId, string type, string subtype, string carrierId, int priority, string drumKeyCode
                                    , string sourceId, string sourceName, string sourcelinkedFacility
                                    , string destinationId, string destinationName, string destinationlinkedFacility
                                    , string specifiedWorkerId, string assignedWorkerId)
        {
            QueueStorage.Add_Job_Enqueue(new Add_Job
            {
                orderId = orderId,
                type = type,
                subtype = subtype,
                carrierId = carrierId,
                drumKeyCode = drumKeyCode,
                groupId = group,
                sourceId = sourceId,
                destinationId = destinationId,
                priority = priority,
                specifiedWorkerId = specifiedWorkerId,
                assignedWorkerId = assignedWorkerId,
                sourceName = sourceName,
                destinationName = destinationName,
                sourcelinkedFacility = sourcelinkedFacility,
                destinationlinkedFacility = destinationlinkedFacility,
            });

            _queueProcess.Add_Job();
        }

        public void Remove_Job(Job job, DateTime? finishedAt)
        {
            QueueStorage.Remove_Job_Enqueue(new Remove_Job
            {
                job = job,
                finishedAt = finishedAt
            });
            _queueProcess.Remove_Job_Mission();
        }

        public void ProcessAllOrder()
        {
            _queueProcess.Add_Order();
            _queueProcess.RemoveOrder();
        }

        public void ProcessAllJob()
        {
            _queueProcess.AddJob_Mission();
        }

        public void SaveChanges()
        {
        }

        public void Dispose()
        {
        }
    }
}