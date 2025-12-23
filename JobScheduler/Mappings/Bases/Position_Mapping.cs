using Common.DTOs.MQTTs.Positions;
using Common.DTOs.Rests.Positions;
using Common.Models.Jobs;

namespace JOB.Mappings.Bases
{
    public class Position_Mapping
    {
        public Position Response(Response_PositionDto model)
        {
            var response = new Position
            {
                id = model._id,
                positionId = model.positionId,
                source = model.source,
                group = model.groupId,
                type = model.type.Replace(" ", "").ToUpper(),
                subType = model.subType.Replace(" ", "").ToUpper(),
                nodeType = model.nodeType.Replace(" ", "").ToUpper(),
                mapId = model.mapId,
                name = model.name,
                x = model.x,
                y = model.y,
                theth = model.theta,
                isDisplayed = model.isDisplayed,
                isEnabled = model.isEnabled,
                isOccupied = model.isOccupied,
                linkedArea = model.linkedArea,
                linkedZone = model.linkedZone,
                linkedFacility = model.linkedFacility,
                linkedRobotId = model.linkedRobotId,
                hasCharger = model.hasCharger,
            };
            return response;
        }

        public Publish_PositionDto Publish(Position model)
        {
            var publish = new Publish_PositionDto()
            {
                id = model.id,
                source = model.source,
                group = model.group,
                type = model.type.Replace(" ", "").ToUpper(),
                subType = model.subType.Replace(" ", "").ToUpper(),
                mapId = model.mapId,
                name = model.name,
                x = model.x,
                y = model.y,
                theth = model.theth,
                isDisplayed = model.isDisplayed,
                isEnabled = model.isEnabled,
                isOccupied = model.isOccupied,
                linkedArea = model.linkedArea,
                linkedZone = model.linkedZone,
                linkedFacility = model.linkedFacility,
                linkedRobotId = model.linkedRobotId,
                hasCharger = model.hasCharger,
                updatedAt = DateTime.Now,
                updatedBy = "JobScheduler"
            };
            return publish;
        }
    }
}