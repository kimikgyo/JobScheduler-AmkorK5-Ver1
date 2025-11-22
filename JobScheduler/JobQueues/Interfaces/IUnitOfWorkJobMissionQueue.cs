using Common.DTOs.Rests.Orders;
using Common.Models.Jobs;
using Common.Templates;

namespace JOB.JobQueues.Interfaces
{
    public interface IUnitOfWorkJobMissionQueue : IDisposable
    {
        void CreateOrder(Post_OrderDto addRequestorder);

        void RemoveOrder(Order target, DateTime? finishedAt);

        void CreateJobMission(JobTemplate jobTemplate, string orderId, string carrierId, int priority, string drumKeyCode
                            , string sourceId, string sourceName, string sourcelinkedFacility
                            , string destinationId, string destinationName, string destinationlinkedFacility
                            , string specifiedWorkerId, string assignedWorkerId);

        void RemoveJobMission(Job job, DateTime? finishedAt);

        void ProcessAllOrder();

        void ProcessAllJob();
    }
}