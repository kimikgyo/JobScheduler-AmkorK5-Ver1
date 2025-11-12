using System.Text.Json.Serialization;

namespace Common.Templates
{
    public class JobTemplate
    {
        [JsonPropertyOrder(1)] public int id { get; set; }
        [JsonPropertyOrder(2)] public string group { get; set; }
        [JsonPropertyOrder(3)] public string type { get; set; }
        [JsonPropertyOrder(4)] public string subType { get; set; }
        [JsonPropertyOrder(5)] public bool isLocked { get; set; }
        [JsonPropertyOrder(6)] public List<MissionTemplate> missionTemplates = new List<MissionTemplate>();

        // 사람용 요약 (디버거/로그에서 보기 좋게)
        public override string ToString()
        {
            return
                $"id = {id,-5}" +
                $",group = {group,-5}" +
                $",type = {type,-5}" +
                $",subType = {subType,-5}" +
                $",isLocked = {isLocked,-5}" +
                $",missionTemplates = {missionTemplates,-5}";
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