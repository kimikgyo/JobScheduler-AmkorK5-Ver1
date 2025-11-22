using Common.DTOs.MQTTs.Missions;
using Common.DTOs.Rests.Missions;
using Common.Models.Jobs;

namespace JOB.Mappings.Jobs
{
    public class MissionMapping
    {
        public Get_MissionDto Response(Mission model)
        {
            var response = new Get_MissionDto()
            {
                orderId = model.orderId,
                jobId = model.jobId,
                guid = model.guid,
                carrierId = model.carrierId,
                name = model.name,
                service = model.service,
                type = model.type,
                subType = model.subType,
                sequence = model.sequence,
                linkedFacility = model.linkedFacility,
                isLocked = model.isLocked,
                sequenceChangeCount = model.sequenceChangeCount,
                retryCount = model.retryCount,
                state = model.state,
                specifiedWorkerId = model.specifiedWorkerId,
                assignedWorkerId = model.assignedWorkerId,
                createdAt = model.createdAt,
                updatedAt = model.updatedAt,
                finishedAt = model.finishedAt,
                sequenceUpdatedAt = model.sequenceUpdatedAt,
                parameters = model.parameters,
                preReports = model.preReports,
                postReports = model.postReports
            };

            return response;
        }

        public Publish_MissionDto MqttPublish(Mission model)
        {
            var publish = new Publish_MissionDto()
            {
                orderId = model.orderId,
                jobId = model.jobId,
                guid = model.guid,
                carrierId = model.carrierId,
                name = model.name,
                service = model.service,
                type = model.type,
                subType = model.subType,
                sequence = model.sequence,
                linkedFacility = model.linkedFacility,
                isLocked = model.isLocked,
                sequenceChangeCount = model.sequenceChangeCount,
                retryCount = model.retryCount,
                state = model.state,
                specifiedWorkerId = model.specifiedWorkerId,
                assignedWorkerId = model.assignedWorkerId,
                parameters = model.parameters,
                preReports = model.preReports,
                postReports = model.postReports
            };
            return publish;
        }

        public Mission MqttUpdateStatus(Mission model, Subscribe_MissionDto missionData)
        {
            model.state = missionData.state.Replace(" ", "").ToUpper();
            model.updatedAt = DateTime.Now;

            return model;
        }

        public Request_MissionDto ApiRequestDtoPostMission(Mission model)
        {
            var apiRequest = new Request_MissionDto
            {
                orderId = model.orderId,
                jobId = model.jobId,
                guid = model.guid,
                carrierId = model.carrierId,
                name = model.name,
                service = model.service,
                type = model.type,
                subType = model.subType,
                sequence = model.sequence,
                linkedFacility = model.linkedFacility,
                isLocked = model.isLocked,
                sequenceChangeCount = model.sequenceChangeCount,
                retryCount = model.retryCount,
                state = model.state,
                specifiedWorkerId = model.specifiedWorkerId,
                assignedWorkerId = model.assignedWorkerId,
                parameters = model.parameters,
            };

            return apiRequest;
        }
    }
}