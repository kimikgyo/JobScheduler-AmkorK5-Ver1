using Common.Models.Bases;
using Microsoft.Extensions.Hosting;
using System.Text.Json.Serialization;

namespace Common.DTOs.Rests.Nodes_Edges
{
    public class Response_Node_EdgeDto
    {
        [JsonPropertyOrder(1)] public List<Response_NodeDto> nodes { get; set; }
        [JsonPropertyOrder(2)] public List<Response_EdgeDto> edges { get; set; }
        [JsonPropertyOrder(3)] public Response_MetricDto metrics { get; set; }

        public override string ToString()
        {
            return
                $" nodes = {nodes,-5}" +
                $",edges = {edges,-5}" +
                $",metrics = {metrics,-5}";
        }
    }
}