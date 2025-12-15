using Data.Interfaces;
using JOB.Mappings.Interfaces;
using JOB.MQTTs.Interfaces;
using JobScheduler.Services;
using log4net;

namespace JOB.JobQueues.Process
{
    public partial class QueueProcess
    {
        private static readonly ILog EventLogger = LogManager.GetLogger("Event");

        private readonly IUnitOfWorkRepository _repository;
        private readonly IUnitofWorkMqttQueue _mqttQueue;
        private readonly IUnitOfWorkMapping _mapping;
        private MainService main = null;

        public QueueProcess(MainService mainService, IUnitOfWorkRepository repository, IUnitofWorkMqttQueue mqttQueue, IUnitOfWorkMapping mapping)
        {
            _repository = repository;
            _mqttQueue = mqttQueue;
            _mapping = mapping;
            main = mainService;
        }
    }
}