using Common.DTOs.Rests.JobTemplates;
using Common.Models.Bases;
using Common.Templates;

namespace JOB.Mappings.Bases
{
    public class JobTemplateMapping
    {
        public JobTemplate ApiGetResourceResponse(Response_JobTemplateDto model)
        {
            var response = new JobTemplate
            {
                id = model.id,
                group = model.group,
                type = model.type.Replace(" ", "").ToUpper(),
                subType = model.subType.Replace(" ", "").ToUpper(),
                isLocked = model.isLocked,
            };
            foreach (var missionTemplateDto in model.missionTemplates)
            {
                var missionTemplete = new MissionTemplate
                {
                    name = missionTemplateDto.name,
                    service = missionTemplateDto.service.Replace(" ", "").ToUpper(),
                    type = missionTemplateDto.type.Replace(" ", "").ToUpper(),
                    subType = missionTemplateDto.subType.Replace(" ", "").ToUpper(),
                    isLook = missionTemplateDto.isLook
                };

                foreach (var parameta in missionTemplateDto.parameters)
                {
                    var param = new Parameter
                    {
                        key = parameta.key,
                        value = parameta.value,
                    };
                    missionTemplete.parameters.Add(param);
                }
                foreach (var preReport in missionTemplateDto.preReports)
                {
                    var createPreReport = new PreReport
                    {
                        ceid = preReport.ceid,
                        eventName = preReport.eventName,
                        rptid = preReport.rptid,
                    };
                    missionTemplete.preReports.Add(createPreReport);
                }
                foreach (var postReport in missionTemplateDto.postReports)
                {
                    var createpostReport = new PostReport
                    {
                        ceid = postReport.ceid,
                        eventName = postReport.eventName,
                        rptid = postReport.rptid,
                    };
                    missionTemplete.postReports.Add(createpostReport);
                }

                response.missionTemplates.Add(missionTemplete);
            }
            return response;
        }
    }
}