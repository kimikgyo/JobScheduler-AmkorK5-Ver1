using Common.Models.Bases;
using Common.Models.Jobs;

namespace JOB.Services
{
    public partial class SchedulerService
    {
        /// <summary>
        /// [Sub] MissionPostScheduler
        /// 미션 전송 ACS -> Service
        /// </summary>
        /// <param name="mission"></param>
        /// <returns></returns>
        private bool postMission(Mission mission)
        {
            bool CommandRequst = false;
            //[조건1] 미션 상태가 COMMANDREQUEST 다르면 COMMANDREQUEST 로 상태변경한다
            updateStateMission(mission, nameof(MissionState.COMMANDREQUEST), true);

            if (mission == null) return CommandRequst;
            if (mission != null && string.IsNullOrWhiteSpace(mission.service)) return CommandRequst;
            var serviceApi = _repository.ServiceApis.GetAll().FirstOrDefault(r => r.type == mission.service);
            if (serviceApi != null)
            {
                switch (mission.service)
                {
                    case nameof(Service.WORKER):
                        //[조건2] Service Api를 조회한다.
                        CommandRequst = WorkerPostMission(serviceApi, mission);
                        break;

                    case nameof(Service.MIDDLEWARE):
                        //미들웨어 전송 API로
                        CommandRequst = MiddleWarePostMission(serviceApi, mission);
                        break;

                    case nameof(Service.ELEVATOR):
                        CommandRequst = ElevatorPostMission(serviceApi, mission);
                        break;

                    case nameof(Service.TRAFFIC):
                        CommandRequst = TrafficPostMission(serviceApi, mission);
                        break;

                    case nameof(Service.IOT):
                        CommandRequst = IOTPostMission(serviceApi, mission);
                        break;

                    case nameof(Service.JOBSCHEDULER):
                        CommandRequst = JobSchedulerPostMission(serviceApi, mission);
                        break;
                }
            }
            else
            {
                EventLogger.Warn($"[PostMission][WORKER][API_IsNull] MissionName = {mission.name}, MissionSubType = {mission.subType}" +
                                 $", MissionId = {mission.guid}, AssignedWorkerId = {mission.assignedWorkerId}, AssignedWorkerName = {mission.assignedWorkerName}");
                return CommandRequst;
            }
            if (CommandRequst)
            {
                updateStateMission(mission, nameof(MissionState.COMMANDREQUESTCOMPLETED), true);
            }
            return CommandRequst;
        }

        private bool JobSchedulerPostMission(ServiceApi service, Mission mission)
        {
            bool CommandRequst = false;
            EventLogger.Info($"[PostMission][WORKER][Success], Message = OK, MissionName = {mission.name}, MissionSubType = {mission.subType}" +
                                               $", MissionId = {mission.guid}, AssignedWorkerId = {mission.assignedWorkerId}, AssignedWorkerName = {mission.assignedWorkerName}");
            return CommandRequst = true;
        }

        private bool WorkerPostMission(ServiceApi service, Mission mission)
        {
            bool CommandRequst = false;
            bool elevatorparamMapping = workerElevatorParameterMapping(mission);
            if (elevatorparamMapping)
            {
                //[조건3] API 형식에 맞추어서 Mapping 을 한다.
                var mapping_mission = _mapping.Missions.Request(mission);
                if (mapping_mission != null)
                {
                    //[조건4] Service 로 Api Mission 전송을 한다.
                    var postmission = service.Api.Post_Worker_Mission_Async(mapping_mission).Result;
                    if (postmission != null)
                    {
                        //[조건5] 상태코드 200~300 까지는 완료 처리
                        if (postmission.statusCode >= 200 && postmission.statusCode < 300)
                        {
                            EventLogger.Info($"[PostMission][WORKER][Success], Message = {postmission.statusText}, MissionName = {mission.name}, MissionSubType = {mission.subType}" +
                                             $", MissionId = {mission.guid}, AssignedWorkerId = {mission.assignedWorkerId}, AssignedWorkerName = {mission.assignedWorkerName}");
                            CommandRequst = true;
                        }
                        else EventLogger.Warn($"[PostMission][WORKER][Failed], Message = {postmission.message}, MissionName = {mission.name}, MissionSubType = {mission.subType}" +
                                              $", MissionId = {mission.guid}, AssignedWorkerId = {mission.assignedWorkerId}, AssignedWorkerName = {mission.assignedWorkerName}");
                    }
                    else EventLogger.Warn($"[PostMission][WORKER][APIResponseIsNull] MissionName = {mission.name}, MissionSubType = {mission.subType}" +
                                          $", MissionId = {mission.guid}, AssignedWorkerId = {mission.assignedWorkerId}, AssignedWorkerName = {mission.assignedWorkerName}");
                }
            }

            return CommandRequst;
        }

