using Common.DTOs.Rests.Maps;
using Common.Models.Bases;

namespace JOB.Mappings.Bases
{
    public class MapMapping
    {
        public Map ApiGetResourceResponse(Response_MapDto model)
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