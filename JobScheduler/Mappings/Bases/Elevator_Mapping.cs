using Common.DTOs.MQTTs.Elevator;
using Common.Models.Bases;
using Common.Models.Jobs;

namespace JOB.Mappings.Bases
{
    public class Elevator_Mapping
    {
        public Elevator MqttCreateElevator(Subscribe_ElevatorStatusDto statusDto)
        {
            var model = new Elevator
            {
                id = statusDto.id,
                name = statusDto.name,
                mode = statusDto.mode,
                state = statusDto.state,
                createAt = DateTime.Now,
            };
            return model;
        }

    }
}
