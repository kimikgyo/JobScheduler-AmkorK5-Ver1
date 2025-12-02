using System.Text.Json.Serialization;

namespace Common.DTOs.Rests.Nodes_Edges
{
    public class Response_NodeDto
    {
        [JsonPropertyOrder(1)] public string positionId { get; set; }
        [JsonPropertyOrder(2)] public string name { get; set; }
        [JsonPropertyOrder(3)] public string mapId { get; set; }
        [JsonPropertyOrder(4)] public double x { get; set; }
        [JsonPropertyOrder(5)] public double y { get; set; }
        [JsonPropertyOrder(6)] public string nodeType { get; set; }
        [JsonPropertyOrder(7)] public int level { get; set; }

        public override string ToString()
        {
            return
                $" positionId = {positionId,-5}" +
                $",name = {name,-5}" +
                $",mapId = {mapId,-5}" +
                $",x = {x,-5}" +
                $",y = {y,-5}" +
                $",nodeType = {nodeType,-5}" +
                $",level = {level,-5}";
        }
    }
}