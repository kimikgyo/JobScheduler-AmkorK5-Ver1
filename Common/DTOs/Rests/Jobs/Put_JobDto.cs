using System.Text.Json.Serialization;

namespace Common.DTOs.Rests.Jobs
{
    public class Put_JobDto
    {
        [JsonPropertyOrder(1)] public string id { get; set; }
        [JsonPropertyOrder(2)] public string terminationType { get; set; }
        [JsonPropertyOrder(3)] public string terminator { get; set; }
        [JsonPropertyOrder(4)] public DateTime terminatingAt { get; set; }

        public override string ToString()
        {
            return
                $" id = {id,-5}" +
                $",terminationType = {terminationType,-5}" +
                $",terminator = {terminator,-5}" +
                $",terminatingAt = {terminatingAt,-5}";
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
