using Common.DTOs.Bases;
using Common.Models.Jobs;

namespace JOB.Mappings.Bases
{
    public class WorkerMapping
    {
        public Worker ApiGetResourceResponse(ApiGetResponseDtoResourceWorker model)
        {
            var response = new Worker()
            {
                id = model._id,
                source = model.source,
                name = model.name,
                group = model.groupId,
            };
            return response;
        }

        public Worker MqttUpdateState(Worker worker, MqttSubscribeDtoWorkerStatus state)
        {
            worker.state = state.state.Replace(" ", "").ToUpper();
            if (state.battery.percent == null)
            {
                worker.batteryPercent = 0;
            }
            else worker.batteryPercent = Convert.ToDouble(state.battery.percent);

            if (state.pose.x == null)
            {
                worker.position_X = 0;
            }
            else worker.position_X = Convert.ToDouble(state.pose.x);
            if (state.pose.y == null)
            {
                worker.position_Y = 0;
            }
            else worker.position_Y = Convert.ToDouble(state.pose.y);
            if (state.pose.theta == null)
            {
                worker.position_Orientation = 0;
            }
            else worker.position_Orientation = Convert.ToDouble(state.pose.theta);

            worker.mapId = state.pose.mapId;
            worker.isOnline = state.connectivity.online;
            worker.isActive = state.application.isActive;

            return worker;
        }
    }
}