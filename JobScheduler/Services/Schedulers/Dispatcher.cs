using Common.Models.Bases;
using Common.Models.Jobs;
using Common.Models.Settings;
using Common.Templates;
using System.Text.Json;

namespace JOB.Services
{
    public partial class SchedulerService
    {
        private void Dispatcher()
        {
            carrierRemoveContorl();
            workerAssignedControl();
            postMissionControl();
            cancelControl();
        }

        /// <summary>
        /// 케리어 삭제 제어
        /// </summary>
        private void carrierRemoveContorl()
        {
            var carriers = _repository.Carriers.GetAll();
            foreach (var carrier in carriers)
            {
                if (IsInvalid(carrier.workerId))
                {
                    _repository.Carriers.Remove(carrier);
                }
            }
        }

        /// <summary>
        /// 워커 할당 제어
        /// </summary>
        private void workerAssignedControl()
        {
            //신규 Job 이있는지 확인
            var UnAssignedWorkerJobs = _repository.Jobs.GetByInit().Where(j => j.terminationType == null && string.IsNullOrWhiteSpace(j.assignedWorkerId)).ToList();
            UnAssignedWorkerJobs = UnAssignedWorkerJobs.OrderByDescending(r => r.priority).ToList();
            UnAssignedWorkerJobs = UnAssignedWorkerJobs.OrderBy(r => r.createdAt).ToList();
            if (UnAssignedWorkerJobs == null || UnAssignedWorkerJobs.Count == 0) return;
            var batterySetting = _repository.Battery.GetAll();
            if (batterySetting == null) return;

            //순차적용
            firstJob(UnAssignedWorkerJobs, batterySetting);
            //거리순 적용
            //distance(UnAssignedWorkerJobs, batterySetting);
        }

        /// <summary>
        /// [Sub] AssignedControl
        /// Job이 들어오는 순서대로 워커할당
        /// </summary>
        /// <param name="UnAssignedWorkerJobs"></param>
        /// <param name="batterySetting"></param>
        private void firstJob(List<Job> UnAssignedWorkerJobs, Battery batterySetting)
        {
            // [일단!order1개씩처리]
            // 1.Job 받을수있는 워커를 확인한다.
            // 1-1 통신연결 되어있고 Error 상태가 아니고 엑티브 활성화 되어있는지

            foreach (var worker in _repository.Workers.MiR_GetByActive())
            {
                Job job = null;
                var runjob = _repository.Jobs.GetByAssignWorkerId(worker.id).FirstOrDefault();
                if (runjob != null) continue;
                //그룹과 일치하는 Job이있는지
                UnAssignedWorkerJobs = UnAssignedWorkerJobs.Where(u => u.group == worker.group).ToList();

                //지정한 워커가 있는지
                job = UnAssignedWorkerJobs.FirstOrDefault(j => j.specifiedWorkerId == worker.id);

                if (job == null)
                {
                    job = UnAssignedWorkerJobs.FirstOrDefault(j => IsInvalid(j.specifiedWorkerId));
                }

                if (job != null)
                {
                    if (TransPortCondition(job, worker) == false) continue;
                    //orderId 가 있는경우는 외부에서 Order 를 받은것이기때문에 minimum 배터리 이하면 전송하지않는다.
                    if (job.orderId != null && worker.batteryPercent < batterySetting.minimum) continue;

                    job.assignedWorkerId = worker.id;
                    updateStateJob(job, nameof(JobState.WORKERASSIGNED), true);
                    foreach (var mission in _repository.Missions.GetByJobId(job.guid))
                    {
                        mission.assignedWorkerId = worker.id;
                        updateStateMission(mission, nameof(MissionState.WORKERASSIGNED), true);
                    }
                    var order = _repository.Orders.GetByid(job.orderId);
                    if (order != null)
                    {
                        order.assignedWorkerId = worker.id;
                        updateStateOrder(order, OrderState.Transferring, true);
                    }
                }
            }
        }

