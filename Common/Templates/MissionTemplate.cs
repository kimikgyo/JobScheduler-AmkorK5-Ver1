using Common.Models.Bases;
using System.Text.Json.Serialization;

namespace Common.Templates
{
    public class MissionTemplate
    {
        //[JsonProperty(Order = 1)]
        //public int jobTemplateId { get; set; }

        [JsonPropertyOrder(1)] public string name { get; set; }
        [JsonPropertyOrder(2)] public string service { get; set; }
        [JsonPropertyOrder(3)] public string type { get; set; }
        [JsonPropertyOrder(4)] public string subType { get; set; }
        [JsonPropertyOrder(5)] public bool isLook { get; set; }
        [JsonPropertyOrder(6)] public List<Parameter> parameters { get; set; } = new List<Parameter>();
        [JsonPropertyOrder(7)] public List<PreReport> preReports { get; set; } = new List<PreReport>();
        [JsonPropertyOrder(8)] public List<PostReport> postReports { get; set; } = new List<PostReport>();

        // 사람용 요약 (디버거/로그에서 보기 좋게)
        public override string ToString()
        {
            return
                $"name = {name,-5}" +
                $"service = {service,-5}" +
                $",type = {type,-5}" +
                $",subType = {subType,-5}" +
                $",isLook = {isLook,-5}" +
                $",parameters = {parameters,-5}" +
                $",preReports = {preReports,-5}" +
                $",postReports = {postReports,-5}";
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