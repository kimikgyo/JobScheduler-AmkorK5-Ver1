using Common.Models.Jobs;

namespace JOB.Services
{
    public partial class SchedulerService
    {
        /// <summary>
        /// postDeleteMission
        /// 미션 삭제 요청 ACS -> Service
        /// </summary>
        /// <param name="mission"></param>
        private bool deleteMission(Mission mission)
        {
            bool completed = false;
            switch (mission.service)
            {
                case nameof(Service.WORKER):

                    completed = WorkerDeleteMission(mission);
                    break;

                case nameof(Service.MIDDLEWARE):

                    break;

                case nameof(Service.ELEVATOR):

                    completed = ElevatorDeleteMission(mission);
                    break;

                case nameof(Service.TRAFFIC):
                    completed = TrafficDeleteMission(mission);
                    break;
            }
            return completed;
        }

        private bool WorkerDeleteMission(Mission mission)
        {
            bool CommandRequst = false;
            //Worker 전송 API로
            var workerApi = _repository.ServiceApis.GetAll().FirstOrDefault(r => r.type == "worker");
            if (workerApi != null)
            {
                var postmission = workerApi.Api.Delete_Worker_Mission_Async(mission.guid).Result;
                if (postmission != null)
                {
                    if (postmission.statusCode >= 200 && postmission.statusCode < 300)
                    {
                        EventLogger.Info($"[DeleteMission][WORKER][Success], Message = {postmission.statusText}, MissionName = {mission.name}, MissionSubType = {mission.subType}" +
                                             $", MissionId = {mission.guid}, AssignedWorkerId = {mission.assignedWorkerId}, AssignedWorkerName = {mission.assignedWorkerName}");
                        CommandRequst = true;
                    }
                    else EventLogger.Warn($"[DeleteMission][WORKER][Failed], Message = {postmission.message}, MissionName = {mission.name}, MissionSubType = {mission.subType}" +
                                             $", MissionId = {mission.guid}, AssignedWorkerId = {mission.assignedWorkerId}, AssignedWorkerName = {mission.assignedWorkerName}");
                }
                else EventLogger.Warn($"[DeleteMission][WORKER][APIResponseIsNull] MissionName = {mission.name}, MissionSubType = {mission.subType}" +
                                      $", MissionId = {mission.guid}, AssignedWorkerId = {mission.assignedWorkerId}, AssignedWorkerName = {mission.assignedWorkerName}");
            }
            return CommandRequst;
        }

        private bool ElevatorDeleteMission(Mission mission)
        {
            bool CommandRequst = false;
            var elevatorApi = _repository.ServiceApis.GetAll().FirstOrDefault(r => r.type == "elevator");
            if (elevatorApi != null)
            {
                //[조건3] API 형식에 맞추어서 Mapping 을 한다.
                var mapping_mission = _mapping.Missions.Request(mission);
                if (mapping_mission != null)
                {
                    //[조건4] Service 로 Api Mission 전송을 한다.
                    var postmission = elevatorApi.Api.Deletet_Elevator_Mission_Async(mapping_mission.guid).Result;
                    if (postmission != null)
                    {
                        //[조건5] 상태코드 200~300 까지는 완료 처리
                        if (postmission.statusCode >= 200 && postmission.statusCode < 300)
                        {
                            EventLogger.Info($"[DeleteMission][ELEVATOR][Success], Message = {postmission.statusText}, MissionName = {mission.name}, MissionSubType = {mission.subType}" +
                                             $", MissionId = {mission.guid}, AssignedWorkerId = {mission.assignedWorkerId}, AssignedWorkerName = {mission.assignedWorkerName}");
                            CommandRequst = true;
                        }
                        else EventLogger.Warn($"[DeleteMission][ELEVATOR][Failed], Message = {postmission.message}, MissionName = {mission.name}, MissionSubType = {mission.subType}" +
                                             $", MissionId = {mission.guid}, AssignedWorkerId = {mission.assignedWorkerId}, AssignedWorkerName = {mission.assignedWorkerName}");
                    }
                    else EventLogger.Warn($"[DeleteMission][ELEVATOR][APIResponseIsNull] MissionName = {mission.name}, MissionSubType = {mission.subType}" +
                                         $", MissionId = {mission.guid}, AssignedWorkerId = {mission.assignedWorkerId}, AssignedWorkerName = {mission.assignedWorkerName}");
                }
            }
            return CommandRequst;
        }

        private bool TrafficDeleteMission(Mission mission)
        {
            bool CommandRequst = false;
            var service = _repository.ServiceApis.GetAll().FirstOrDefault(r => r.type == "traffic");
            if (service != null)
            {
                //[조건3] API 형식에 맞추어서 Mapping 을 한다.
                var mapping_mission = _mapping.Missions.Request(mission);
                if (mapping_mission != null)
                {
                    //[조건4] Service 로 Api Mission 전송을 한다.
                    var postmission = service.Api.Deletet_Elevator_Mission_Async(mapping_mission.guid).Result;
                    if (postmission != null)
                    {
                        //[조건5] 상태코드 200~300 까지는 완료 처리
                        if (postmission.statusCode >= 200 && postmission.statusCode < 300)
                        {
                            EventLogger.Info($"[DeleteMission][TRAFFIC][Success], Message = {postmission.statusText}, MissionName = {mission.name}, MissionSubType = {mission.subType}" +
                                             $", MissionId = {mission.guid}, AssignedWorkerId = {mission.assignedWorkerId}, AssignedWorkerName = {mission.assignedWorkerName}");
                            CommandRequst = true;
                        }
                        else EventLogger.Warn($"[DeleteMission][TRAFFIC][Failed], Message = {postmission.message}, MissionName = {mission.name}, MissionSubType = {mission.subType}" +
                                             $", MissionId = {mission.guid}, AssignedWorkerId = {mission.assignedWorkerId}, AssignedWorkerName = {mission.assignedWorkerName}");
                    }
                    else EventLogger.Warn($"[DeleteMission][TRAFFIC][APIResponseIsNull] MissionName = {mission.name}, MissionSubType = {mission.subType}" +
                                         $", MissionId = {mission.guid}, AssignedWorkerId = {mission.assignedWorkerId}, AssignedWorkerName = {mission.assignedWorkerName}");
                }
            }
            return CommandRequst;
        }
    }
}