        /// <summary>
        /// [Sub] AssignedControl
        /// 거리순서 워커 할당
        /// </summary>
        /// <param name="UnAssignedWorkerJobs"></param>
        /// <param name="batterySetting"></param>
        private void distance(List<Job> UnAssignedWorkerJobs, Battery batterySetting)
        {
            var workers = _repository.Workers.MiR_GetByActive();
            if (workers.Count == 0 || workers == null) return;
            var idleWorkers = workers.Where(r => r.state == nameof(WorkerState.IDLE) && r.batteryPercent > batterySetting.minimum).ToList();
            if (idleWorkers == null || idleWorkers.Count == 0) return;
            var findSpecifiedWorkerjobs = UnAssignedWorkerJobs.Where(j => IsInvalid(j.specifiedWorkerId) == false).ToList();
            var findNotspecifiedWorkerjobs = UnAssignedWorkerJobs.Where(j => IsInvalid(j.specifiedWorkerId) == true).ToList();

            foreach (var runjob in _repository.Jobs.GetAll().Where(r => r.assignedWorkerId != null))
            {
                // [조회] 워커가 실행중인 Job
                var runjobWorker = workers.FirstOrDefault(r => r.id == runjob.assignedWorkerId);
                // [조건] Job실행중인것이 있고 충전이 아닌것!
                if (runjobWorker != null && runjob.type != nameof(JobType.CHARGE)) workers.Remove(runjobWorker);
            }
            if (findSpecifiedWorkerjobs.Count > 0)
            {
                List<Worker> _assignedWorkers = new List<Worker>();

                //지정 보낼경우 워커 기준으로 가까운 Job을 판단
                foreach (var worker in workers)
                {
                    Job job = null;
                    var specifiedWorkerjobs = findSpecifiedWorkerjobs.Where(r => r.specifiedWorkerId == worker.id).ToList();
                    if (specifiedWorkerjobs.Count == 0 || specifiedWorkerjobs == null) continue;

                    job = specifiedJobSelect(worker, specifiedWorkerjobs);
                    if (job != null)
                    {
                        if (TransPortCondition(job, worker) == false) continue;

                        job.assignedWorkerId = worker.id;
                        updateStateJob(job, nameof(JobState.WORKERASSIGNED), true);
                        foreach (var mission in _repository.Missions.GetByJobId(job.guid))
                        {
                            mission.assignedWorkerId = worker.id;
                            updateStateMission(mission, nameof(MissionState.WORKERASSIGNED), true);
                        }
                        var order = _repository.Orders.GetByid(job.orderId);
                        if (order != null)
                        {
                            order.assignedWorkerId = worker.id;
                            updateStateOrder(order, OrderState.Transferring, true);
                        }
                        _assignedWorkers.Add(worker);
                    }
                }
                foreach (var _assignedWorker in _assignedWorkers)
                {
                    workers.Remove(_assignedWorker);
                }
            }

            if (findNotspecifiedWorkerjobs.Count > 0)
            {
                //지정 워커가 없을경우 Job기준 거리 판단 Worker Assigned
                foreach (var job in findNotspecifiedWorkerjobs)
                {
                    workers = workers.Where(w => w.group == job.group).ToList();
                    if (workers.Count == 0 || workers == null) continue;

                    var worker = NotspecifiedJobSelect(workers, job);
                    if (worker != null)
                    {
                        if (TransPortCondition(job, worker) == false) continue;

                        job.assignedWorkerId = worker.id;
                        updateStateJob(job, nameof(JobState.WORKERASSIGNED), true);

                        foreach (var mission in _repository.Missions.GetByJobId(job.guid))
                        {
                            mission.assignedWorkerId = worker.id;
                            updateStateMission(mission, nameof(MissionState.WORKERASSIGNED), true);
                        }
                        var order = _repository.Orders.GetByid(job.orderId);
                        if (order != null)
                        {
                            order.assignedWorkerId = worker.id;
                            updateStateOrder(order, OrderState.Transferring, true);
                        }
                        workers.Remove(worker);
                    }
                }
            }
        }

