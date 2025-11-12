using System.Text.Json.Serialization;

namespace Common.Models.Bases
{
    public enum MiddlewareState
    {
        NONE,
        IDLE,
        WORKING,
        MANUAL,
        ERROR
    }

    public class Middleware
    {
        [JsonPropertyOrder(1)] public string workerId { get; set; }
        [JsonPropertyOrder(2)] public string id { get; set; }
        [JsonPropertyOrder(3)] public string ip { get; set; }
        [JsonPropertyOrder(4)] public int port { get; set; }
        [JsonPropertyOrder(5)] public bool isOnline { get; set; }
        [JsonPropertyOrder(6)] public bool isActive { get; set; }
        [JsonPropertyOrder(7)] public string carrier { get; set; }
        [JsonPropertyOrder(8)] public string acsmissionId { get; set; }
        [JsonPropertyOrder(10)] public string state { get; set; } = nameof(MiddlewareState.IDLE);

        public override string ToString()
        {
            return
                $" workerId = {workerId,-5}" +
                $",id = {id,-5}" +
                $",ip = {ip,-5}" +
                $",port = {port,-5}" +
                $",isOnline = {isOnline,-5}" +
                $",isActive = {isActive,-5}" +
                $",carrier = {carrier,-5}" +
                $",acsmissionId = {acsmissionId,-5}" +
                $",state = {state,-5}";
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