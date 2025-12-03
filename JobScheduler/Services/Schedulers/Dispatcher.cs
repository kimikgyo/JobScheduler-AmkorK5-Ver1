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
            cancelControl();
            ElevatorModeChange();
        }

        /// <summary>
        /// Cancel 제어
        /// </summary>
        private void cancelControl()
        {
            var jobs = _repository.Jobs.GetAll();

            terminateState_Null(jobs);
            terminateState_Inited(jobs);
            terminateState_Executing(jobs);
        }

        private void terminateState_Executing(List<Job> jobs)
        {
            var terminateState_EXECUTING_Jobs = jobs.Where(j => j.terminateState == nameof(TerminateState.EXECUTING)).ToList();
            if (terminateState_EXECUTING_Jobs == null || terminateState_EXECUTING_Jobs.Count == 0) return;

            foreach (var terminateState_EXECUTING_Job in terminateState_EXECUTING_Jobs)
            {
                var missions = _repository.Missions.GetByJobId(terminateState_EXECUTING_Job.guid);

                var unlockMissions = missions.Where(m => m.isLocked == false).ToList();
                var executingMission = unlockMissions.FirstOrDefault(m => (m.state == nameof(MissionState.PENDING))
                                                            || (m.state == nameof(MissionState.EXECUTING))
                                                            || (m.state == nameof(MissionState.COMMANDREQUESTCOMPLETED)));

                if (executingMission != null)
                {
                    deleteMission(executingMission);
                }
            }
        }

        private void terminateState_Null(List<Job> jobs)
        {
            var terminateState_Null_jobs = jobs.Where(j => j.terminateState == null).ToList();
            if (terminateState_Null_jobs == null || terminateState_Null_jobs.Count == 0) return;

            foreach (var terminateState_Null_Job in terminateState_Null_jobs)
            {
                var Cancelmission = _repository.Missions.GetByJobId(terminateState_Null_Job.guid).Where(r => r.state == nameof(MissionState.CANCELED)).FirstOrDefault();
                if (Cancelmission != null)
                {
                    terminateState_Null_Job.terminationType = nameof(TerminateType.CANCEL);
                    terminateState_Null_Job.terminateState = nameof(TerminateState.INITED);
                    terminateState_Null_Job.terminatingAt = DateTime.Now;
                    terminateState_Null_Job.terminator = "JobScheduler";
                    updateStateJob(terminateState_Null_Job, terminateState_Null_Job.state, true);
                }
            }
        }

        //누군가에 Cancel 을 전달 받은경우.
        private void terminateState_Inited(List<Job> jobs)
        {
            var terminateState_INITED_Jobs = jobs.Where(j => j.terminateState == nameof(TerminateState.INITED)).ToList();
            if (terminateState_INITED_Jobs == null || terminateState_INITED_Jobs.Count == 0) return;

            foreach (var terminateState_INITED_Job in terminateState_INITED_Jobs)
            {
                var missions = _repository.Missions.GetByJobId(terminateState_INITED_Job.guid);
                if (terminateState_INITED_Job.terminator.ToUpper() == "INATECH")
                {
                    //관리자가 취소할경우
                    var cancelMissions = missions.Where(m => m.state != nameof(MissionState.COMPLETED)).ToList();
                    foreach (var cancelMission in cancelMissions)
                    {
                        updateStateMission(cancelMission, nameof(MissionState.CANCELED));
                    }

                    terminateState_INITED_Job.terminateState = nameof(TerminateState.EXECUTING);
                    terminateState_INITED_Job.terminatingAt = DateTime.Now;
                    updateStateJob(terminateState_INITED_Job, terminateState_INITED_Job.state, true);

                    var order = _repository.Orders.GetByid(terminateState_INITED_Job.orderId);
                    if (order != null)
                    {
                        switch (terminateState_INITED_Job.terminationType)
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
                    // 상위에서 취소할경우 unlock 일경우
                    var unlockMissions = missions.Where(m => m.isLocked == false).ToList();

                    // 미션을 전송완료 하거나 진행중인것이 아닌 미션
                    var cancelPossibleMission = unlockMissions.Where(m => m.state != nameof(MissionState.PENDING)
                                                                       && m.state != nameof(MissionState.COMMANDREQUESTCOMPLETED)
                                                                       && m.state != nameof(MissionState.EXECUTING)
                                                                        && m.state != nameof(MissionState.COMPLETED)
                                                                       ).FirstOrDefault();

                    if (cancelPossibleMission == null)
                    {
                        //취소할 Mission이없을경우
                        terminateState_INITED_Job.terminateState = nameof(TerminateState.FAILED);
                        updateStateJob(terminateState_INITED_Job, terminateState_INITED_Job.state, true);
                    }
                    else
                    {
                        var cancelMissions = missions.Where(m => m.sequence >= cancelPossibleMission.sequence).ToList();

                        //취소할 Mission 이 있을경우
                        foreach (var cancelMission in cancelMissions)
                        {
                            updateStateMission(cancelMission, nameof(MissionState.CANCELED));
                        }

                        terminateState_INITED_Job.terminateState = nameof(TerminateState.EXECUTING);
                        terminateState_INITED_Job.terminatingAt = DateTime.Now;
                        updateStateJob(terminateState_INITED_Job, terminateState_INITED_Job.state, true);

                        var order = _repository.Orders.GetByid(terminateState_INITED_Job.orderId);
                        if (order != null)
                        {
                            switch (terminateState_INITED_Job.terminationType)
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
                if (param.value == null)
                {
                    param.value = Position.id;
                    mission.parametersJson = JsonSerializer.Serialize(mission.parameters);
                    _repository.Missions.Update(mission);
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
                        if (completed == true)
                        {
                            //직접 파라메타를 변경하는것이기때문에 포지션점유를 업데이트한다
                            updateOccupied(mapSwitchPosition, true);
                        }
                    }
                }
            }

            return completed;
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