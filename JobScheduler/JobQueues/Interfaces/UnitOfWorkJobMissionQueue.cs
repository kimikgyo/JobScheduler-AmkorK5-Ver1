using Common.DTOs.Jobs;
using Common.Models.Jobs;
using Common.Models.Queues;
using Common.Templates;
using Data.Interfaces;
using JOB.Mappings.Interfaces;
using JOB.MQTTs.Interfaces;

namespace JOB.JobQueues.Interfaces
{
    public class UnitOfWorkJobMissionQueue : IUnitOfWorkJobMissionQueue
    {
        private readonly QueueProcess _queueProcess;

        public UnitOfWorkJobMissionQueue(IUnitOfWorkRepository repository, IUnitofWorkMqttQueue mqttQueue, IUnitOfWorkMapping mapping)
        {
            _queueProcess = new QueueProcess(repository, mqttQueue, mapping);
        }

        public void CreateOrder(AddRequestDtoOrder orderAddRequest)
        {
            QueueStorage.AddEnqueueOrder(new AddOrder
            {
                AddRequestOrder = orderAddRequest
            });
            _queueProcess.AddOrder();
        }

        public void RemoveOrder(Order target, DateTime? finishedAt)
        {
            QueueStorage.RemoveEnqueueOrder(new RemoveOrder
            {
                orderTarget = target,
                finishedAt = finishedAt,
            });
            _queueProcess.RemoveOrder();
        }

                        
        public void CreateJobMission(JobTemplate jobTemplate,string orderId, string carrierId, int priority,string drumKeyCode
                                    , string sourceId, string sourceName,string sourcelinkedFacility
                                    , string destinationId, string destinationName , string destinationlinkedFacility
                                    , string specifiedWorkerId,string assignedWorkerId)
        {
            QueueStorage.AddEnqueueJobMission(new AddJobMission
            {
                orderId = orderId,
                carrierId = carrierId,
                drumKeyCode = drumKeyCode,
                groupId = jobTemplate.group,
                sourceId = sourceId,
                destinationId = destinationId,
                priority = priority,
                specifiedWorkerId = specifiedWorkerId,
                assignedWorkerId = assignedWorkerId,
                jobTemplate = jobTemplate,
                sourceName = sourceName,
                destinationName = destinationName,
                sourcelinkedFacility = sourcelinkedFacility,
                destinationlinkedFacility = destinationlinkedFacility,
            });

            _queueProcess.AddJob_Mission();
        }

        public void RemoveJobMission(Job job, DateTime? finishedAt)
        {
            QueueStorage.RemoveEnqueueJobMission(new RemoveJobMission
            {
                job = job,
                finishedAt = finishedAt
            });
            _queueProcess.RemoveJob_Mission();
        }

        public void ProcessAllOrder()
        {
            _queueProcess.AddOrder();
            _queueProcess.RemoveOrder();
        }

        public void ProcessAllJob()
        {
            _queueProcess.AddJob_Mission();
        }

        public void SaveChanges()
        {
        }

        public void Dispose()
        {
        }
    }
}