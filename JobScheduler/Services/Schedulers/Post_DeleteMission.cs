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
        private void deleteMission(Mission mission)
        {
            switch (mission.service)
            {
                case nameof(Service.WORKER):

                    WorkerDeleteMission(mission);
                    break;

                case nameof(Service.MIDDLEWARE):

                    break;

                case nameof(Service.ELEVATOR):

                    ElevatorDeleteMission(mission);
                    break;
            }
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
                    var mapping_mission = _mapping.Missions.ApiRequestDtoPostMission(mission);
                    if (mapping_mission != null)
                    {
                        //[조건4] Service 로 Api Mission 전송을 한다.
                        var postmission = workerApi.Api.WorkerPostMissionQueueAsync(mapping_mission).Result;
                        if (postmission != null)
                        {
                            //[조건5] 상태코드 200~300 까지는 완료 처리
                            if (postmission.statusCode >= 200 && postmission.statusCode < 300)
                            {
                                EventLogger.Info($"PostMission Success = Service = {nameof(Service.WORKER)}, Message = {postmission.statusText}, MissionName = {mission.name}, MissionId = {mission.guid}, AssignedWorkerId = {mission.assignedWorkerId}");
                                CommandRequst = true;
                            }
                            else EventLogger.Info($"PostMission Failed = Service = {nameof(Service.WORKER)}, Message = {postmission.message}, MissionName = {mission.name}, MissionId = {mission.guid}, AssignedWorkerId = {mission.assignedWorkerId}");
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
                var mapping_mission = _mapping.Missions.ApiRequestDtoPostMission(mission);
                if (mapping_mission != null)
                {
                    //[조건4] Service 로 Api Mission 전송을 한다.
                    var postmission = elevatorApi.Api.ElevatorPostMissionQueueAsync(mapping_mission).Result;
                    if (postmission != null)
                    {
                        //[조건5] 상태코드 200~300 까지는 완료 처리
                        if (postmission.statusCode >= 200 && postmission.statusCode < 300)
                        {
                            EventLogger.Info($"PostMission Success = Service = {nameof(Service.ELEVATOR)}, Message = {postmission.statusText}, MissionName = {mission.name}, MissionId = {mission.guid}, AssignedWorkerId = {mission.assignedWorkerId}");
                            CommandRequst = true;
                        }
                        else EventLogger.Info($"PostMission Failed = Service = {nameof(Service.ELEVATOR)}, Message = {postmission.message}, MissionName = {mission.name}, MissionId = {mission.guid}, AssignedWorkerId = {mission.assignedWorkerId}");
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
                var mapping_mission = _mapping.Missions.ApiRequestDtoPostMission(mission);
                if (mapping_mission != null)
                {
                    var postmission = middlewareApi.Api.MiddlewarePostMissionQueueAsync(mapping_mission).Result;
                    if (postmission != null)
                    {
                        if (postmission.statusCode >= 200 && postmission.statusCode < 300)
                        {
                            EventLogger.Info($"PostMission Success = Service = {nameof(Service.MIDDLEWARE)}, Message = {postmission.statusText}, MissionId = {mission.guid}, AssignedWorkerId = {mission.assignedWorkerId}");
                            CommandRequst = true;
                        }
                        else EventLogger.Info($"PostMission Failed = Service = {nameof(Service.MIDDLEWARE)}, Message = {postmission.message}, MissionId = {mission.guid}, AssignedWorkerId = {mission.assignedWorkerId}");
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
                var postmission = workerApi.Api.WorkerDeleteMissionQueueAsync(mission.guid).Result;
                if (postmission != null)
                {
                    if (postmission.statusCode >= 200 && postmission.statusCode < 300)
                    {
                        EventLogger.Info($"DeleteMission Success = Service = {nameof(Service.WORKER)}, Message = {postmission.statusText}, MissionId = {mission.guid}, AssignedWorkerId = {mission.assignedWorkerId}");
                        CommandRequst = true;
                    }
                    else EventLogger.Info($"DeleteMission Failed = Service = {nameof(Service.WORKER)}, Message = {postmission.message}, MissionId = {mission.guid}, AssignedWorkerId = {mission.assignedWorkerId}");
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
                var mapping_mission = _mapping.Missions.ApiRequestDtoPostMission(mission);
                if (mapping_mission != null)
                {
                    //[조건4] Service 로 Api Mission 전송을 한다.
                    var postmission = elevatorApi.Api.ElevatorDeletetMissionQueueAsync(mapping_mission.guid).Result;
                    if (postmission != null)
                    {
                        //[조건5] 상태코드 200~300 까지는 완료 처리
                        if (postmission.statusCode >= 200 && postmission.statusCode < 300)
                        {
                            EventLogger.Info($"DeleteMission Success = Service = {nameof(Service.ELEVATOR)}, Message = {postmission.statusText}, MissionName = {mission.name}, MissionId = {mission.guid}, AssignedWorkerId = {mission.assignedWorkerId}");
                            CommandRequst = true;
                        }
                        else EventLogger.Info($"DeleteMission Failed = Service = {nameof(Service.ELEVATOR)}, Message = {postmission.message}, MissionName = {mission.name}, MissionId = {mission.guid}, AssignedWorkerId = {mission.assignedWorkerId}");
                    }
                }
            }
            return CommandRequst;
        }
    }
}