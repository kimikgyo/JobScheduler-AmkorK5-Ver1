using Common.DTOs.Rests.Orders;
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

    public class Add_Order
    {
        public Post_OrderDto post_Order;
    }

    public class Remove_Order
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