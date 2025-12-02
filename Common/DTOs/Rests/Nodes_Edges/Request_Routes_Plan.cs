using System.Text.Json.Serialization;
using System.Xml.Linq;

namespace Common.DTOs.Rests.Nodes_Edges
{
    public class Request_Routes_Plan
    {
        [JsonPropertyOrder(1)] public string startPositionId { get; set; }
        [JsonPropertyOrder(2)] public string targetPositionId { get; set; }

        public override string ToString()
        {
            return
                $" startPositionId = {startPositionId,-5}" +
                $",targetPositionId = {targetPositionId,-5}";
        }
    }
}