using Common.Templates;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;

namespace Common.DTOs.Bases
{
    public class ApiGetResponseDtoResourceMap
    {
        [JsonPropertyOrder(1)]public string _id { get; set; }
        [JsonPropertyOrder(2)]public string mapId { get; set; }
        [JsonPropertyOrder(3)]public string source { get; set; }
        [JsonPropertyOrder(4)]public int __v { get; set; }
        [JsonPropertyOrder(5)]public DateTime createdAt { get; set; }
        [JsonPropertyOrder(6)]public string imageId { get; set; }
        [JsonPropertyOrder(7)]public int level { get; set; }
        [JsonPropertyOrder(8)]public string name { get; set; }
        [JsonPropertyOrder(9)]public double originTheta { get; set; }
        [JsonPropertyOrder(10)]public double originX { get; set; }
        [JsonPropertyOrder(11)]public double originY { get; set; }
        [JsonPropertyOrder(12)]public double resolution { get; set; }
        [JsonPropertyOrder(13)] public DateTime updatedAt { get; set; }

        public override string ToString()
        {
            return
                $"_id = {_id,-5}" +
                $",mapId = {mapId,-5}" +
                $",source = {source,-5}" +
                $",__v = {__v,-5}" +
                $",createdAt = {createdAt,-5}" +
                $",imageId = {imageId,-5}" +
                $",level = {level,-5}" +
                $",name = {name,-5}" +
                $",originTheta = {originTheta,-5}" +
                $",originX = {originX,-5}" +
                $",originY = {originY,-5}" +
                $",resolution = {resolution,-5}" +
                $",updatedAt = {updatedAt,-5}";
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
