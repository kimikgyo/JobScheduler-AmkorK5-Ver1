using System.Text.Json.Serialization;

namespace Common.DTOs.Rests.Nodes_Edges
{
    public class Response_MetricDto
    {
        [JsonPropertyOrder(1)] public double totalCost { get; set; }
        [JsonPropertyOrder(2)] public double totalDistance { get; set; }
        [JsonPropertyOrder(3)] public int elevatorCount { get; set; }

        public override string ToString()
        {
            return
                $" totalCost = {totalCost,-5}" +
                $",totalDistance = {totalDistance,-5}" +
                $",elevatorCount = {elevatorCount,-5}";
        }
    }
}