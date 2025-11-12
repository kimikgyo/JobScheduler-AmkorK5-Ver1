using Common.Models.Jobs;
using Common.Templates;

namespace Common.Models.Queues
{
    public enum CodeJob
    {
        ADD,
        UPDATE,
        CANCEL,
        REMOVE
    }

    public class AddJobMission
    {
        public string orderId { get; set; }
        public string carrierId { get; set; }
        public string drumKeyCode { get; set; }
        public JobTemplate jobTemplate { get; set; }
        public int priority { get; set; }
        public string sourceId { get; set; }
        public string sourceName { get; set; }
        public string sourcelinkedFacility { get; set; }
        public string destinationId { get; set; }
        public string destinationName { get; set; }
        public string destinationlinkedFacility { get; set; }
        public string specifiedWorkerId { get; set; }
        public string assignedWorkerId { get; set; }
    }
    public class RemoveJobMission
    {
        public Job job { get; set; }
        public DateTime? finishedAt { get; set; }
    }
    public class AddMission
    {
        public JobTemplate jobTemplate { get; set; }
        public string jobId { get; set; }
    }
}