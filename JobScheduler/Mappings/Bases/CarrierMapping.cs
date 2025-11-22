using Common.DTOs.MQTTs.Carriers;
using Common.DTOs.Rests.Carriers;
using Common.Models.Bases;

namespace JOB.Mappings.Bases
{
    public class CarrierMapping
    {
        public Carrier ApiGetResourceResponse(Response_CarrierDto model)
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

        public Carrier MqttUpdate(Carrier carrier, Subscribe_CarrierDto Dto)
        {
            carrier.carrierId = Dto.carrierId;
            carrier.name = Dto.name;
            carrier.location = Dto.location;
            carrier.installedTime = Dto.installedTime;
            carrier.workerId = Dto.workerId;

            return carrier;
        }

        public Carrier MqttCreate(Subscribe_CarrierDto Dto)
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