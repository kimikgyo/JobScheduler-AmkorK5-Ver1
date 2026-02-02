using Common.Models.Jobs;

namespace JOB.Services
{
    public partial class SchedulerService
    {
        private bool skipMission(Mission mission, Worker worker)
        {
            bool completed = false;

            switch (mission.type)
            {
                case nameof(MissionType.MOVE):
                    if (worker.PositionId != null)
                    {
                        //[조건2] 이동 목적지 파라메타가 있는경우
                        var param = mission.parameters.FirstOrDefault(r => r.key == "target" && r.value != null);
                        if (param != null)
                        {
                            //[조건3]워커 포지션 Id와 이동하는 미션의 목적지 파라메타 와 일치하는경우
                            if (worker.PositionId == param.value)
                            {
                                updateStateMission(mission, nameof(MissionState.SKIPPED), true);
                                EventLogger.Info($"[PostMission][{nameof(Service.WORKER)}][SKIPPED], PositionId = {worker.PositionId}, PositionName = {worker.PositionName}, MissionId = {mission.guid}, AssignedWorkerId = {mission.assignedWorkerId}");
                                completed = true;
                            }
                        }
                    }
                    break;

                case nameof(MissionType.ACTION):
                    //if (mission.subType == nameof(MissionSubType.DOORCLOSE))
                    //{
                    //    var elevatorMoveMissions = _repository.Missions.GetAll().Where(r => r.subType == nameof(MissionSubType.ELEVATORWAITMOVE)
                    //                                                                    || r.subType == nameof(MissionSubType.ELEVATORENTERMOVE)
                    //                                                                    || r.subType == nameof(MissionSubType.ELEVATOREXITMOVE)
                    //                                                                    || r.subType == nameof(MissionSubType.RIGHTTURN)
                    //                                                                    || r.subType == nameof(MissionSubType.LEFTTURN)
                    //                                                                    || r.subType == nameof(MissionSubType.SWITCHINGMAP)).ToList();
                    //    var runmission = _repository.Missions.GetByRunMissions(elevatorMoveMissions).FirstOrDefault();

                    //    if (runmission != null)
                    //    {
                    //        updateStateMission(mission, nameof(MissionState.SKIPPED), true);
                    //        EventLogger.Info($"[PostMission][{nameof(Service.ELEVATOR)}][SKIPPED], MissionId = {mission.guid}, missionName = {mission.name} ,AssignedWorkerId = {mission.assignedWorkerId}");
                    //        completed = true;
                    //    }
                    //}
                    break;
            }

            return completed;
        }
    }
}
