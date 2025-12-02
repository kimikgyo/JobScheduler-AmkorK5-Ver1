using Common.Models.Bases;
using System.Text.Json.Serialization;

namespace Common.Templates
{
    public class MissionTemplate_Single
    {
        //[JsonProperty(Order = 1)]
        //public int jobTemplateId { get; set; }
        [JsonPropertyOrder(1)] public string guid { get; set; }

        [JsonPropertyOrder(2)] public string name { get; set; }
        [JsonPropertyOrder(3)] public string service { get; set; }
        [JsonPropertyOrder(4)] public string type { get; set; }
        [JsonPropertyOrder(5)] public string subType { get; set; }
        [JsonPropertyOrder(6)] public bool isLook { get; set; }
        [JsonPropertyOrder(7)] public List<Parameter> parameters { get; set; } = new List<Parameter>();
        [JsonPropertyOrder(8)] public string parametersJson { get; set; }        // DB 파라메타를 저장하기 위하여
        [JsonPropertyOrder(9)] public List<PreReport> preReports { get; set; } = new List<PreReport>();
        [JsonPropertyOrder(10)] public string preReportsJson { get; set; }        //Mission 전에 보내지는 Report
        [JsonPropertyOrder(11)] public List<PostReport> postReports { get; set; } = new List<PostReport>();
        [JsonPropertyOrder(12)] public string postReportsJson { get; set; }        //Mission 이후에 보내지는 Report
        [JsonPropertyOrder(13)] public DateTime createdAt { get; set; }                  // 생성 시각
        [JsonPropertyOrder(14)] public DateTime? updatedAt { get; set; }

        // 사람용 요약 (디버거/로그에서 보기 좋게)
        public override string ToString()
        {
            string parametersStr;
            string preReportsStr;
            string postReportsStr;

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
                parametersStr = "{}";
            }

            if (preReports != null && preReports.Count > 0)
            {
                // 리스트 안의 Parameter 각각을 { ... } 모양으로 변환
                var items = preReports
                    .Select(p => $"{{ ceid={p.ceid}, eventName={p.eventName},rptid = {p.rptid} }}");

                // 여러 개 항목을 ", " 로 이어붙임
                preReportsStr = string.Join(", ", items);
            }
            else
            {
                preReportsStr = "{}";
            }

            if (postReports != null && postReports.Count > 0)
            {
                // 리스트 안의 Parameter 각각을 { ... } 모양으로 변환
                var items = postReports
                    .Select(p => $"{{ ceid={p.ceid}, eventName={p.eventName},rptid = {p.rptid} }}");

                // 여러 개 항목을 ", " 로 이어붙임
                postReportsStr = string.Join(", ", items);
            }
            else
            {
                postReportsStr = "{}";
            }
            return
                 $"guid = {guid,-5}" +
                $"name = {name,-5}" +
                $"service = {service,-5}" +
                $",type = {type,-5}" +
                $",subType = {subType,-5}" +
                $",isLook = {isLook,-5}" +
                $",createdAt = {createdAt,-5}" +
                $",updatedAt = {updatedAt,-5}" +
                $",parametersJson = {parametersJson,-5}" +
                $",parameters = [{parametersStr,-5}]" +
                $",preReportsJson = {preReportsJson,-5}" +
                $",preReports = [{preReportsStr,-5}]" +
                $",postReportsJson = {postReportsJson,-5}" +
                $",postReports = [{postReportsStr,-5}]";
        }

        // 기계용 JSON (전송/저장에만 사용)
        //public string ToJson(bool indented = false)
        //{
        //    return JsonSerializer.Serialize(
        //        this,
        //        new JsonSerializerOptions
        //        {
        //            IncludeFields = true,
        //            WriteIndented = indented
        //        });
        //}
    }
}