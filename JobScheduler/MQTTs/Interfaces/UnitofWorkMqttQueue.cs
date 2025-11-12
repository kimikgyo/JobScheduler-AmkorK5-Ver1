using Common.Models;
using Common.Models.Queues;
using Data.Interfaces;
using Data.Repositorys.Bases;
using JOB.Mappings.Interfaces;
using log4net;

namespace JOB.MQTTs.Interfaces
{
    public class UnitofWorkMqttQueue : IUnitofWorkMqttQueue
    {
        private static readonly ILog MqttServiceLogger = LogManager.GetLogger("MQTT");

        private readonly MqttProcess _mqttProcess;
        private readonly IUnitOfWorkRepository _repository;
        private readonly IUnitOfWorkMapping _mapping;

        public UnitofWorkMqttQueue(IMqttWorker mqttWorker, IUnitOfWorkRepository repository, IUnitOfWorkMapping mapping)
        {
            _mqttProcess = new MqttProcess(this, mqttWorker, repository, mapping);
        }

        public void MqttPublishMessage(TopicType topicType, TopicSubType topicSubType, object value)
        {
            lock (this)
            {
                var getByPublish = ConfigData.PublishTopics.FirstOrDefault(t => t.type == $"{topicType}"
                                                                            && t.subType == $"{topicSubType}");
                if (getByPublish == null)
                {
                    MqttServiceLogger.Info($"{nameof(MqttPublishMessage)} = ConfigTopic Flie " +
                                           $" ,type = {topicType} ,subType = {topicSubType}");
                    return;
                }
                else
                {
                    string payload = value.ToJson();

                    switch (getByPublish.type)
                    {
                        case nameof(TopicType.order):
                            QueueStorage.MqttEnqueuePublishOrder(new MqttPublishMessageDto
                            {
                                Topic = getByPublish.topic,
                                Payload = payload,
                                Timestamp = DateTime.Now,
                            });
                            _mqttProcess.Order();
                            break;

                        case nameof(TopicType.job):
                            QueueStorage.MqttEnqueuePublishJob(new MqttPublishMessageDto
                            {
                                Topic = getByPublish.topic,
                                Payload = payload,
                                Timestamp = DateTime.Now,
                            });
                            _mqttProcess.Job();
                            break;

                        case nameof(TopicType.mission):
                            QueueStorage.MqttEnqueuePublishMission(new MqttPublishMessageDto
                            {
                                Topic = getByPublish.topic,
                                Payload = payload,
                                Timestamp = DateTime.Now,
                            });
                            _mqttProcess.Mission();
                            break;

                        case nameof(TopicType.position):
                            QueueStorage.MqttEnqueuePublishPosition(new MqttPublishMessageDto
                            {
                                Topic = getByPublish.topic,
                                Payload = payload,
                                Timestamp = DateTime.Now,
                            });
                            _mqttProcess.Position();
                            break;
                    }
                }
            }
        }

        public void MqttSubscribe(MqttSubscribeMessageDto subscribe)
        {
            switch (subscribe.type)
            {
                case nameof(TopicType.worker):
                    QueueStorage.MqttEnqueueSubscribeWorker(subscribe);
                    break;

                case nameof(TopicType.middleware):
                    QueueStorage.MqttEnqueueSubscribeMiddleware(subscribe);
                    break;
                case nameof(TopicType.carrier):
                    QueueStorage.MqttEnqueueSubscribeCarrier(subscribe);
                    break;  
            }
        }

        public void HandleReceivedMqttMessage()
        {
            _mqttProcess.HandleReceivedMqttMessage();
            _mqttProcess.Worker();
            _mqttProcess.Middleware();
            _mqttProcess.Carrier();
        }
    }
}