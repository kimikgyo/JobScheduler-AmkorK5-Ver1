using Common.DTOs.Bases;
using Common.Models.Bases;
using Common.Models.Jobs;
using Common.Templates;
using System.Reflection;

namespace JOB.Mappings.Bases
{
    public class CarrierMapping
    {
        public Carrier ApiGetResourceResponse(ApiGetResponseDtoResourceCarrier model)
        {
            var response = new Carrier
            {
                carrierId = model.carrierId,
                name = model.name,
                location = model.location,
                installedTime = model.installedTime,
                workerId = model.workerId
            };

            return response;
        }

        public Carrier MqttUpdate(Carrier carrier, MqttSubscribeDtoCarrier Dto)
        {
            carrier.carrierId = Dto.carrierId;
            carrier.name = Dto.name;
            carrier.location = Dto.location;
            carrier.installedTime = Dto.installedTime;
            carrier.workerId = Dto.workerId;

            return carrier;
        }
        public Carrier MqttCreate( MqttSubscribeDtoCarrier Dto)
        {
            var response = new Carrier
            {
                carrierId = Dto.carrierId,
                name = Dto.name,
                location = Dto.location,
                installedTime = Dto.installedTime,
                workerId = Dto.workerId
            };

            return response;
        }
    }
}