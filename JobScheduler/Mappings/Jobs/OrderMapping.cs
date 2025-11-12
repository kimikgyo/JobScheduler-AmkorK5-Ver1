using Common.DTOs.Jobs;
using Common.Models.Jobs;
using System.Data;

namespace JOB.Mappings.Jobs
{
    public class OrderMapping
    {
        public ResponseDtoOrder Response(Order model)
        {
            var response = new ResponseDtoOrder()
            {
                id = model.id,
                type = model.type,
                subType = model.subType,
                sourceId = model.sourceId,
                destinationId = model.destinationId,
                carrierId = model.carrierId,
                drumKeyCode = "",
                orderedBy = model.orderedBy,
                orderedAt = model.orderedAt,
                priority = model.priority,
                state = model.state,
                specifiedWorkerId = model.specifiedWorkerId,
                assignedWorkerId = model.assignedWorkerId,
                createdAt = model.createdAt,
                updatedAt = model.updatedAt,
                finishedAt = model.finishedAt,
            };

            return response;
        }

        public MqttPublishDtoOrder MqttPublish(Order model)
        {
            var publish = new MqttPublishDtoOrder()
            {
                id = model.id,
                type = model.type,
                subType = model.subType,
                sourceId = model.sourceId,
                destinationId = model.destinationId,
                carrierId = model.carrierId,
                drumKeyCode = "",
                orderedBy = model.orderedBy,
                orderedAt = model.orderedAt,
                priority = model.priority,
                state = model.state,
                stateCode = model.stateCode,
                specifiedWorkerId = model.specifiedWorkerId,
                assignedWorkerId = model.assignedWorkerId,
            };
            return publish;
        }
    }
}