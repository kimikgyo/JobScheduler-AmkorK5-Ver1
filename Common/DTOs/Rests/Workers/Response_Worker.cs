using Common.DTOs.MQTTs.Workers;
using Common.DTOs.Rests.Middlewares;
using System.Text.Json.Serialization;

namespace Common.DTOs.Rests.Workers
{
    public class Response_Worker
    {
        [JsonPropertyOrder(1)] public string _id { get; set; }
        [JsonPropertyOrder(2)] public string source { get; set; }
        [JsonPropertyOrder(3)] public string workerId { get; set; }
        [JsonPropertyOrder(4)] public int __v { get; set; }
        [JsonPropertyOrder(5)] public string apiUrl { get; set; }
        [JsonPropertyOrder(6)] public List<Subscribe_CapabilitiesDto> capabilities { get; set; }
        [JsonPropertyOrder(7)] public string createdAt { get; set; }
        [JsonPropertyOrder(8)] public string createdBy { get; set; }
        [JsonPropertyOrder(9)] public string groupId { get; set; }
        [JsonPropertyOrder(10)] public string ipAddress { get; set; }
        [JsonPropertyOrder(11)] public string loginId { get; set; }
        [JsonPropertyOrder(12)] public string name { get; set; }
        [JsonPropertyOrder(13)] public string password { get; set; }
        [JsonPropertyOrder(14)] public Response_MiddlewareDto Middleware { get; set; }

        public override string ToString()
        {
            return
                $"_id = {_id,-5}" +
                $",source = {source,-5}" +
                $",workerId = {workerId,-5}" +
                $",__v = {__v,-5}" +
                $",apiUrl = {apiUrl,-5}" +
                $",capabilities = {capabilities,-5}" +
                $",createdAt = {createdAt,-5}" +
                $",createdBy = {createdBy,-5}" +
                $",ipAddress = {ipAddress,-5}" +
                $",groupId = {groupId,-5}" +
                $",loginId = {loginId,-5}" +
                $",name = {name,-5}" +
                $",password = {password,-5}" +
                $",Middleware = {Middleware,-5}";
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