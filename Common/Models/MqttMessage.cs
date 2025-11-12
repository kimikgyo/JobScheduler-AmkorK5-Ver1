namespace Common.Models
{
    public enum TopicType
    {
        worker,
        middleware,
        order,
        job,
        mission,
        position,
        carrier
    }

    public enum TopicSubType
    {
        isOccupied,
        state,
        pose,
        mission,
        status
    }

    public class MQTTSetting
    {
        public string id { get; set; }
        public string host { get; set; }
        public string prot { get; set; }
    }

    public class MqttTopicSubscribe
    {
        public string topic { get; set; }
    }

    public class MqttTopicPublish
    {
        public string type { get; set; }
        public string subType { get; set; }
        public string topic { get; set; }
    }

    public class MqttPublishMessageDto
    {
        public string Topic { get; set; }
        public string Payload { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class MqttSubscribeMessageDto
    {
        public string id { get; set; }
        public string type { get; set; }
        public string subType { get; set; }
        public string topic { get; set; }
        public string Payload { get; set; }
        public DateTime Timestamp { get; set; }
    }

    //사용안함
    //public class MqttMessage
    //{
    //    public string Topic { get; set; }
    //    public string Payload { get; set; }
    //    public DateTime Timestamp { get; set; }
    //}
}