using System.Text.Json.Serialization;

namespace Common.DTOs.Rests.Missions
{
    public class Patch_MissionDto
    {
        [JsonPropertyOrder(0)] public string orderId { get; set; }
        [JsonPropertyOrder(1)] public string destinationId { get; set; }

        public override string ToString()
        {
            return
            $"orderId = {orderId,-5}" +
            $"destinationId = {destinationId,-5}";
        }
    }
}
