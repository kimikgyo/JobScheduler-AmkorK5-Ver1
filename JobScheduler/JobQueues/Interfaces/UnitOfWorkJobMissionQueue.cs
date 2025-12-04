using Common.DTOs.Rests.Orders;
using Common.Models.Jobs;
using Common.Models.Queues;
using Common.Templates;

namespace JOB.JobQueues.Interfaces
{
    public class UnitOfWorkJobMissionQueue : IUnitOfWorkJobMissionQueue
    {
        public void Create_Order(Post_OrderDto post_OrderDto)
        {
            QueueStorage.Create_Order_Enqueue(new Create_Order
            {
                post_Order = post_OrderDto
            });
        }

        public void Remove_Order(Order target, DateTime? finishedAt)
        {
            QueueStorage.Remove_Order_Enqueue(new Remove_Order
            {
                orderTarget = target,
                finishedAt = finishedAt,
            });
        }

        public void Create_Job(string group, string orderId, string type, string subtype, string carrierId, int priority, string drumKeyCode
                                    , string sourceId, string sourceName, string sourcelinkedFacility
                                    , string destinationId, string destinationName, string destinationlinkedFacility
                                    , string specifiedWorkerId)
        {
            QueueStorage.Create_Job_Enqueue(new Create_Job
            {
                orderId = orderId,
                groupId = group,
                type = type,
                subtype = subtype,
                carrierId = carrierId,
                drumKeyCode = drumKeyCode,
                sourceId = sourceId,
                sourceName = sourceName,
                sourcelinkedFacility = sourcelinkedFacility,
                destinationId = destinationId,
                destinationName = destinationName,
                destinationlinkedFacility = destinationlinkedFacility,
                priority = priority,
                specifiedWorkerId = specifiedWorkerId,
            });
        }

        public void Remove_Job(Job job, DateTime? finishedAt)
        {
            QueueStorage.Remove_Job_Enqueue(new Remove_Job
            {
                job = job,
                finishedAt = finishedAt
            });
        }

        public void Create_Mission(Job job, MissionTemplate missionTemplate,Position position,Worker worker, int seq)
        {
            QueueStorage.Create_Mission_Enqueue(new Create_Mission
            {
                job = job,
                position = position,
                missionTemplate = missionTemplate,
                worker = worker,
                seq = seq
            });
        }

        public void SaveChanges()
        {
        }

        public void Dispose()
        {
        }
    }
}