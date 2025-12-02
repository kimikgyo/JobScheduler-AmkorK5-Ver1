using System.Text.Json.Serialization;

namespace Common.DTOs.Rests.Nodes_Edges
{
    public class Response_EdgeDto
    {
        [JsonPropertyOrder(1)] public string linkId { get; set; }
        [JsonPropertyOrder(2)] public string from { get; set; }
        [JsonPropertyOrder(3)] public string to { get; set; }
        [JsonPropertyOrder(4)] public double cost { get; set; }
        [JsonPropertyOrder(5)] public double distance { get; set; }
        [JsonPropertyOrder(6)] public bool isElevator { get; set; }

        public override string ToString()
        {
            return
                $" linkId = {linkId,-5}" +
                $",from = {from,-5}" +
                $",to = {to,-5}" +
                $",cost = {cost,-5}" +
                $",distance = {distance,-5}" +
                $",isElevator = {isElevator,-5}";
        }
    }
}