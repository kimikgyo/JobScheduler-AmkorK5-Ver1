using Common.Models.Jobs;

namespace JOB.Services
{
    public partial class SchedulerService
    {
        private void manualTransport_PickAndDrop_Control()
        {
            var missions = _repository.Missions.GetAll().Where(r => r.service == nameof(Service.JOBSCHEDULER) && r.state == nameof(MissionState.COMMANDREQUESTCOMPLETED)
                                                        && (r.subType == nameof(MissionSubType.MANUALTRANSPORTPICK) || r.subType == nameof(MissionSubType.MANUALTRANSPORTDROP))).ToList();

            foreach (var mission in missions)
            {
                updateStateMission(mission, nameof(MissionState.EXECUTING), "manualTransport_PickAndDrop_Control", true);
            }
        }
    }
}