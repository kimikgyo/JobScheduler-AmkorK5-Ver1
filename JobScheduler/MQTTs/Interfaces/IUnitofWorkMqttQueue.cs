using Common.Models;

namespace JOB.MQTTs.Interfaces
{
    public interface IUnitofWorkMqttQueue
    {
        void MqttPublishMessage(TopicType topicType, TopicSubType topicSubType, object value);

        void HandleReceivedMqttMessage();
    }
}