using System.Collections.Concurrent;

namespace Common.Models.Queues
{
    public static class QueueStorage
    {
        #region Order

        private static readonly ConcurrentQueue<AddOrder> AddQueueOrder = new ConcurrentQueue<AddOrder>();
        private static readonly ConcurrentQueue<RemoveOrder> RemoveQueueOrder = new ConcurrentQueue<RemoveOrder>();

        public static void AddEnqueueOrder(AddOrder item)
        {
            //미션 및 Queue 를 실행한부분을 순차적으로 추가시킨다
            AddQueueOrder.Enqueue(item);
        }

        public static bool AddTryDequeueOrder(out AddOrder item)
        {
            //실행하면 순차적으로 하나씩 Return한다
            return AddQueueOrder.TryDequeue(out item);
        }

        public static void RemoveEnqueueOrder(RemoveOrder item)
        {
            //미션 및 Queue 를 실행한부분을 순차적으로 추가시킨다
            RemoveQueueOrder.Enqueue(item);
        }

        public static bool RemoveTryDequeueOrder(out RemoveOrder item)
        {
            //실행하면 순차적으로 하나씩 Return한다
            return RemoveQueueOrder.TryDequeue(out item);
        }

        #endregion Order

        #region Job

        private static readonly ConcurrentQueue<AddJobMission> AddQueueJobMission = new ConcurrentQueue<AddJobMission>();
        private static readonly ConcurrentQueue<AddMission> AddQueueMission = new ConcurrentQueue<AddMission>();
        private static readonly ConcurrentQueue<RemoveJobMission> RemoveQueueJobMission = new ConcurrentQueue<RemoveJobMission>();

        public static void AddEnqueueJobMission(AddJobMission item)
        {
            //미션 및 Queue 를 실행한부분을 순차적으로 추가시킨다
            AddQueueJobMission.Enqueue(item);
        }

        public static bool AddTryDequeueJobMission(out AddJobMission item)
        {
            //실행하면 순차적으로 하나씩 Return한다
            return AddQueueJobMission.TryDequeue(out item);
        }

        public static void RemoveEnqueueJobMission(RemoveJobMission item)
        {
            //미션 및 Queue 를 실행한부분을 순차적으로 추가시킨다
            RemoveQueueJobMission.Enqueue(item);
        }

        public static bool RemoveTryDequeueJobMission(out RemoveJobMission item)
        {
            //실행하면 순차적으로 하나씩 Return한다
            return RemoveQueueJobMission.TryDequeue(out item);
        }

        public static void AddEnqueueMission(AddMission item)
        {
            //미션 및 Queue 를 실행한부분을 순차적으로 추가시킨다
            AddQueueMission.Enqueue(item);
        }

        public static bool AddTryDequeueMission(out AddMission item)
        {
            //실행하면 순차적으로 하나씩 Return한다
            return AddQueueMission.TryDequeue(out item);
        }

        #endregion Job

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