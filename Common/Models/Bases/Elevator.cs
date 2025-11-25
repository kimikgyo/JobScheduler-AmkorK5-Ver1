using System.Text.Json.Serialization;

namespace Common.Models.Bases
{
    public class Elevator
    {
        [JsonPropertyOrder(1)] public string id { get; set; }
        [JsonPropertyOrder(2)] public string name { get; set; }
        [JsonPropertyOrder(3)] public string state { get; set; }
        [JsonPropertyOrder(4)] public string mode { get; set; }
        [JsonPropertyOrder(5)] public string modeChangeRequest { get; set; }
        [JsonPropertyOrder(6)] public DateTime createAt { get; set; }
        [JsonPropertyOrder(7)] public DateTime? updateAt { get; set; }

        public override string ToString()
        {
            return
                $" id = {id,-5}" +
                $",name = {name,-5}" +
                $",state = {state,-5}" +
                $",mode = {mode,-5}" +
                $",modeChangeRequest = {modeChangeRequest,-5}" +
                $",createAt = {createAt,-5}" +
                $",updateAt = {updateAt,-5}";
        }

    }
}
