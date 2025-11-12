using Common.DTOs.Jobs;
using Common.Models.Jobs;

namespace JOB.Mappings.Jobs
{
    public class JobMapping
    {
        public ResponseDtoJob Response(Job model)
        {
            var response = new ResponseDtoJob()
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

        public MqttPublishDtoJob MqttPublish(Job model)
        {
            var publish = new MqttPublishDtoJob()
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
                terminationType = model.terminationType,
                terminateState = model.terminateState,
                terminator = model.terminator
            };
            return publish;
        }
    }
}