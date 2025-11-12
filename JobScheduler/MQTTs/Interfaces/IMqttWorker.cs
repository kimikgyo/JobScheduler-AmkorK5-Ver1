namespace JOB.MQTTs.Interfaces
{
    public interface IMqttWorker
    {
        Task PublishAsync(string topic, string payload);

        Task StartAsync(CancellationToken cancellationToken);

        void Dispose();
    }
}