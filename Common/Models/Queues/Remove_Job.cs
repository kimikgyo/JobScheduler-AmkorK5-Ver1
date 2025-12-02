using Common.Models.Jobs;

namespace Common.Models.Queues
{
    public class Remove_Job
    {
        public Job job { get; set; }
        public DateTime? finishedAt { get; set; }
    }
}
