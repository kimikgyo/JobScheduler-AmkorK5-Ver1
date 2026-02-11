using Common.DTOs.Rests.Orders;
using Common.Models.Jobs;
using Common.Templates;

namespace JOB.JobQueues.Interfaces
{
    public interface IUnitOfWorkJobMissionQueue : IDisposable
    {
        void Create_Order(Post_OrderDto addRequestorder);

        void Remove_Order(Order target, DateTime? finishedAt);

        //void Create_Job(string group, string orderId, string type, string subtype, string carrierId, int priority, string drumKeyCode
        //                           , string sourceId, string sourceName, string sourcelinkedFacility
        //                           , string destinationId, string destinationName, string destinationlinkedFacility
        //                           , string specifiedWorkerId);

        void Remove_Job(Job job, DateTime? finishedAt);

        void Create_Mission(Job job, MissionTemplate missionTemplate, Position position, Worker worker, int seq);
    }
}