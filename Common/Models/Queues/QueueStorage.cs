using System.Collections.Concurrent;

namespace Common.Models.Queues
{
    public static class QueueStorage
    {
        #region Order,Job,Mission

        private static readonly ConcurrentQueue<Create_Order> Add_Order = new ConcurrentQueue<Create_Order>();
        private static readonly ConcurrentQueue<Remove_Order> Remove_Order = new ConcurrentQueue<Remove_Order>();
        private static readonly ConcurrentQueue<Create_Job> Add_Job = new ConcurrentQueue<Create_Job>();
        private static readonly ConcurrentQueue<Remove_Job> Remove_Job = new ConcurrentQueue<Remove_Job>();
        private static readonly ConcurrentQueue<Create_Mission> Add_Mission = new ConcurrentQueue<Create_Mission>();

        public static void Create_Order_Enqueue(Create_Order item)
        {
            //미션 및 Queue 를 실행한부분을 순차적으로 추가시킨다
            Add_Order.Enqueue(item);
        }

        public static bool AddTryDequeueOrder(out Create_Order item)
        {
            //실행하면 순차적으로 하나씩 Return한다
            return Add_Order.TryDequeue(out item);
        }

        public static void Remove_Order_Enqueue(Remove_Order item)
        {
            //미션 및 Queue 를 실행한부분을 순차적으로 추가시킨다
            Remove_Order.Enqueue(item);
        }

        public static bool RemoveTryDequeueOrder(out Remove_Order item)
        {
            //실행하면 순차적으로 하나씩 Return한다
            return Remove_Order.TryDequeue(out item);
        }

        public static void Create_Job_Enqueue(Create_Job item)
        {
            //미션 및 Queue 를 실행한부분을 순차적으로 추가시킨다
            Add_Job.Enqueue(item);
        }

        public static bool Add_Job_TryDequeue(out Create_Job item)
        {
            //실행하면 순차적으로 하나씩 Return한다
            return Add_Job.TryDequeue(out item);
        }

        public static void Remove_Job_Enqueue(Remove_Job item)
        {
            //미션 및 Queue 를 실행한부분을 순차적으로 추가시킨다
            Remove_Job.Enqueue(item);
        }

        public static bool Remove_Job_TryDequeue(out Remove_Job item)
        {
            //실행하면 순차적으로 하나씩 Return한다
            return Remove_Job.TryDequeue(out item);
        }

        public static void Create_Mission_Enqueue(Create_Mission item)
        {
            //미션 및 Queue 를 실행한부분을 순차적으로 추가시킨다
            Add_Mission.Enqueue(item);
        }

        public static bool Add_Mission_TryDequeue(out Create_Mission item)
        {
            //실행하면 순차적으로 하나씩 Return한다
            return Add_Mission.TryDequeue(out item);
        }

        #endregion Order,Job,Mission

        #region MQTT

        private static readonly ConcurrentQueue<MqttPublishMessageDto> publishOrder = new ConcurrentQueue<MqttPublishMessageDto>();
        private static readonly ConcurrentQueue<MqttPublishMessageDto> publishJob = new ConcurrentQueue<MqttPublishMessageDto>();
        private static readonly ConcurrentQueue<MqttPublishMessageDto> publishMission = new ConcurrentQueue<MqttPublishMessageDto>();
        private static readonly ConcurrentQueue<MqttPublishMessageDto> publishPosition = new ConcurrentQueue<MqttPublishMessageDto>();

        private static readonly ConcurrentQueue<MqttSubscribeMessageDto> mqttMessagesSubscribe = new ConcurrentQueue<MqttSubscribeMessageDto>();
        private static readonly ConcurrentQueue<MqttSubscribeMessageDto> mqttSubscribeWorker = new ConcurrentQueue<MqttSubscribeMessageDto>();
        private static readonly ConcurrentQueue<MqttSubscribeMessageDto> mqttSubscribeMiddleware = new ConcurrentQueue<MqttSubscribeMessageDto>();
        private static readonly ConcurrentQueue<MqttSubscribeMessageDto> mqttSubscribeCarrier = new ConcurrentQueue<MqttSubscribeMessageDto>();
        private static readonly ConcurrentQueue<MqttSubscribeMessageDto> mqttSubscribeElevator = new ConcurrentQueue<MqttSubscribeMessageDto>();
        private static readonly ConcurrentQueue<MqttSubscribeMessageDto> mqttSubscribeTraffic = new ConcurrentQueue<MqttSubscribeMessageDto>();

