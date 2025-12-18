using Common.DTOs.MQTTs.Jobs;
using Common.DTOs.Rests.Jobs;
using Common.Models.Jobs;

namespace JOB.Mappings.Jobs
{
    public class Job_Mapping
    {
        public Get_JobDto Get(Job model)
        {
            var response = new Get_JobDto()
            {
                guid = model.guid,
                group = model.group,
                name = model.name,
                orderId = model.orderId,
                type = model.type,
                subType = model.subType,
                priority = model.priority,
                sequence = model.sequence,
                carrierId = model.carrierId,
                drumKeyCode = model.drumKeyCode,
                sourceId = model.sourceId,
                sourceName = model.sourceName,
                sourcelinkedFacility = model.sourcelinkedFacility,
                destinationId = model.destinationId,
                destinationName = model.destinationName,
                destinationlinkedFacility = model.destinationlinkedFacility,
                isLocked = model.isLocked,
                state = model.state,
                specifiedWorkerId = model.specifiedWorkerId,
                assignedWorkerId = model.assignedWorkerId,
                assignedWorkerName = model.assignedWorkerName,
                createdAt = model.createdAt,
                updatedAt = model.updatedAt,
                finishedAt = model.finishedAt,
                terminationType = model.terminationType,
                terminateState = model.terminateState,
                terminator = model.terminator,
                terminatingAt = model.terminatingAt,
                terminatedAt = model.terminatedAt
            };

            return response;
        }

        public Publish_JobDto Publish(Job model)
        {
            var publish = new Publish_JobDto()
            {

                guid = model.guid,
                group = model.group,
                name = model.name,
                orderId = model.orderId,
                type = model.type,
                subType = model.subType,
                priority = model.priority,
                sequence = model.sequence,
                carrierId = model.carrierId,
                drumKeyCode = model.drumKeyCode,
                sourceId = model.sourceId,
                sourceName = model.sourceName,
                sourcelinkedFacility = model.sourcelinkedFacility,
                destinationId = model.destinationId,
                destinationName = model.destinationName,
                destinationlinkedFacility = model.destinationlinkedFacility,
                isLocked = model.isLocked,
                state = model.state,
                specifiedWorkerId = model.specifiedWorkerId,
                assignedWorkerId = model.assignedWorkerId,
                assignedWorkerName = model.assignedWorkerName,
                createdAt = model.createdAt,
                updatedAt = model.updatedAt,
                finishedAt = model.finishedAt,
                terminationType = model.terminationType,
                terminateState = model.terminateState,
                terminator = model.terminator,
                terminatingAt = model.terminatingAt,
                terminatedAt = model.terminatedAt
            };
            return publish;
        }
    }
}