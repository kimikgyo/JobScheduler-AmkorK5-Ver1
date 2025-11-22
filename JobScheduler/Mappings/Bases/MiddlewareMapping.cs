using Common.DTOs.MQTTs.Middlewares;
using Common.DTOs.Rests.Middlewares;
using Common.Models.Bases;

namespace JOB.Mappings.Bases
{
    public class MiddlewareMapping
    {
        public Middleware ApiGetResourceResponse(string workerId, Response_MiddlewareDto model)
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

        public Middleware MqttUpdateState(Middleware middleware, Subscribe_MiddlewareStatusDto state)
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