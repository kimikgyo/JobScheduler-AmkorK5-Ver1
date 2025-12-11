using Common.Models.Jobs;

namespace JOB.Services
{
    public partial class SchedulerService
    {
        /// <summary>
        /// [Sub] postMissionControl
        /// 미션 전송 ACS -> Service
        /// </summary>
        /// <param name="mission"></param>
        /// <returns></returns>
        private bool postMission(Mission mission)
        {
            bool CommandRequst = false;
            //[조건1] 미션 상태가 COMMANDREQUEST 다르면 COMMANDREQUEST 로 상태변경한다

            updateStateMission(mission, nameof(MissionState.COMMANDREQUEST), true);

            switch (mission.service)
            {
                case nameof(Service.WORKER):
                    //[조건2] Service Api를 조회한다.
                    CommandRequst = WorkerPostMission(mission);
                    break;

                case nameof(Service.MIDDLEWARE):
                    //미들웨어 전송 API로
                    CommandRequst = MiddleWarePostMission(mission);
                    break;

                case nameof(Service.ELEVATOR):
                    CommandRequst = ElevatorPostMission(mission);
                    break;

                case nameof(Service.TRAFFIC):
                    CommandRequst = TrafficPostMission(mission);
                    break;
            }
            if (CommandRequst)
            {
                updateStateMission(mission, nameof(MissionState.COMMANDREQUESTCOMPLETED), true);
            }
            return CommandRequst;
        }

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

        private bool WorkerPostMission(Mission mission)
        {
            bool CommandRequst = false;
            var workerApi = _repository.ServiceApis.GetAll().FirstOrDefault(r => r.type == "worker");
            if (workerApi != null)
            {
                bool elevatorparamMapping = workerElevatorParameterMapping(mission);
                if (elevatorparamMapping)
                {
                    //[조건3] API 형식에 맞추어서 Mapping 을 한다.
                    var mapping_mission = _mapping.Missions.Request(mission);
                    if (mapping_mission != null)
                    {
                        //[조건4] Service 로 Api Mission 전송을 한다.
                        var postmission = workerApi.Api.Post_Worker_Mission_Async(mapping_mission).Result;
                        if (postmission != null)
                        {
                            //[조건5] 상태코드 200~300 까지는 완료 처리
                            if (postmission.statusCode >= 200 && postmission.statusCode < 300)
                            {
                                EventLogger.Info($"[PostMission][WORKER][Success], Message = {postmission.statusText}, MissionName = {mission.name}, MissionId = {mission.guid}" +
                                                 $", AssignedWorkerId = {mission.assignedWorkerId}");
                                CommandRequst = true;
                            }
                            else EventLogger.Warn($"[PostMission][WORKER][Failed], Message = {postmission.message}, MissionName = {mission.name}, MissionId = {mission.guid}" +
                                                  $", AssignedWorkerId = {mission.assignedWorkerId}");
                        }
                    }
                }
            }

            return CommandRequst;
        }

