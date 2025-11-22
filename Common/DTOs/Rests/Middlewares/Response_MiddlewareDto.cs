using System.Text.Json.Serialization;

namespace Common.DTOs.Rests.Middlewares
{
    public class Response_MiddlewareDto
    {
        [JsonPropertyOrder(1)] public string _id { get; set; }
        [JsonPropertyOrder(2)] public string ip { get; set; }
        [JsonPropertyOrder(3)] public int port { get; set; }

        public override string ToString()
        {
            return
                $"_id = {_id,-5}" +
                $",ip = {ip,-5}" +
                $",port = {port,-5}";
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
