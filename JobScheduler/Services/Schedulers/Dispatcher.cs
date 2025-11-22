using Common.Models.Jobs;
using Common.Templates;
using System.Text.Json;

namespace JOB.Services
{
    public partial class SchedulerService
    {
        private void Dispatcher()
        {
            postMissionControl();
            cancelControl();
        }

        /// <summary>
        /// Cancel 제어
        /// </summary>
        private void cancelControl()
        {
            //[조회] Cancel Job 조회
            var CancelJobs = _repository.Jobs.GetAll().Where(j => j.terminateState == nameof(TerminateState.INITED)).ToList();

            foreach (var CancelJob in CancelJobs)
            {
                var missions = _repository.Missions.GetByJobId(CancelJob.guid);
                if (CancelJob.terminator.ToUpper() == "INATECH" || CancelJob.terminator.ToUpper() == "TABLET")
                {
                    //관리자가 취소할경우
                    var cancelMissions = missions.Where(m => m.state != nameof(MissionState.COMPLETED)).ToList();
                    foreach (var cancelMission in cancelMissions)
                    {
                        updateStateMission(cancelMission, nameof(MissionState.CANCELED));
                    }

                    CancelJob.terminateState = nameof(TerminateState.EXECUTING);
                    CancelJob.terminatingAt = DateTime.Now;
                    updateStateJob(CancelJob, CancelJob.state, true);
                    var order = _repository.Orders.GetByid(CancelJob.orderId);
                    if (order != null)
                    {
                        switch (CancelJob.terminationType)
                        {
                            case nameof(TerminateType.CANCEL):
                                updateStateOrder(order, OrderState.Canceling, true);
                                break;

                            case nameof(TerminateType.ABORT):
                                updateStateOrder(order, OrderState.Aborting, true);
                                break;
                        }
                    }
                }
                else
                {
                    //상위에서 취소할경우
                    var unlockMissions = missions.Where(m => m.isLocked == false).ToList();
                    var cancelPossibleMission = unlockMissions.Where(m => m.state != nameof(MissionState.CANCELED)
                                                                       && m.state != nameof(MissionState.PENDING)
                                                                       && m.state != nameof(MissionState.COMMANDREQUESTCOMPLETED)
                                                                       && m.state != nameof(MissionState.EXECUTING)
                                                                       && m.state != nameof(MissionState.COMPLETED)).FirstOrDefault();

                    if (cancelPossibleMission == null)
                    {
                        //취소할 Mission이없을경우
                        CancelJob.terminateState = nameof(TerminateState.FAILED);
                        updateStateJob(CancelJob, CancelJob.state, true);
                    }
                    else
                    {
                        var cancelMissions = missions.Where(m => m.sequence >= cancelPossibleMission.sequence).ToList();

                        //취소할 Mission 이 있을경우
                        foreach (var cancelMission in cancelMissions)
                        {
                            updateStateMission(cancelMission, nameof(MissionState.CANCELED));
                        }

                        CancelJob.terminateState = nameof(TerminateState.EXECUTING);
                        CancelJob.terminatingAt = DateTime.Now;
                        updateStateJob(CancelJob, CancelJob.state, true);

                        var order = _repository.Orders.GetByid(CancelJob.orderId);
                        if (order != null)
                        {
                            switch (CancelJob.terminationType)
                            {
                                case nameof(TerminateType.CANCEL):
                                    updateStateOrder(order, OrderState.Canceling, true);
                                    break;

                                case nameof(TerminateType.ABORT):
                                    updateStateOrder(order, OrderState.Aborting, true);
                                    break;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 미션 전송 제어
        /// </summary>
        private void postMissionControl()
        {
            //[조회] 배터리 Setting 정보
            var batterySetting = _repository.Battery.GetAll();

            //[조회] 작업이 가능한 Worker
            foreach (var worker in _repository.Workers.MiR_GetByActive()/*.Where(m => m.state == nameof(WorkerState.IDLE) && m.acsmissionId == null */)
            {
                //[초기화] 충전 파라메터
                Parameter ChargeEquest = null;

                //[조회] 현재 Worker에게 할당된 Mission
                var missions = _repository.Missions.GetByAssignedWorkerId(worker.id).ToList();
                if (missions == null || missions.Count == 0) continue;

                //[조회] Middlewares 정보
                var middleware = _repository.Middlewares.GetByWorkerId(worker.id);

                //[조회] 현재 진행중인 Mission
                var runmission = _repository.Missions.GetByRunMissions(missions).FirstOrDefault();

                //[업데이트] 재정렬 및 상태변경된 Mission
                missions = missionDispatcher(missions, runmission, worker);

                bool c1 = worker.isMiddleware == true;

                bool c2 = worker.state == nameof(WorkerState.IDLE) && runmission == null;

                //bool c3 = /*worker.state != nameof(WorkerState.IDLE) && */ChargeEquest != null && worker.batteryPercent > batterySetting.minimum;

                //if (c3)
                //{
                //    //충전중일경우
                //    deleteMission(runmission);
                //}
                if (c1 && c2)
                {
                    //[조건] 전송 실패시 재전송 또는 대기중인 미션전송
                    var mission = missions.Where(m => (m.state == nameof(MissionState.WAITING))
                                                 || (m.state == nameof(MissionState.FAILED))
                                                 || (m.state == nameof(MissionState.COMMANDREQUEST))
                                                    ).FirstOrDefault();
                    if (mission != null)
                    {
                        //[조건] 충전중일경우 Cancel 진행[구현 필요]

                        //[조건] 충전중이 아닐경우 Skipped 후 다른 미션 전송[구현 필요]

                        if (skipMission(mission, worker)) continue;

                        postMission(mission);
                    }
                }
            }
        }

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
                                EventLogger.Info($"PostMission SKIPPED = Service = {nameof(Service.WORKER)}, PositionId = {worker.PositionId}, PositionName = {worker.PositionName}, MissionId = {mission.guid}, AssignedWorkerId = {mission.assignedWorkerId}");
                                completed = true;
                            }
                        }
                    }
                    break;

                case nameof(MissionType.ACTION):
                    if (mission.subType == nameof(MissionSubType.DOORCLOSE))
                    {
                        var elevatorMoveMissions = _repository.Missions.GetAll().Where(r => r.subType == nameof(MissionSubType.ELEVATORENTERMOVE)
                                                                                        || r.subType == nameof(MissionSubType.ELEVATOREXITMOVE)
                                                                                        || r.subType == nameof(MissionSubType.SWITCHINGMAP)).ToList();
                        var runmission = _repository.Missions.GetByRunMissions(elevatorMoveMissions).FirstOrDefault();

                        if (runmission != null)
                        {
                            updateStateMission(mission, nameof(MissionState.SKIPPED), true);
                            EventLogger.Info($"PostMission SKIPPED = Service = {nameof(Service.ELEVATOR)}, MissionId = {mission.guid}, missionName = {mission.name} ,AssignedWorkerId = {mission.assignedWorkerId}");
                            completed = true;
                        }
                    }
                    break;
            }

            return completed;
        }

        private bool workerElevatorParameterMapping(Mission mission)
        {
            bool completed = false;

            var workers = _repository.Workers.MiR_GetByActive();
            //현재 워커의 정보를 조회한다
            var assignedWorker = workers.FirstOrDefault(r => r.id == mission.assignedWorkerId);
            if (assignedWorker == null) return completed;

            //현재 워커와 다른 워커를 조회한다
            var anotherWorkers = workers.Where(r => r.id != assignedWorker.id).ToList();

            switch (mission.subType)
            {
                case nameof(MissionSubType.ELEVATORWAITMOVE):
                    //엘리베이터 대기위치 이고 점유중인 포지션이 아니고 현재워커와 맵아이디가 일치하는 것을 가지고온다.
                    var elevatorWaitpositions = _repository.Positions.MiR_GetBySubType(nameof(PositionSubType.ELEVATORWAIT));
                    var waitPosition = elevatorWaitpositions.FirstOrDefault(r => r.mapId == assignedWorker.mapId);

                    //var waitPositionNotOccupieds = _repository.Positions.MiR_GetNotOccupied(null, nameof(PositionSubType.ELEVATORWAIT));
                    //var waitPosition = waitPositionNotOccupieds.FirstOrDefault(r => r.mapId == assignedWorker.mapId);

                    var waitparam = mission.parameters.FirstOrDefault(r => r.key == "target");

                    if (waitPosition != null && waitparam.value == null)
                    {
                        waitparam.value = waitPosition.id;
                        completed = true;
                    }

                    break;

                case nameof(MissionSubType.ELEVATORENTERMOVE):
                    var enter1IsOccupied = _repository.Positions.MiR_GetIsOccupied(null, nameof(PositionSubType.ELEVATORENTER1)).FirstOrDefault();
                    var enter2IsOccupied = _repository.Positions.MiR_GetIsOccupied(null, nameof(PositionSubType.ELEVATORENTER2)).FirstOrDefault();

                    if (enter1IsOccupied == null)
                    {
                        var enter1Positions = _repository.Positions.MiR_GetNotOccupied(null, nameof(PositionSubType.ELEVATORENTER1));
                        if (enter1Positions == null || enter1Positions.Count == 0) break;

                        completed = elevatorEnterParameterMapping(enter1Positions, mission, assignedWorker);
                        if (completed)
                        {
                            completed = switchingMapParameterMapping(enter1Positions, mission);
                        }
                    }
                    else if (enter2IsOccupied == null)
                    {
                        var enter2Positions = _repository.Positions.MiR_GetNotOccupied(null, nameof(PositionSubType.ELEVATORENTER2));
                        if (enter2Positions == null || enter2Positions.Count == 0) break;

                        completed = elevatorEnterParameterMapping(enter2Positions, mission, assignedWorker);
                        if (completed)
                        {
                            completed = switchingMapParameterMapping(enter2Positions, mission);
                        }
                    }
                    break;

                case nameof(MissionSubType.ELEVATOREXITMOVE):
                    var elevatorExitpositions = _repository.Positions.MiR_GetBySubType(nameof(PositionSubType.ELEVATOREXIT));
                    var exitPosition = elevatorExitpositions.FirstOrDefault(r => r.mapId == assignedWorker.mapId);

                    //var waitPositionNotOccupieds = _repository.Positions.MiR_GetNotOccupied(null, nameof(PositionSubType.ELEVATOREXIT));
                    //var waitPosition = waitPositionNotOccupieds.FirstOrDefault(r => r.mapId == assignedWorker.mapId);

                    var exitparam = mission.parameters.FirstOrDefault(r => r.key == "target");

                    if (exitPosition != null && exitparam.value == null)
                    {
                        exitparam.value = exitPosition.id;
                        completed = true;
                    }

                    break;

                default:
                    completed = true;
                    break;
            }
            return completed;
        }

        private bool elevatorEnterParameterMapping(List<Position> positions, Mission mission, Worker worker)
        {
            bool completed = false;
            var enterPosition = positions.FirstOrDefault(r => r.mapId == worker.mapId);
            if (enterPosition != null)
            {
                //이동 포지션 적용
                var param = mission.parameters.FirstOrDefault(r => r.key == "target");
                if (param.value == null)
                {
                    param.value = enterPosition.id;
                    mission.parametersJson = JsonSerializer.Serialize(mission.parameters);
                    _repository.Missions.Update(mission);
                    completed = true;
                }
            }

            return completed;
        }

        private bool switchingMapParameterMapping(List<Position> positions, Mission mission)
        {
            bool completed = false;

            //Map스위칭 포지션 적용
            var job = _repository.Jobs.GetByid(mission.jobId);
            if (job != null)
            {
                var position = _repository.Positions.MiR_GetById(job.destinationId);
                var switchMapMission = _repository.Missions.GetByJobId(job.guid).FirstOrDefault(r => r.subType == nameof(MissionSubType.SWITCHINGMAP));
                if (position != null && switchMapMission != null)
                {
                    var mapSwitchPosition = positions.FirstOrDefault(r => r.mapId == position.mapId);
                    if (mapSwitchPosition != null)
                    {
                        var mapSwitchParam = switchMapMission.parameters.FirstOrDefault(p => p.key == "target");
                        if (mapSwitchParam.value == null)
                        {
                            mapSwitchParam.value = mapSwitchPosition.id;
                            switchMapMission.parametersJson = JsonSerializer.Serialize(switchMapMission.parameters);
                            _repository.Missions.Update(switchMapMission);
                            completed = true;
                        }
                    }
                }
            }
            return completed;
        }

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

                    break;

                case nameof(Service.MIDDLEWARE):
                    //미들웨어 전송 API로
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
                    break;

                case nameof(Service.ELEVATOR):
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
                            }
                            else EventLogger.Info($"DeleteMission Failed = Service = {nameof(Service.WORKER)}, Message = {postmission.message}, MissionId = {mission.guid}, AssignedWorkerId = {mission.assignedWorkerId}");
                        }
                    }

                    break;

                case nameof(Service.MIDDLEWARE):

                    break;

                case nameof(Service.ELEVATOR):

                    break;
            }
        }

        /// <summary>
        /// [Sub] postMissionControl
        /// 새로운 미션 시퀀스 재배치
        /// </summary>
        /// <param name="missions"></param>
        /// <param name="runmission"></param>
        /// <param name="worker"></param>
        /// <returns>Worker 기준 Missions 목록 반환</returns>
        private List<Mission> missionDispatcher(List<Mission> missions, Mission runmission, Worker worker)
        {
            //[초기화] Return Mission
            List<Mission> _missions = null;

            //[조회] 현재 Mission 상태중 WORKERASSIGNED 일치하는 Mission
            var newmissions = missions.Where(m => m.state == nameof(MissionState.WORKERASSIGNED)).ToList();
            if (newmissions != null && newmissions.Count > 0)
            {
                //[조회] 현재 Mission 중 WAITING 상태의 맨마지막 미션
                var mission = missions.LastOrDefault(m => m.state == nameof(MissionState.WAITING));
                if (mission == null && runmission == null)
                {
                    foreach (var Initmission in newmissions)
                    {
                        //[업데이트] 미션상태 를 WAITING 으로 변경
                        updateStateMission(Initmission, nameof(MissionState.WAITING), true);
                    }
                }
                else
                {
                    //[업데이트] 실행중인 미션이 있는경우 , 시퀀스 번호 재정렬
                    int seq = 0;
                    if (runmission != null) seq = runmission.sequence;
                    if (mission != null) seq = mission.sequence;

                    //[업데이트] 신규 미션들을 순차적으로 시퀀스 및 상태를 갱신
                    foreach (var Initmission in newmissions)
                    {
                        seq = seq + 1;
                        Initmission.sequence = seq;
                        Initmission.sequenceChangeCount = Initmission.sequenceChangeCount + 1;
                        Initmission.sequenceUpdatedAt = DateTime.Now;

                        //[업데이트] 미션상태 를 WAITING 으로 변경
                        updateStateMission(Initmission, nameof(MissionState.WAITING), true);
                    }
                }
            }
            //[조회] Worker 기준 Mission 목록 반환(시퀀스 순으로 정렬한다)
            _missions = _repository.Missions.GetByAssignedWorkerId(worker.id).OrderBy(s => s.sequence).ToList();
            return _missions;
        }
    }
}