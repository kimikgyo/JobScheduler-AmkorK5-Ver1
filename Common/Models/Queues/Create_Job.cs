namespace Common.Models.Queues
{
    public class Create_Job
    {
        public string orderId { get; set; }
        public string type { get; set; }
        public string subtype { get; set; }
        public string carrierId { get; set; }
        public string drumKeyCode { get; set; }
        public int priority { get; set; }
        public string groupId { get; set; }
        public string sourceId { get; set; }
        public string sourceName { get; set; }
        public string sourcelinkedFacility { get; set; }
        public string destinationId { get; set; }
        public string destinationName { get; set; }
        public string destinationlinkedFacility { get; set; }
        public string specifiedWorkerId { get; set; }
    }
}
