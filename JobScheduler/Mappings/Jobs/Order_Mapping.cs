using Common.DTOs.MQTTs.Orders;
using Common.DTOs.Rests.Orders;
using Common.Models.Jobs;

namespace JOB.Mappings.Jobs
{
    public class Order_Mapping
    {
        public Get_OrderDto Get(Order model)
        {
            var response = new Get_OrderDto()
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

        public Publish_OrderDto Publish(Order model)
        {
            var publish = new Publish_OrderDto()
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