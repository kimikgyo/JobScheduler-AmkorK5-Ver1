using System.Text.Json.Serialization;

namespace Common.DTOs.MQTTs.Jobs
{
    public class Publish_JobDto
    {
        [JsonPropertyOrder(1)] public string guid { get; set; }                          // 고유 식별자
        [JsonPropertyOrder(2)] public string group { get; set; }                          // 고유 식별자
        [JsonPropertyOrder(3)] public string name { get; set; }
        [JsonPropertyOrder(4)] public string orderId { get; set; }                  // 상위 오더 참조
        [JsonPropertyOrder(5)] public string type { get; set; }
        [JsonPropertyOrder(6)] public string subType { get; set; }
        [JsonPropertyOrder(7)] public int priority { get; set; }                     // 기본 우선순위
        [JsonPropertyOrder(8)] public int sequence { get; set; }                   // Job 실행 순서
        [JsonPropertyOrder(9)] public string carrierId { get; set; }
        [JsonPropertyOrder(9)] public string drumKeyCode { get; set; }
        [JsonPropertyOrder(10)] public string sourceId { get; set; }                     // 시작 위치
        [JsonPropertyOrder(11)] public string sourceName { get; set; }
        [JsonPropertyOrder(11)] public string sourcelinkedFacility { get; set; }
        [JsonPropertyOrder(12)] public string destinationId { get; set; }                // 도착 위치
        [JsonPropertyOrder(13)] public string destinationName { get; set; }
        [JsonPropertyOrder(13)] public string destinationlinkedFacility { get; set; }
        [JsonPropertyOrder(14)] public bool isLocked { get; set; }                       // 취소 불가
        [JsonPropertyOrder(15)] public string state { get; set; }                       // 상태: 대기, 실행중, 완료 등
        [JsonPropertyOrder(16)] public string specifiedWorkerId { get; set; }            //order 지정된 Worker
        [JsonPropertyOrder(17)] public string assignedWorkerId { get; set; }             //할당된 Worker
        [JsonPropertyOrder(18)] public string terminationType { get; set; }     //초기데이터 null
        [JsonPropertyOrder(19)] public string terminateState { get; set; }      //초기데이터 null
        [JsonPropertyOrder(20)] public string terminator { get; set; }          //초기데이터 null

        public override string ToString()
        {
            return
                $" guid = {guid,-5}" +
                $",group = {group,-5}" +
                $",name = {name,-5}" +
                $",orderId = {orderId,-5}" +
                $",type = {type,-5}" +
                $",subType = {subType,-5}" +
                $",priority = {priority,-5}" +
                $",sequence = {sequence,-5}" +
                $",carrierId = {carrierId,-5}" +
                $",drumKeyCode = {drumKeyCode,-5}" +
                $",sourceId = {sourceId,-5}" +
                $",sourceName = {sourceName,-5}" +
                $",sourcelinkedFacility = {sourcelinkedFacility,-5}" +
                $",destinationId = {destinationId,-5}" +
                $",destinationName = {destinationName,-5}" +
                $",destinationlinkedFacility = {destinationlinkedFacility,-5}" +
                $",isLocked = {isLocked,-5}" +
                $",state = {state,-5}" +
                $",specifiedWorkerId = {specifiedWorkerId,-5}" +
                $",assignedWorkerId = {assignedWorkerId,-5}" +
                $",terminationType = {terminationType,-5}" +
                $",terminateState = {terminateState,-5}" +
                $",terminator = {terminator,-5}";
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
