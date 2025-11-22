using System.Text.Json.Serialization;

namespace Common.DTOs.MQTTs.Workers
{
    public class Subscribe_WorkerStatusDto
    {
        [JsonPropertyOrder(1)] public string robotId { get; set; }
        [JsonPropertyOrder(2)] public string vendor { get; set; }
        [JsonPropertyOrder(3)] public string vendorId { get; set; }
        [JsonPropertyOrder(4)] public string state { get; set; }
        [JsonPropertyOrder(5)] public string mode { get; set; }
        [JsonPropertyOrder(6)] public string severity { get; set; }
        [JsonPropertyOrder(9)] public string ts { get; set; }
        [JsonPropertyOrder(10)] public string vendorTs { get; set; }
        [JsonPropertyOrder(11)] public Subscribe_WorkerBatteryDto battery { get; set; }
        [JsonPropertyOrder(12)] public Subscribe_WorkerMissionDto mission { get; set; }
        [JsonPropertyOrder(13)] public Subscribe_WorkerPoseDto pose { get; set; }
        [JsonPropertyOrder(14)] public Subscribe_WorkerVelocityDto velocity { get; set; }
        [JsonPropertyOrder(15)] public Subscribe_WorkerPayloadDto payload { get; set; }
        [JsonPropertyOrder(16)] public Subscribe_WorkerConnectivityDto connectivity { get; set; }

        //[JsonPropertyOrder(17)] public Subscribe_WorkerHealthDto health { get; set; }
        [JsonPropertyOrder(17)] public Subscribe_WorkerApplicationDto application { get; set; }

        public override string ToString()
        {
            return

                $" robotId = {robotId,-5}" +
                $",vendor = {vendor,-5}" +
                $",vendorId = {vendorId,-5}" +
                $",state = {state,-5}" +
                $",mode = {mode,-5}" +
                $",severity = {severity,-5}" +
                $",ts = {ts,-5}" +
                $",vendorTs = {vendorTs,-5}" +
                $",battery = {battery,-5}" +
                $",mission = {mission,-5}" +
                $",pose = {pose,-5}" +
                $",velocity = {velocity,-5}" +
                $",payload = {payload,-5}" +
                $",connectivity = {connectivity,-5}" +
                //$",health = {health,-5}" +
                $",application = {application,-5}";
        }
    }
}