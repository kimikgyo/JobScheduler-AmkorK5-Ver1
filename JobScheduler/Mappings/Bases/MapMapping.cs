using Common.DTOs.Bases;
using Common.Models.Bases;

namespace JOB.Mappings.Bases
{
    public class MapMapping
    {
        public Map ApiGetResourceResponse(ApiGetResponseDtoResourceMap model)
        {
            var response = new Map
            {
                id = model._id,
                mapId = model.mapId,
                source = model.source,
                level = model.level,
                name = model.name,
            };
            return response;
        }
    }
}