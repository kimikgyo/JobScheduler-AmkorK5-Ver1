using Common.Models.Jobs;
using System.Text.Json.Serialization;

namespace Common.DTOs.MQTTs.Orders
{
    public class Publish_OrderDto
    {
        [JsonPropertyOrder(1)] public string id { get; set; }
        [JsonPropertyOrder(2)] public string type { get; set; }
        [JsonPropertyOrder(3)] public string subType { get; set; }
        [JsonPropertyOrder(4)] public string sourceId { get; set; }
        [JsonPropertyOrder(5)] public string destinationId { get; set; }
        [JsonPropertyOrder(6)] public string carrierId { get; set; }
        [JsonPropertyOrder(7)] public string drumKeyCode { get; set; }
        [JsonPropertyOrder(8)] public string orderedBy { get; set; }
        [JsonPropertyOrder(9)] public DateTime orderedAt { get; set; }
        [JsonPropertyOrder(10)] public int priority { get; set; }
        [JsonPropertyOrder(11)] public string state { get; set; }
        [JsonPropertyOrder(12)] public OrderState stateCode { get; set; }
        [JsonPropertyOrder(13)] public string specifiedWorkerId { get; set; }            //지정된 Worker
        [JsonPropertyOrder(14)] public string assignedWorkerId { get; set; }             //할당된 Worker

        public override string ToString()
        {
            return

                $" id = {id,-5}" +
                $",type = {type,-5}" +
                $",subType = {subType,-5}" +
                $",sourceId = {sourceId,-5}" +
                $",destinationId = {destinationId,-5}" +
                $",carrierId = {carrierId,-5}" +
                $",drumKeyCode = {drumKeyCode,-5}" +
                $",orderedBy = {orderedBy,-5}" +
                $",orderedAt = {orderedAt,-5}" +
                $",priority = {priority,-5}" +
                $",state = {state,-5}" +
                $",stateCode = {stateCode,-5}" +
                $",specifiedWorkerId = {specifiedWorkerId,-5}" +
                $",assignedWorkerId = {assignedWorkerId,-5}";
        }

        //public string ToJson(bool indented = false)
        //{
        //    return JsonSerializer.Serialize(this, new JsonSerializerOptions
        //    {
        //        IncludeFields = true,
        //        WriteIndented = indented
        //    });
        //}
    }

}
