using System.Text.Json.Serialization;

namespace Common.DTOs.Bases
{
    public class ApiGetResponseDtoResourceWorker
    {
        [JsonPropertyOrder(1)] public string _id { get; set; }
        [JsonPropertyOrder(2)] public string source { get; set; }
        [JsonPropertyOrder(3)] public string workerId { get; set; }
        [JsonPropertyOrder(4)] public int __v { get; set; }
        [JsonPropertyOrder(5)] public List<Capabilities> capabilities { get; set; }
        [JsonPropertyOrder(6)] public string createdAt { get; set; }
        [JsonPropertyOrder(7)] public string createdBy { get; set; }
        [JsonPropertyOrder(8)] public string ipAddress { get; set; }
        [JsonPropertyOrder(12)] public string loginId { get; set; }
        [JsonPropertyOrder(13)] public string name { get; set; }
        [JsonPropertyOrder(14)] public string password { get; set; }
        [JsonPropertyOrder(15)] public ApiGetResponseDtoResourceMiddleware Middleware { get; set; }

        public override string ToString()
        {
            return
                $"_id = {_id,-5}" +
                $",source = {source,-5}" +
                $",workerId = {workerId,-5}" +
                $",__v = {__v,-5}" +
                $",capabilities = {capabilities,-5}" +
                $",createdAt = {createdAt,-5}" +
                $",createdBy = {createdBy,-5}" +
                $",ipAddress = {ipAddress,-5}" +
                $",loginId = {loginId,-5}" +
                $",name = {name,-5}" +
                $",password = {password,-5}" +
                $",Middleware = {Middleware,-5}";
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

    public class Capabilities
    {
    }

    public class MqttSubscribeDtoWorkerStatus
    {
        [JsonPropertyOrder(1)] public string robotId { get; set; }
        [JsonPropertyOrder(2)] public string vendor { get; set; }
        [JsonPropertyOrder(3)] public string vendorId { get; set; }
        [JsonPropertyOrder(4)] public string state { get; set; }
        [JsonPropertyOrder(5)] public string mode { get; set; }
        [JsonPropertyOrder(6)] public string severity { get; set; }
        [JsonPropertyOrder(9)] public DateTime ts { get; set; }
        [JsonPropertyOrder(10)] public DateTime vendorTs { get; set; }
        [JsonPropertyOrder(11)] public MqttSubscribeDtoWorkerBattery battery { get; set; }
        [JsonPropertyOrder(12)] public MqttSubscribeDtoWorkerMission mission { get; set; }
        [JsonPropertyOrder(13)] public MqttSubscribeDtoWorkerPose pose { get; set; }
        [JsonPropertyOrder(14)] public MqttSubscribeDtoWorkerVelocity velocity { get; set; }
        [JsonPropertyOrder(15)] public MqttSubscribeDtoWorkerPayload payload { get; set; }
        [JsonPropertyOrder(16)] public MqttSubscribeDtoWorkerConnectivity connectivity { get; set; }
        //[JsonPropertyOrder(17)] public MqttSubscribeDtoWorkerHealth health { get; set; }
        [JsonPropertyOrder(17)] public MqttSubscribeDtoWorkerApplication application { get; set; }

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

    public class MqttSubscribeDtoWorkerBattery
    {
        public int? percent { get; set; }
        public bool isCharging { get; set; }

        public override string ToString()
        {
            return
                $"percent = {percent,-5}" +
                $",isCharging = {isCharging,-5}";
        }
    }

    public class MqttSubscribeDtoWorkerMission
    {
        public string missionId { get; set; }
        public string missionText { get; set; }
        public string status { get; set; }

        public override string ToString()
        {
            return
               $"missionId = {missionId,-5}" +
               $",missionText = {missionText,-5}" +
               $",status = {status,-5}";
        }
    }

    public class MqttSubscribeDtoWorkerPose
    {
        public double? x { get; set; }
        public double? y { get; set; }
        public double? theta { get; set; }
        public string mapId { get; set; }

        public override string ToString()
        {
            return
                $"x = {x,-5}" +
                $",y = {y,-5}" +
                $",theta = {theta,-5}" +
                $",mapId = {mapId,-5}";
        }
    }

    public class MqttSubscribeDtoWorkerVelocity
    {
        public double? linear { get; set; }
        public double? angular { get; set; }

        public override string ToString()
        {
            return

                $"linear = {linear,-5}" +
                $",angular = {angular,-5}";
        }
    }

    public class MqttSubscribeDtoWorkerPayload
    {
        public bool isLoaded { get; set; }
        public double? weightKg { get; set; }

        public override string ToString()
        {
            return

                $"isLoaded = {isLoaded,-5}" +
                $",weightKg = {weightKg,-5}";
        }
    }

    public class MqttSubscribeDtoWorkerConnectivity
    {
        public bool online { get; set; }
        public int? rssi { get; set; }

        public override string ToString()
        {
            return
               $"online = {online,-5}" +
               $",rssi = {rssi,-5}";
        }
    }

    public class MqttSubscribeDtoWorkerHealth
    {
        public List<string> alarms { get; set; }

        public override string ToString()
        {
            return
                $"alarms = {alarms,-5}";
        }
    }

    public class MqttSubscribeDtoWorkerApplication
    {
        public bool isActive { get; set; }

        public override string ToString()
        {
            return
                $"isActive = {isActive,-5}";
        }
    }
}