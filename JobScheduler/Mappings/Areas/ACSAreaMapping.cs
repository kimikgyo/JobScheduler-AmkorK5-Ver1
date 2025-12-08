using Common.DTOs.Rests.Areas;
using Common.Models.Areas;
using System.Text.RegularExpressions;

namespace JOB.Mappings.Areas
{
    public class ACSAreaMapping
    {
        public ACSArea Response(Response_ACS_AareDto response_ACS_AareDto)
        {
            var response = new ACSArea
            {
                areaId = response_ACS_AareDto.areaId,
                mapId = response_ACS_AareDto.mapId,
                name = response_ACS_AareDto.name,
                type = response_ACS_AareDto.type,
                subType = response_ACS_AareDto.subType,
                groupId = response_ACS_AareDto.groupId,
                linkedNode = response_ACS_AareDto.linkedNode,
                linkedFacility = response_ACS_AareDto.linkedFacility,
                x = response_ACS_AareDto.x,
                y = response_ACS_AareDto.y,
                theta = response_ACS_AareDto.theta,
                width = response_ACS_AareDto.width,
                height = response_ACS_AareDto.height,
                isDisplayed = response_ACS_AareDto.isDisplayed,
                isEnabled = response_ACS_AareDto.isEnabled
            };
            return response;
        }
    }
}