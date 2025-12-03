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
            QueueStorage.Add_Order_Enqueue(new Add_Order
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
        }

        public void Remove_Job(Job job, DateTime? finishedAt)
        {
            QueueStorage.Remove_Job_Enqueue(new Remove_Job
            {
                job = job,
                finishedAt = finishedAt
            });
        }

        public void Add_Mission(Job job, MissionTemplate missionTemplate, int seq)
        {
            QueueStorage.Add_Mission_Enqueue(new Add_Mission
            {
                job = job,
                missionTemplate = missionTemplate,
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