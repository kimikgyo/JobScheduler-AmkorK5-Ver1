using System.Text.Json.Serialization;

namespace Common.Models.Bases
{
    public class Parameter
    {
        [JsonPropertyOrder(1)] public string key { get; set; }
        [JsonPropertyOrder(2)] public string value { get; set; }

        // 사람용 요약 (디버거/로그에서 보기 좋게)
        public override string ToString()
        {
            return
                $"key = {key,-5}" +
                $",value = {value,-5}";
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