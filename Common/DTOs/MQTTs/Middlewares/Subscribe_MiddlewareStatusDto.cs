using System.Text.Json.Serialization;

namespace Common.DTOs.MQTTs.Middlewares
{
    public class Subscribe_MiddlewareStatusDto
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
