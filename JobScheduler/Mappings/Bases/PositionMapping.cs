using Common.DTOs.Bases;
using Common.Models.Jobs;

namespace JOB.Mappings.Bases
{
    public class PositionMapping
    {
        public Position ApiGetResourceResponse(ApiGetResponseDtoResourcePosition model)
        {
            var response = new Position
            {
                id = model._id,
                source = model.source,
                group = model.groupId,
                type = model.type.Replace(" ", "").ToUpper(),
                subType = model.subType.Replace(" ", "").ToUpper(),
                mapId = model.mapId,
                name = model.name,
                x = model.x,
                y = model.y,
                theth = model.theta,
                isDisplayed = model.isDisplayed,
                isEnabled = model.isEnabled,
                isOccupied = model.isOccupied,
                linkedFacility = model.linkedFacility,
                linkedRobotId = model.linkedRobotId,
                hasCharger = model.hasCharger,
            };
            return response;
        }

        public ApiPatchDtoPositionOccupied ApiPatchResourceRequest(Position model)
        {
            var Request = new ApiPatchDtoPositionOccupied()
            {
                isOccupied = model.isOccupied,
            };
            return Request;
        }

        public MqttPublishDtoPosition MqttPublish(Position model)
        {
            var publish = new MqttPublishDtoPosition()
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