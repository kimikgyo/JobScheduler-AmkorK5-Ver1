using Common.Models;
using Common.Models.Queues;

namespace JOB.MQTTs
{
    public partial class MqttProcess
    {
        public void Subscribe_Carrier()
        {
            while (QueueStorage.MqttTryDequeueSubscribeWorker(out MqttSubscribeMessageDto subscribe))
            {
                try
                {
                    //생성
                    //업데이트
                    //ReMove
                }
                catch (Exception ex)
                {
                    LogExceptionMessage(ex);
                }
            }
        }
    }
}