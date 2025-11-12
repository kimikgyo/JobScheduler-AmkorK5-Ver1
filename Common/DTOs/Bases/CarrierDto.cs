namespace Common.DTOs.Bases
{
    public class ApiGetResponseDtoResourceCarrier
    {
        public string carrierId { get; set; }
        public string name { get; set; }
        public string location { get; set; }
        public string installedTime { get; set; }
        public string workerId { get; set; }

        public override string ToString()
        {
            return
                $"carrierId = {carrierId,-5}" +
                $"name = {name,-5}" +
                $"location = {location,-5}" +
                $"installedTime = {installedTime,-5}" +
                $"workerId = {workerId,-5}";
        }
    }

    public class MqttSubscribeDtoCarrier
    {
        public string carrierId { get; set; }
        public string name { get; set; }
        public string location { get; set; }
        public string installedTime { get; set; }
        public string workerId { get; set; }

        public override string ToString()
        {
            return
                $"carrierId = {carrierId,-5}" +
                $"name = {name,-5}" +
                $"location = {location,-5}" +
                $"installedTime = {installedTime,-5}" +
                $"workerId = {workerId,-5}";
        }
    }
}