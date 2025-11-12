using Common.Models.Bases;
using Common.Models.Jobs;
using Swashbuckle.AspNetCore.Annotations;
using System.Text.Json.Serialization;

namespace Common.DTOs.Jobs
{
    public class AddRequestDtoOrder
    {
        [JsonPropertyOrder(1)] public string id { get; set; }
        [JsonPropertyOrder(2)] public string type { get; set; }
        [JsonPropertyOrder(3)] public string subType { get; set; }
        [JsonPropertyOrder(4)] public string sourceId { get; set; }
        [JsonPropertyOrder(5)] public string destinationId { get; set; }
        [JsonPropertyOrder(6)] public string carrierId { get; set; }
        //[JsonPropertyOrder(7)] public string drumKeyCode { get; set; }
        [JsonPropertyOrder(7)] public string orderedBy { get; set; }
        [JsonPropertyOrder(8)] public DateTime orderedAt { get; set; }
        [JsonPropertyOrder(9)] public int priority { get; set; }
        [JsonPropertyOrder(10)] public string specifiedWorkerId { get; set; }

        public override string ToString()
        {
            return

                $" id = {id,-5}" +
                $",type = {type,-5}" +
                $",subType = {subType,-5}" +
                $",sourceId = {sourceId,-5}" +
                $",destinationId = {destinationId,-5}" +
                $",carrierId = {carrierId,-5}" +
                //$",drumKeyCode = {drumKeyCode,-5}" +
                $",orderedBy = {orderedBy,-5}" +
                $",orderedAt = {orderedAt,-5}" +
                $",priority = {priority,-5}" +
                $",specifiedWorkerId = {specifiedWorkerId,-5}";
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

    public class ResponseDtoOrder
    {
        [JsonPropertyOrder(1)] public string id { get; set; }                  // 고유 식별자
        [JsonPropertyOrder(2)] public string type { get; set; }            // 운반, 충전, 대기 등
        [JsonPropertyOrder(3)] public string subType { get; set; }            // 운반, 충전, 대기 등
        [JsonPropertyOrder(4)] public string sourceId { get; set; }            // 출발 위치 ID
        [JsonPropertyOrder(5)] public string destinationId { get; set; }           // 도착 위치 ID
        [JsonPropertyOrder(6)] public string carrierId { get; set; }              // 자재 ID (nullable)
        [JsonPropertyOrder(6)] public string drumKeyCode { get; set; }              // 자재 ID (nullable)
        [JsonPropertyOrder(7)] public string orderedBy { get; set; }                 // 지시자 ID 또는 이름
        [JsonPropertyOrder(8)] public DateTime orderedAt { get; set; }
        [JsonPropertyOrder(9)] public int priority { get; set; } = 0;                      // 기본 우선순위
        [JsonPropertyOrder(10)] public string state { get; set; }                         // 상태: 대기, 진행중, 완료 등
        [JsonPropertyOrder(11)] public OrderState stateCode { get; set; }
        [JsonPropertyOrder(13)] public string specifiedWorkerId { get; set; }            //지정된 Worker
        [JsonPropertyOrder(14)] public string assignedWorkerId { get; set; }             //할당된 Worker
        [JsonPropertyOrder(15)] public DateTime createdAt { get; set; }            // 생성 시각
        [JsonPropertyOrder(16)] public DateTime? updatedAt { get; set; }
        [JsonPropertyOrder(17)] public DateTime? finishedAt { get; set; }
        [JsonPropertyOrder(18)] public ResponseDtoJob Job { get; set; }

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
                $",assignedWorkerId = {assignedWorkerId,-5}" +
                $",createdAt = {createdAt,-5}" +
                $",updatedAt = {updatedAt,-5}" +
                $",finishedAt = {finishedAt,-5}" +
                $",Job = {Job,-5}";
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

    public class MqttPublishDtoOrder
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