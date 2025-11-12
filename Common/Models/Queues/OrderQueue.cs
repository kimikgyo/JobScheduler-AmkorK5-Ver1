using Common.DTOs.Jobs;
using Common.Models.Jobs;

namespace Common.Models.Queues
{
    public enum CodeOrder
    {
        ADD,
        UPDATE,
        CANCEL,
        REMOVE
    }

    public class AddOrder
    {
        public AddRequestDtoOrder AddRequestOrder;
    }

    public class RemoveOrder
    {
        public Order orderTarget;
        public DateTime? finishedAt;
    }

    public class MQTTOrder
    {
        public string Topic { get; set; }
        public object Payload { get; set; }
        public DateTime Timestamp { get; set; }
    }
}