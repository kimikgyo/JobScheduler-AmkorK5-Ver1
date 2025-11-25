using System.Text.Json.Serialization;

namespace Common.DTOs.MQTTs.Elevator
{
    public class Subscribe_ElevatorStatusDto
    {
        [JsonPropertyOrder(1)] public string id { get; set; }
        [JsonPropertyOrder(2)] public string name { get; set; }
        [JsonPropertyOrder(3)] public string state { get; set; }
        [JsonPropertyOrder(4)] public string mode { get; set; }

        public override string ToString()
        {
            return
                $" id = {id,-5}" +
                $",name = {name,-5}" +
                $",state = {state,-5}" +
                $",mode = {mode,-5}";
        }
    }
}