        private bool TransPortCondition(Job job, Worker worker)
        {
            bool Condition = true;
            switch (job.type)
            {
                case nameof(JobType.TRANSPORT):
                case nameof(JobType.TRANSPORTCHEMICALSUPPLY):
                case nameof(JobType.TRANSPORTCHEMICALRECOVERY):
                case nameof(JobType.TRANSPORTSLURRYSUPPLY):
                case nameof(JobType.TRANSPORTSLURRYRECOVERY):

                    //미들웨어가 사용중
                    if (worker.isMiddleware == true)
                    {
                        var middleware = _repository.Middlewares.GetByWorkerId(worker.id);
                        if (middleware != null)
                        {
                            //대기 상태일때만 Job이가능
                            if (middleware.state != nameof(MiddlewareState.IDLE)) Condition = false;

                            var carrier = _repository.Carriers.GetByWorkerId(worker.id).FirstOrDefault();

                            //자재가 있을경우에만 Job 이가능
                            if (job.subType == nameof(JobSubType.DROPONLY) && carrier != null) Condition = false;

                            //자재가 없을경우에만 Job 이가능
                            else if (carrier != null) Condition = false;
                        }
                        else Condition = false;
                    }
                    break;
            }
            return Condition;
        }

        /// <summary>
        /// [Sub] AssignedControl
        /// 지정이 아닌 특정조건으로 인하여 Job 전택
        /// </summary>
        /// <param name="workers"></param>
        /// <param name="job"></param>
        /// <returns></returns>
        private Worker NotspecifiedJobSelect(List<Worker> workers, Job job)
        {
            Position position = null;
            Worker woekrSelect = null;
            if (IsInvalid(job.sourceId))
            {
                position = _repository.Positions.MiR_GetById(job.destinationId);
            }
            else
            {
                position = _repository.Positions.MiR_GetById(job.sourceId);
            }

            if (position != null)
            {
                //가까운 거리순으로 판단하여 1개의 포지션을 가지고온다
                var nearestWorker = _repository.Workers.FindNearestWorker(workers, position).FirstOrDefault();
                if (nearestWorker != null)
                {
                    woekrSelect = nearestWorker;
                }
            }
            return woekrSelect;
        }

        /// <summary>
        /// [Sub] AssignedControl
        /// 지정 워커 Job 선택
        /// </summary>
        /// <param name="worker"></param>
        /// <param name="jobs"></param>
        /// <returns></returns>
        private Job specifiedJobSelect(Worker worker, List<Job> jobs)
        {
            Job jobSelect = null;
            List<Position> Positions = new List<Position>();

            foreach (var job in jobs)
            {
                if (IsInvalid(job.sourceId))
                {
                    var position = _repository.Positions.MiR_GetById(job.destinationId);
                    if (position != null) Positions.Add(position);
                }
                else
                {
                    var position = _repository.Positions.MiR_GetById(job.sourceId);
                    if (position != null) Positions.Add(position);
                }
            }
            //포지션값이 있다면
            if (Positions.Count > 0)
            {
                //가까운 거리순으로 판단하여 1개의 포지션을 가지고온다
                var nearestPosition = _repository.Positions.FindNearestWayPoint(worker, Positions).FirstOrDefault();
                if (nearestPosition != null)
                {
                    //1개의 포지션으로 Job을 조회한다.
                    jobSelect = jobs.FirstOrDefault(j => j.sourceId == nearestPosition.id);
                    if (jobSelect == null) jobSelect = jobs.FirstOrDefault(j => j.destinationId == nearestPosition.id);
                }
            }
            return jobSelect;
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
                if (CancelJob.terminator.ToUpper() == "INATECH")
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
                Parameta ChargeEquest = null;

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

                //bool c2 = worker.state == nameof(WorkerState.IDLE) && runmission == null;
                bool c2 = worker.state == "RUNNING" && runmission == null;

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