using System.Text.Json.Serialization;

namespace Common.DTOs.MQTTs.Missions
{
    public class Subscribe_MissionDto
    {
        [JsonPropertyOrder(1)] public string jobId { get; set; }
        [JsonPropertyOrder(2)] public string acsMissionId { get; set; }
        [JsonPropertyOrder(3)] public string robotId { get; set; }
        [JsonPropertyOrder(4)] public string workerId { get; set; }
        [JsonPropertyOrder(5)] public string state { get; set; }
        [JsonPropertyOrder(6)] public DateTime ts { get; set; }
        [JsonPropertyOrder(8)] public DateTime vendorTs { get; set; }
        [JsonPropertyOrder(9)] public string _id { get; set; }
        [JsonPropertyOrder(10)] public Subscribe_MissionRowDataDto rowData { get; set; }

        public override string ToString()
        {
            return
                $" jobId = {jobId,-5}" +
                $",acsMissionId = {acsMissionId,-5}" +
                $",robotId = {robotId,-5}" +
                $",workerId = {workerId,-5}" +
                $",state = {state,-5}" +
                $",ts = {ts,-5}" +
                $",vendorTs = {vendorTs,-5}" +
                $",_id = {_id,-5}" +
                $",rowData = {rowData,-5}";
        }
    }
}