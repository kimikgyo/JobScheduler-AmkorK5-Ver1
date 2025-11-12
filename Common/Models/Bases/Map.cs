using Common.Interfaces;
using Common.Models.Jobs;
using System.Reflection.Emit;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Common.Models.Bases
{
    public class Map
    {
        [JsonPropertyOrder(1)] public string id { get; set; }
        [JsonPropertyOrder(2)] public string mapId { get; set; }
        [JsonPropertyOrder(3)] public string source { get; set; }
        [JsonPropertyOrder(4)] public int    level { get; set; }
        [JsonPropertyOrder(5)] public string name { get; set; }

        public override string ToString()
        {
            return
                $"id = {id,-5}" +
                $",mapId = {mapId,-5}" +
                $",source = {source,-5}" +
                $",level = {level,-5}" +
                $",name = {name,-5}";

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