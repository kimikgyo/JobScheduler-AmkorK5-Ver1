using Common.Models;
using Common.Models.Queues;
using System.Text.Json;

namespace JOB.MQTTs
{
    public partial class MqttProcess
    {
        public void Order()
        {
            while (QueueStorage.MqttTryDequeuePublishOrder(out MqttPublishMessageDto cmd))
            {
                try
                {
                    //Console.WriteLine(string.Format("Process Message: [{0}] {1} at {2:yyyy-MM-dd HH:mm:ss,fff}", cmd.Topic, cmd.Payload, cmd.Timestamp));

                    _mqttWorker.PublishAsync(cmd.Topic, cmd.Payload).Wait();
                }
                catch (Exception ex)
                {
                    LogExceptionMessage(ex);
                }
            }
        }
    }
}