        public static void MqttEnqueuePublishOrder(MqttPublishMessageDto item)
        {
            //미션 및 Queue 를 실행한부분을 순차적으로 추가시킨다
            publishOrder.Enqueue(item);
        }

        public static bool MqttTryDequeuePublishOrder(out MqttPublishMessageDto item)
        {
            //실행하면 순차적으로 하나씩 Return한다
            return publishOrder.TryDequeue(out item);
        }

        public static void MqttEnqueuePublishJob(MqttPublishMessageDto item)
        {
            //미션 및 Queue 를 실행한부분을 순차적으로 추가시킨다
            publishJob.Enqueue(item);
        }

        public static bool MqttTryDequeuePublishJob(out MqttPublishMessageDto item)
        {
            //실행하면 순차적으로 하나씩 Return한다
            return publishJob.TryDequeue(out item);
        }

        public static void MqttEnqueuePublishMission(MqttPublishMessageDto item)
        {
            //미션 및 Queue 를 실행한부분을 순차적으로 추가시킨다
            publishMission.Enqueue(item);
        }

        public static bool MqttTryDequeuePublishMission(out MqttPublishMessageDto item)
        {
            //실행하면 순차적으로 하나씩 Return한다
            return publishMission.TryDequeue(out item);
        }

        public static void MqttEnqueuePublishPosition(MqttPublishMessageDto item)
        {
            //미션 및 Queue 를 실행한부분을 순차적으로 추가시킨다
            publishPosition.Enqueue(item);
        }

        public static bool MqttTryDequeuePublishPosition(out MqttPublishMessageDto item)
        {
            //실행하면 순차적으로 하나씩 Return한다
            return publishPosition.TryDequeue(out item);
        }

        public static void MqttEnqueueSubscribeWorker(MqttSubscribeMessageDto item)
        {
            //미션 및 Queue 를 실행한부분을 순차적으로 추가시킨다
            mqttSubscribeWorker.Enqueue(item);
        }

        public static bool MqttTryDequeueSubscribeWorker(out MqttSubscribeMessageDto item)
        {
            //실행하면 순차적으로 하나씩 Return한다
            return mqttSubscribeWorker.TryDequeue(out item);
        }

        public static void MqttEnqueueSubscribeMiddleware(MqttSubscribeMessageDto item)
        {
            //미션 및 Queue 를 실행한부분을 순차적으로 추가시킨다
            mqttSubscribeMiddleware.Enqueue(item);
        }

        public static bool MqttTryDequeueSubscribeMiddleware(out MqttSubscribeMessageDto item)
        {
            //실행하면 순차적으로 하나씩 Return한다
            return mqttSubscribeMiddleware.TryDequeue(out item);
        }

        public static void MqttEnqueueSubscribeCarrier(MqttSubscribeMessageDto item)
        {
            //미션 및 Queue 를 실행한부분을 순차적으로 추가시킨다
            mqttSubscribeCarrier.Enqueue(item);
        }

        public static bool MqttTryDequeueSubscribeCarrier(out MqttSubscribeMessageDto item)
        {
            //실행하면 순차적으로 하나씩 Return한다
            return mqttSubscribeCarrier.TryDequeue(out item);
        }

        public static void MqttEnqueueSubscribeElevator(MqttSubscribeMessageDto item)
        {
            //미션 및 Queue 를 실행한부분을 순차적으로 추가시킨다
            mqttSubscribeElevator.Enqueue(item);
        }

        public static bool MqttTryDequeueSubscribeElevator(out MqttSubscribeMessageDto item)
        {
            //실행하면 순차적으로 하나씩 Return한다
            return mqttSubscribeElevator.TryDequeue(out item);
        }

        public static void MqttEnqueueSubscribeTraffic(MqttSubscribeMessageDto item)
        {
            //미션 및 Queue 를 실행한부분을 순차적으로 추가시킨다
            mqttSubscribeTraffic.Enqueue(item);
        }

        public static bool MqttTryDequeueSubscribeTraffic(out MqttSubscribeMessageDto item)
        {
            //실행하면 순차적으로 하나씩 Return한다
            return mqttSubscribeTraffic.TryDequeue(out item);
        }

        public static void MqttEnqueueSubscribe(MqttSubscribeMessageDto item)
        {
            //미션 및 Queue 를 실행한부분을 순차적으로 추가시킨다
            mqttMessagesSubscribe.Enqueue(item);
        }

        public static bool MqttTryDequeueSubscribe(out MqttSubscribeMessageDto item)
        {
            //실행하면 순차적으로 하나씩 Return한다
            return mqttMessagesSubscribe.TryDequeue(out item);
        }

        #endregion MQTT
    }
}