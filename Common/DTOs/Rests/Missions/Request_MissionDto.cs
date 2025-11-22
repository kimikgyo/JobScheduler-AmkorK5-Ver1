using Common.Models.Bases;
using Common.Templates;
using System.Text.Json.Serialization;

namespace Common.DTOs.Rests.Missions
{
    public class Request_MissionDto
    {
        [JsonPropertyOrder(1)] public string orderId { get; set; }
        [JsonPropertyOrder(2)] public string jobId { get; set; }
        [JsonPropertyOrder(3)] public string guid { get; set; }
        [JsonPropertyOrder(4)] public string carrierId { get; set; }              // 자재 ID (nullable)
        [JsonPropertyOrder(5)] public string name { get; set; }              // 자재 ID (nullable)
        [JsonPropertyOrder(6)] public string service { get; set; }
        [JsonPropertyOrder(7)] public string type { get; set; }
        [JsonPropertyOrder(8)] public string subType { get; set; }
        [JsonPropertyOrder(9)] public int sequence { get; set; }
        [JsonPropertyOrder(10)] public string linkedFacility { get; set; }
        [JsonPropertyOrder(11)] public bool isLocked { get; set; }
        [JsonPropertyOrder(12)] public int sequenceChangeCount { get; set; } = 0;
        [JsonPropertyOrder(13)] public int retryCount { get; set; } = 0;
        [JsonPropertyOrder(14)] public string state { get; set; }
        [JsonPropertyOrder(15)] public string specifiedWorkerId { get; set; }
        [JsonPropertyOrder(16)] public string assignedWorkerId { get; set; }
        [JsonPropertyOrder(17)] public List<Parameter> parameters { get; set; }

        // 사람용 요약 (디버거/로그에서 보기 좋게)
        public override string ToString()
        {
            string parametersStr;

            if (parameters != null && parameters.Count > 0)
            {
                // 리스트 안의 Parameter 각각을 { ... } 모양으로 변환
                var items = parameters
                    .Select(p => $"{{ key={p.key}, value={p.value} }}");

                // 여러 개 항목을 ", " 로 이어붙임
                parametersStr = string.Join(", ", items);
            }
            else
            {
                // 값이 없으면 빈 중괄호로 표시
                parametersStr = "";
            }
            return

              $" orderId = {orderId,-5}" +
              $",jobId = {jobId,-5}" +
              $",guid = {guid,-5}" +
              $",carrierId = {carrierId,-5}" +
              $",name = {name,-5}" +
              $",service = {service,-5}" +
              $",type = {type,-5}" +
              $",subType = {subType,-5}" +
              $",sequence = {sequence,-5}" +
              $",linkedFacility = {linkedFacility,-5}" +
              $",isLocked = {isLocked,-5}" +
              $",sequenceChangeCount = {sequenceChangeCount,-5}" +
              $",retryCount = {retryCount,-5}" +
              $",state = {state,-5}" +
              $",specifiedWorkerId = {specifiedWorkerId,-5}" +
              $",assignedWorkerId = {assignedWorkerId,-5}" +

                $",parameters = {parametersStr,-5}";
        }
    }

}
