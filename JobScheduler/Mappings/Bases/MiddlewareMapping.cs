using Common.DTOs.Bases;
using Common.Models.Bases;
using Common.Models.Jobs;
using System.Threading;

namespace JOB.Mappings.Bases
{
    public class MiddlewareMapping
    {
        public Middleware ApiGetResourceResponse(string workerId, ApiGetResponseDtoResourceMiddleware model)
        {
            var response = new Middleware()
            {
                workerId = workerId,
                id = model._id,
                ip = model.ip,
                port = model.port
            };
            return response;
        }

        public Middleware MqttUpdateState(Middleware middleware, MqttSubscribeDtoMiddlewareStatus state)
        {
            middleware.state = state.state.Replace(" ", "").ToUpper();
            middleware.isOnline = state.isOnline;
            middleware.isActive = state.isActive;
            middleware.carrier = state.carrier;
            middleware.acsmissionId = state.acsmissionId;
            return middleware;
        }
    }
}