using System.Text.Json.Serialization;

namespace Common.Models.Bases
{
    public class PostReport
    {
        [JsonPropertyOrder(1)] public int ceid { get; set; }
        [JsonPropertyOrder(2)] public string eventName { get; set; }
        [JsonPropertyOrder(3)] public int rptid { get; set; }

        public override string ToString()
        {
            return
                $"ceid = {ceid,-5}" +
                $",eventName = {eventName,-5}" +
                $",rptid = {rptid,-5}";
        }
    }

}
