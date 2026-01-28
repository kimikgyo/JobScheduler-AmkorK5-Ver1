using Common.Models.Bases;
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
            if (mission == null) return completed;
            if (mission != null && string.IsNullOrWhiteSpace(mission.service)) return completed;
            var serviceApi = _repository.ServiceApis.GetAll().FirstOrDefault(r => r.type == mission.service);
            if (serviceApi != null)
            {
                switch (mission.service)
                {
                    case nameof(Service.WORKER):

                        completed = WorkerDeleteMission(serviceApi, mission);
                        break;

                    case nameof(Service.MIDDLEWARE):

                        break;

                    case nameof(Service.ELEVATOR):

                        completed = ElevatorDeleteMission(serviceApi, mission);
                        break;

                    case nameof(Service.TRAFFIC):
                        completed = TrafficDeleteMission(serviceApi, mission);
                        break;

                    case nameof(Service.IOT):
                        completed = IOTDeleteMission(serviceApi, mission);
                        break;
                }
            }
            else
            {
                if (mission.service == nameof(Service.JOBSCHEDULER))
                {
                    updateStateMission(mission, nameof(MissionState.CANCELED), true);
                    completed = true;
                }
                else
                {
                    EventLogger.Warn($"[DeleteMission][API_IsNull] MissionName = {mission.name}, MissionSubType = {mission.subType}" +
                                     $", MissionId = {mission.guid}, AssignedWorkerId = {mission.assignedWorkerId}, AssignedWorkerName = {mission.assignedWorkerName}");
                }
            }
            return completed;
        }

        private bool WorkerDeleteMission(ServiceApi service, Mission mission)
        {
            bool CommandRequst = false;
            //Subscribe_Worker 전송 API로

            var postmission = service.Api.Delete_Worker_Mission_Async(mission.guid).Result;
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
            return CommandRequst;
        }

        private bool ElevatorDeleteMission(ServiceApi service, Mission mission)
        {
            bool CommandRequst = false;

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
            return CommandRequst;
        }

        private bool TrafficDeleteMission(ServiceApi service, Mission mission)
        {
            bool CommandRequst = false;
            //[조건3] API 형식에 맞추어서 Mapping 을 한다.
            var mapping_mission = _mapping.Missions.Request(mission);
            if (mapping_mission != null)
            {
                //[조건4] Service 로 Api Mission 전송을 한다.
                var postmission = service.Api.Deletet_Traffic_Mission_Async(mapping_mission.guid).Result;
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
            return CommandRequst;
        }

        private bool IOTDeleteMission(ServiceApi service, Mission mission)
        {
            bool CommandRequst = false;
            //[조건3] API 형식에 맞추어서 Mapping 을 한다.
            var mapping_mission = _mapping.Missions.Request(mission);
            if (mapping_mission != null)
            {
                //[조건4] Service 로 Api Mission 전송을 한다.
                var postmission = service.Api.Deletet_IOT_Mission_Async(mapping_mission.guid).Result;
                if (postmission != null)
                {
                    //[조건5] 상태코드 200~300 까지는 완료 처리
                    if (postmission.statusCode >= 200 && postmission.statusCode < 300)
                    {
                        EventLogger.Info($"[DeleteMission][IOT][Success], Message = {postmission.statusText}, MissionName = {mission.name}, MissionSubType = {mission.subType}" +
                                         $", MissionId = {mission.guid}, AssignedWorkerId = {mission.assignedWorkerId}, AssignedWorkerName = {mission.assignedWorkerName}");
                        CommandRequst = true;
                    }
                    else EventLogger.Warn($"[DeleteMission][IOT][Failed], Message = {postmission.message}, MissionName = {mission.name}, MissionSubType = {mission.subType}" +
                                         $", MissionId = {mission.guid}, AssignedWorkerId = {mission.assignedWorkerId}, AssignedWorkerName = {mission.assignedWorkerName}");
                }
                else EventLogger.Warn($"[DeleteMission][IOT][APIResponseIsNull] MissionName = {mission.name}, MissionSubType = {mission.subType}" +
                                     $", MissionId = {mission.guid}, AssignedWorkerId = {mission.assignedWorkerId}, AssignedWorkerName = {mission.assignedWorkerName}");
            }
            return CommandRequst;
        }
    }
}