        private bool ElevatorPostMission(Mission mission)
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
                    var postmission = elevatorApi.Api.Post_Elevator_Mission_Async(mapping_mission).Result;
                    if (postmission != null)
                    {
                        //[조건5] 상태코드 200~300 까지는 완료 처리
                        if (postmission.statusCode >= 200 && postmission.statusCode < 300)
                        {
                            EventLogger.Info($"[PostMission][ELEVATOR][Success], Message = {postmission.statusText}, MissionName = {mission.name}, MissionId = {mission.guid}" +
                                             $", AssignedWorkerId = {mission.assignedWorkerId}");
                            CommandRequst = true;
                        }
                        else EventLogger.Warn($"[PostMission][ELEVATOR][Failed], Message = {postmission.message}, MissionName = {mission.name}, MissionId = {mission.guid}" +
                                              $", AssignedWorkerId = {mission.assignedWorkerId}");
                    }
                }
            }
            return CommandRequst;
        }

        private bool TrafficPostMission(Mission mission)
        {
            bool CommandRequst = false;
            var Api = _repository.ServiceApis.GetAll().FirstOrDefault(r => r.type == "traffic");
            if (Api != null)
            {
                //[조건3] API 형식에 맞추어서 Mapping 을 한다.
                var mapping_mission = _mapping.Missions.Request(mission);
                if (mapping_mission != null)
                {
                    //[조건4] Service 로 Api Mission 전송을 한다.
                    var postmission = Api.Api.Post_Traffic_Mission_Async(mapping_mission).Result;
                    if (postmission != null)
                    {
                        //[조건5] 상태코드 200~300 까지는 완료 처리
                        if (postmission.statusCode >= 200 && postmission.statusCode < 300)
                        {
                            EventLogger.Info($"[PostMission][TRAFFIC][Success], Message = {postmission.statusText}, MissionName = {mission.name}, MissionId = {mission.guid}" +
                                             $", AssignedWorkerId = {mission.assignedWorkerId}");
                            CommandRequst = true;
                        }
                        else EventLogger.Warn($"[PostMission][TRAFFIC][Failed], Message = {postmission.message}, MissionName = {mission.name}, MissionId = {mission.guid}" +
                                              $", AssignedWorkerId = {mission.assignedWorkerId}");
                    }
                }
            }
            return CommandRequst;
        }

        private bool MiddleWarePostMission(Mission mission)
        {
            bool CommandRequst = false;
            var middlewareApi = _repository.ServiceApis.GetAll().FirstOrDefault(r => r.type == "worker");
            if (middlewareApi != null)
            {
                var mapping_mission = _mapping.Missions.Request(mission);
                if (mapping_mission != null)
                {
                    var postmission = middlewareApi.Api.Post_Middleware_Mission_Async(mapping_mission).Result;
                    if (postmission != null)
                    {
                        if (postmission.statusCode >= 200 && postmission.statusCode < 300)
                        {
                            EventLogger.Info($"[PostMission][MIDDLEWARE][Success], Message = {postmission.statusText}, MissionId = {mission.guid}" +
                                             $", AssignedWorkerId = {mission.assignedWorkerId}");
                            CommandRequst = true;
                        }
                        else EventLogger.Warn($"[PostMission][MIDDLEWARE][Failed], Message = {postmission.message}, MissionId = {mission.guid}" +
                                              $", AssignedWorkerId = {mission.assignedWorkerId}");
                    }
                }
            }
            return CommandRequst;
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
                        EventLogger.Info($"[DeleteMission][WORKER][Success], Message = {postmission.statusText}, MissionId = {mission.guid}" +
                                         $", AssignedWorkerId = {mission.assignedWorkerId}");
                        CommandRequst = true;
                    }
                    else EventLogger.Warn($"[DeleteMission][WORKER][Failed], Message = {postmission.message}, MissionId = {mission.guid}" +
                                          $", AssignedWorkerId = {mission.assignedWorkerId}");
                }
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
                            EventLogger.Info($"[DeleteMission][ELEVATOR][Success], Message = {postmission.statusText}, MissionName = {mission.name}, MissionId = {mission.guid}" +
                                             $", AssignedWorkerId = {mission.assignedWorkerId}");
                            CommandRequst = true;
                        }
                        else EventLogger.Warn($"[DeleteMission][ELEVATOR][Failed], Message = {postmission.message}, MissionName = {mission.name}, MissionId = {mission.guid}" +
                                             $", AssignedWorkerId = {mission.assignedWorkerId}");
                    }
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
                            EventLogger.Info($"[DeleteMission][TRAFFIC][Success], Message = {postmission.statusText}, MissionName = {mission.name}, MissionId = {mission.guid}" +
                                             $", AssignedWorkerId = {mission.assignedWorkerId}");
                            CommandRequst = true;
                        }
                        else EventLogger.Warn($"[DeleteMission][TRAFFIC][Failed], Message = {postmission.message}, MissionName = {mission.name}, MissionId = {mission.guid}" +
                                             $", AssignedWorkerId = {mission.assignedWorkerId}");
                    }
                }
            }
            return CommandRequst;
        }
    }
}