        private bool ElevatorPostMission(ServiceApi service, Mission mission)
        {
            bool CommandRequst = false;
            //[조건3] API 형식에 맞추어서 Mapping 을 한다.
            var mapping_mission = _mapping.Missions.Request(mission);
            if (mapping_mission != null)
            {
                //[조건4] Service 로 Api Mission 전송을 한다.
                var postmission = service.Api.Post_Elevator_Mission_Async(mapping_mission).Result;
                if (postmission != null)
                {
                    //[조건5] 상태코드 200~300 까지는 완료 처리
                    if (postmission.statusCode >= 200 && postmission.statusCode < 300)
                    {
                        EventLogger.Info($"[PostMission][ELEVATOR][Success], Message = {postmission.statusText}, MissionName = {mission.name}, MissionSubType = {mission.subType}" +
                                         $", MissionId = {mission.guid}, AssignedWorkerId = {mission.assignedWorkerId}, AssignedWorkerName = {mission.assignedWorkerName}");
                        CommandRequst = true;
                    }
                    else EventLogger.Warn($"[PostMission][ELEVATOR][Failed], Message = {postmission.message}, MissionName = {mission.name}, MissionSubType = {mission.subType}" +
                                         $", MissionId = {mission.guid}, AssignedWorkerId = {mission.assignedWorkerId}, AssignedWorkerName = {mission.assignedWorkerName}");
                }
                else EventLogger.Warn($"[PostMission][ELEVATOR][APIResponseIsNull] MissionName = {mission.name}, MissionSubType = {mission.subType}" +
                                          $", MissionId = {mission.guid}, AssignedWorkerId = {mission.assignedWorkerId}, AssignedWorkerName = {mission.assignedWorkerName}");
            }
            return CommandRequst;
        }

        private bool TrafficPostMission(ServiceApi service, Mission mission)
        {
            bool CommandRequst = false;
            //[조건3] API 형식에 맞추어서 Mapping 을 한다.
            var mapping_mission = _mapping.Missions.Request(mission);
            if (mapping_mission != null)
            {
                //[조건4] Service 로 Api Mission 전송을 한다.
                var postmission = service.Api.Post_Traffic_Mission_Async(mapping_mission).Result;
                if (postmission != null)
                {
                    //[조건5] 상태코드 200~300 까지는 완료 처리
                    if (postmission.statusCode >= 200 && postmission.statusCode < 300)
                    {
                        EventLogger.Info($"[PostMission][TRAFFIC][Success], Message = {postmission.statusText}, MissionName = {mission.name}, MissionSubType = {mission.subType}" +
                                         $", MissionId = {mission.guid}, AssignedWorkerId = {mission.assignedWorkerId}, AssignedWorkerName = {mission.assignedWorkerName}");
                        CommandRequst = true;
                    }
                    else EventLogger.Warn($"[PostMission][TRAFFIC][Failed], Message = {postmission.message}, MissionName = {mission.name}, MissionSubType = {mission.subType}" +
                                          $", MissionId = {mission.guid}, AssignedWorkerId = {mission.assignedWorkerId}, AssignedWorkerName = {mission.assignedWorkerName}");
                }
                else EventLogger.Warn($"[PostMission][TRAFFIC][APIResponseIsNull] MissionName = {mission.name}, MissionSubType = {mission.subType}" +
                                        $", MissionId = {mission.guid}, AssignedWorkerId = {mission.assignedWorkerId}, AssignedWorkerName = {mission.assignedWorkerName}");
            }
            return CommandRequst;
        }

