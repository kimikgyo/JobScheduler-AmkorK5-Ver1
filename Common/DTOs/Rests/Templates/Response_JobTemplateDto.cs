using System.Text.Json.Serialization;

namespace Common.DTOs.Rests.JobTemplates
{
    public class Response_JobTemplateDto
    {
        [JsonPropertyOrder(1)] public int id { get; set; }
        [JsonPropertyOrder(2)] public string group { get; set; }
        [JsonPropertyOrder(3)] public string type { get; set; }
        [JsonPropertyOrder(4)] public string subType { get; set; }
        [JsonPropertyOrder(5)] public bool isLocked { get; set; }
        [JsonPropertyOrder(6)] public List<Response_MissionTemplateDto> missionTemplates { get; set; }

        public override string ToString()
        {
            string missionTemplatesStr;

            if (missionTemplates != null && missionTemplates.Count > 0)
            {
                // 리스트 안의 Parameter 각각을 { ... } 모양으로 변환
                var items = missionTemplates
                    .Select(p => $"{{ service = {p.service}" +
                                    $",type = {p.type}" +
                                    $",subType = {p.subType}" +
                                    $",isLook = {p.isLook}}}");

                // 여러 개 항목을 ", " 로 이어붙임
                missionTemplatesStr = string.Join(", ", items);
            }
            else
            {
                // 값이 없으면 빈 중괄호로 표시
                missionTemplatesStr = "{}";
            }
            return
                    $"id = {id,-5}" +
                    $",group = {group,-5}" +
                    $",type = {type,-5}" +
                    $",subType = {subType,-5}" +
                    $",isLocked = {isLocked,-5}" +
                    $",missionTemplates = [{missionTemplatesStr,-5}]";
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