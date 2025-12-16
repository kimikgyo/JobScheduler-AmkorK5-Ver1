using Common.Models.Bases;
using Common.Models.Jobs;
using System.Text.Json;

namespace JOB.Services
{
    public partial class SchedulerService
    {
        private void Dispatcher()
        {
            postMissionControl();
            ElevatorModeChange();
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
                var missions = _repository.Missions.GetByAssignedWorkerId(worker.id).OrderBy(r=>r.sequence).ToList();
                if (missions == null || missions.Count == 0) continue;

                //[조회] Middlewares 정보
                var middleware = _repository.Middlewares.GetByWorkerId(worker.id);

                //[조회] 현재 진행중인 Mission
                var runmission = _repository.Missions.GetByRunMissions(missions).FirstOrDefault();


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
                                EventLogger.Info($"[PostMission][{nameof(Service.WORKER)}][SKIPPED], PositionId = {worker.PositionId}, PositionName = {worker.PositionName}, MissionId = {mission.guid}, AssignedWorkerId = {mission.assignedWorkerId}");
                                completed = true;
                            }
                        }
                    }
                    break;

                case nameof(MissionType.ACTION):
                    if (mission.subType == nameof(MissionSubType.DOORCLOSE))
                    {
                        var elevatorMoveMissions = _repository.Missions.GetAll().Where(r => r.subType == nameof(MissionSubType.ELEVATORWAITMOVE)
                                                                                        || r.subType == nameof(MissionSubType.ELEVATORENTERMOVE)
                                                                                        || r.subType == nameof(MissionSubType.ELEVATOREXITMOVE)
                                                                                        || r.subType == nameof(MissionSubType.RIGHTTURN)
                                                                                        || r.subType == nameof(MissionSubType.LEFTTURN)
                                                                                        || r.subType == nameof(MissionSubType.SWITCHINGMAP)).ToList();
                        var runmission = _repository.Missions.GetByRunMissions(elevatorMoveMissions).FirstOrDefault();

                        if (runmission != null)
                        {
                            updateStateMission(mission, nameof(MissionState.SKIPPED), true);
                            EventLogger.Info($"[PostMission][{nameof(Service.ELEVATOR)}][SKIPPED], MissionId = {mission.guid}, missionName = {mission.name} ,AssignedWorkerId = {mission.assignedWorkerId}");
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

                    //엘리베이터 대기위치 점유 상황을 판단하여 점유하고있지않은 포지션으로 전달한다.
                    var waitPositionNotOccupieds = _repository.Positions.MiR_GetNotOccupied(null, nameof(PositionSubType.ELEVATORWAIT));
                    if (waitPositionNotOccupieds == null || waitPositionNotOccupieds.Count == 0) break;

                    completed = elevatorParameterMapping(waitPositionNotOccupieds, mission, assignedWorker);

                    break;

                case nameof(MissionSubType.ELEVATORENTERMOVE):

                    var enterPositions = _repository.Positions.MiR_GetNotOccupied(null, nameof(PositionSubType.ELEVATORENTER));
                    if (enterPositions == null || enterPositions.Count == 0) break;

                    completed = elevatorParameterMapping(enterPositions, mission, assignedWorker);
                    if (completed)
                    {
                        completed = switchingMapParameterMapping(enterPositions, mission);
                    }
                    break;

                case nameof(MissionSubType.ELEVATOREXITMOVE):
                    var elevatorExitpositions = _repository.Positions.MiR_GetNotOccupied(null, nameof(PositionSubType.ELEVATOREXIT));
                    if (elevatorExitpositions == null || elevatorExitpositions.Count == 0) break;

                    completed = elevatorParameterMapping(elevatorExitpositions, mission, assignedWorker);

                    break;

                default:
                    completed = true;
                    break;
            }
            return completed;
        }

        private bool elevatorParameterMapping(List<Position> positions, Mission mission, Worker worker)
        {
            bool completed = false;
            var Position = positions.FirstOrDefault(r => r.mapId == worker.mapId);
            if (Position != null)
            {
                var param = mission.parameters.FirstOrDefault(r => r.key == "target");
                if (IsInvalid(param.value))
                {
                    param.value = Position.id;
                    mission.parametersJson = JsonSerializer.Serialize(mission.parameters);
                    _repository.Missions.Update(mission);
                    completed = true;
                }
                else
                {
                    completed = true;
                }
                if (completed == true)
                {
                    //직접 파라메타를 변경하는것이기때문에 포지션점유를 업데이트한다
                    updateOccupied(Position, true);
                }
            }

            return completed;
        }

        private bool switchingMapParameterMapping(List<Position> positions, Mission mission)
        {
            bool completed = false;

            Position sourcePosition = null;
            Position destPosition = null;
            Position mapSwitchPosition = null;
            //Map스위칭 포지션 적용
            var job = _repository.Jobs.GetByid(mission.jobId);
            if (job != null)
            {
                var missions = _repository.Missions.GetByJobId(job.guid);
                if (job.sourceId != null) sourcePosition = _repository.Positions.MiR_GetById(job.sourceId);

                //출발지 가 현재 Worker층과 다를경우
                var sourceMission = missions.Where(r => r.subType == nameof(MissionSubType.SOURCEMOVE) && r.state == nameof(MissionState.WAITING)).FirstOrDefault();
                if (sourceMission != null && sourcePosition != null)
                {
                    mapSwitchPosition = positions.FirstOrDefault(r => r.mapId == sourcePosition.mapId);
                }
                else
                {
                    //도착지층 과 다를경우
                    destPosition = _repository.Positions.MiR_GetById(job.destinationId);
                    var destMission = missions.Where(r => r.subType == nameof(MissionSubType.DESTINATIONMOVE) && r.state == nameof(MissionState.WAITING)).FirstOrDefault();
                    if (destMission != null && destPosition != null)
                    {
                        mapSwitchPosition = positions.FirstOrDefault(r => r.mapId == destPosition.mapId);
                    }
                }

                if (mapSwitchPosition != null)
                {
                    var switchMapMission = missions.FirstOrDefault(r => r.subType == nameof(MissionSubType.SWITCHINGMAP) && r.state == nameof(MissionState.WAITING));
                    var mapSwitchParam = switchMapMission.parameters.FirstOrDefault(p => p.key == "target");
                    if (IsInvalid(mapSwitchParam.value))
                    {
                        mapSwitchParam.value = mapSwitchPosition.id;
                        switchMapMission.parametersJson = JsonSerializer.Serialize(switchMapMission.parameters);
                        _repository.Missions.Update(switchMapMission);
                        //직접 파라메타를 변경하는것이기때문에 포지션점유를 업데이트한다
                        updateOccupied(mapSwitchPosition, true);
                    }
                    completed = true;
                }
            }

            return completed;
        }

  

        private void ElevatorModeChange()
        {
            bool CommandRequst = false;
            var missions = _repository.Missions.GetAll();
            if (missions == null || missions.Count() == 0) return;

            var mission = missions.FirstOrDefault(m => m.service == nameof(Service.ELEVATOR) && m.type == nameof(MissionType.ACTION) && m.subType == nameof(MissionSubType.MODECHANGE));
            if (mission != null && mission.state == nameof(MissionState.WAITING))
            {
                //엘리베이터 모드체인지 미션
                var Jobs = _repository.Jobs.GetAll();
                //JOb 중에 WITHEV 미션을 가지고 있고 워커가 지정되어 이 되어있지않을 경우 !!!
                var withEv_Job = Jobs.FirstOrDefault(r => r.subType.Contains("WITHEV") && !IsInvalid(r.assignedWorkerId));
                if (withEv_Job == null)
                {
                    CommandRequst = ElevatorPostMission(mission);

                    if (CommandRequst)
                    {
                        updateStateMission(mission, nameof(MissionState.COMMANDREQUESTCOMPLETED), true);
                    }
                }
            }
        }
    }
}