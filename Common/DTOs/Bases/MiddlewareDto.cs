using Microsoft.AspNetCore.Http.HttpResults;
using System.Reflection.Emit;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;

namespace Common.DTOs.Bases
{
    public class ApiGetResponseDtoResourceMiddleware
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
    public class MqttSubscribeDtoMiddlewareStatus
    {
        [JsonPropertyOrder(1)] public bool isOnline { get; set; }
        [JsonPropertyOrder(2)] public bool isActive { get; set; }
        [JsonPropertyOrder(3)] public string carrier { get; set; }
        [JsonPropertyOrder(4)] public string acsmissionId { get; set; }
        [JsonPropertyOrder(6)] public string state { get; set; }

        public override string ToString()
        {
            return

                $" isOnline = {isOnline,-5}" +
                $",isActive = {isActive,-5}" +
                $",carrier = {carrier,-5}" +
                $",acsmissionId = {acsmissionId,-5}" +
                $",state = {state,-5}";
        }
    }
}