        private bool MiddleWarePostMission(ServiceApi service, Mission mission)
        {
            bool CommandRequst = false;
            var mapping_mission = _mapping.Missions.Request(mission);
            if (mapping_mission != null)
            {
                var postmission = service.Api.Post_Middleware_Mission_Async(mapping_mission).Result;
                if (postmission != null)
                {
                    if (postmission.statusCode >= 200 && postmission.statusCode < 300)
                    {
                        EventLogger.Info($"[PostMission][MIDDLEWARE][Success], Message = {postmission.statusText}, MissionName = {mission.name}, MissionSubType = {mission.subType}" +
                                         $", MissionId = {mission.guid}, AssignedWorkerId = {mission.assignedWorkerId}, AssignedWorkerName = {mission.assignedWorkerName}");
                        CommandRequst = true;
                    }
                    else EventLogger.Warn($"[PostMission][MIDDLEWARE][Failed], Message = {postmission.message}, MissionName = {mission.name}, MissionSubType = {mission.subType}" +
                                         $", MissionId = {mission.guid}, AssignedWorkerId = {mission.assignedWorkerId}, AssignedWorkerName = {mission.assignedWorkerName}");
                }
                else EventLogger.Warn($"[PostMission][MIDDLEWARE][APIResponseIsNull] MissionName = {mission.name}, MissionSubType = {mission.subType}" +
                                           $", MissionId = {mission.guid}, AssignedWorkerId = {mission.assignedWorkerId}, AssignedWorkerName = {mission.assignedWorkerName}");
            }
            return CommandRequst;
        }

        private bool IOTPostMission(ServiceApi service, Mission mission)
        {
            bool CommandRequst = false;
            //[조건3] API 형식에 맞추어서 Mapping 을 한다.
            var mapping_mission = _mapping.Missions.Request(mission);
            if (mapping_mission != null)
            {
                //[조건4] Service 로 Api Mission 전송을 한다.
                var postmission = service.Api.Post_IOT_Mission_Async(mapping_mission).Result;
                if (postmission != null)
                {
                    //[조건5] 상태코드 200~300 까지는 완료 처리
                    if (postmission.statusCode >= 200 && postmission.statusCode < 300)
                    {
                        EventLogger.Info($"[PostMission][IOT][Success], Message = {postmission.statusText}, MissionName = {mission.name}, MissionSubType = {mission.subType}" +
                                         $", MissionId = {mission.guid}, AssignedWorkerId = {mission.assignedWorkerId}, AssignedWorkerName = {mission.assignedWorkerName}");
                        CommandRequst = true;
                    }
                    else EventLogger.Warn($"[PostMission][IOT][Failed], Message = {postmission.message}, MissionName = {mission.name}, MissionSubType = {mission.subType}" +
                                          $", MissionId = {mission.guid}, AssignedWorkerId = {mission.assignedWorkerId}, AssignedWorkerName = {mission.assignedWorkerName}");
                }
                else EventLogger.Warn($"[PostMission][IOT][APIResponseIsNull] MissionName = {mission.name}, MissionSubType = {mission.subType}" +
                                        $", MissionId = {mission.guid}, AssignedWorkerId = {mission.assignedWorkerId}, AssignedWorkerName = {mission.assignedWorkerName}");
            }
            return CommandRequst;
        }
    }
}