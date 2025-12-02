using Common.Models.Jobs;

namespace JOB.Services
{
    public partial class SchedulerService
    {
        private void JobPlanner()
        {
            createJob();

            //orderCreateJob();
            //createWaitControl();
            //createChargeControl();
        }

        //Job생성
        private void createJob()
        {
            Position source = null;
            Position destination = null;
            var Orders = _repository.Orders.GetByOrderStatus(nameof(OrderState.Queued));
            foreach (var Order in Orders)
            {
                var Job = _repository.Jobs.GetByOrderId(Order.id);
                if (Job != null) continue;

                if (IsInvalid(Order.sourceId))
                {
                    var worker = _repository.Workers.MiR_GetById(Order.specifiedWorkerId);
                    if (worker == null) continue;
                    var positions = _repository.Positions.MiR_GetByMapId(worker.mapId);
                    if (positions == null || positions.Count() == 0) continue;
                    //워커에서 가장 가까운 포지션을 출발지 포지션으로 설정한다.
                    source = _repository.Positions.FindNearestWayPoint(worker, positions).FirstOrDefault();
                    if (source == null) continue;
                    //목적지 조회.
                    destination = _repository.Positions.GetById(Order.destinationId);
                    if (destination == null) continue;

                    _Queue.Create_Job(worker.group, Order.id, Order.type, Order.subType, Order.carrierId, Order.priority, Order.drumKeyCode
                                     , source.id, source.name, source.linkedFacility, destination.id, destination.name, destination.linkedFacility
                                     , Order.specifiedWorkerId, Order.assignedWorkerId);
                }
                else
                {
                    //출발지 조회
                    source = _repository.Positions.MiR_GetById(Order.sourceId);
                    if (source == null) continue;
                    //목적지 조회.
                    destination = _repository.Positions.GetById(Order.destinationId);
                    if (destination == null) continue;

                    _Queue.Create_Job(source.group, Order.id, Order.type, Order.subType, Order.carrierId, Order.priority, Order.drumKeyCode
                           , source.id, source.name, source.linkedFacility, destination.id, destination.name, destination.linkedFacility
                           , Order.specifiedWorkerId, Order.assignedWorkerId);

                }
            }
        }
        private void Create_Mission()
        {
            var Jobs = _repository.Jobs.GetByInit();
            foreach (var job in Jobs)
            {
                
            }





        /*
        private void orderCreateJob()
        {
            var initStatusOrders = _repository.Orders.GetByOrderStatus(nameof(OrderState.Queued));

            foreach (var initStatusOrder in initStatusOrders)
            {
                var Job = _repository.Jobs.GetByOrderId(initStatusOrder.id, initStatusOrder.type, initStatusOrder.subType);
                if (Job != null) continue;

                bool reValue = createJob_Mission(initStatusOrder.type, initStatusOrder.subType
                                                , initStatusOrder.id, initStatusOrder.carrierId, initStatusOrder.priority, initStatusOrder.drumKeyCode
                                                , initStatusOrder.sourceId, initStatusOrder.destinationId, initStatusOrder.specifiedWorkerId, initStatusOrder.assignedWorkerId);

                if (reValue == false)
                {
                    //템플릿에 없는경우
                    initStatusOrder.state = nameof(OrderState.JobTemplateNotFind);
                    _Queue.Remove_Order(initStatusOrder, DateTime.Now);
                    EventLogger.Info($"JobTemplate NotFind, OrderId = {initStatusOrder.id}");
                }
            }
        }

        /// <summary>
        /// Job 생성
        /// Job템플릿에서 선택하여서 Job 생성
        /// </summary>
        private bool createJob_Mission(string jobType, string jobSubtype
                                     , string orderId, string carrierId, int priority, string drumKeyCode
                                     , string sourceId, string destinationId
                                     , string specifiedWorkerId, string assignedWorkerId)
        {
            bool reValue = false;
            Position source = null;
            Position destination = null;
            bool selectJobflag = false;
            JobTemplate selectJob = null;

            //JobTemplates 타입과 서브타입으로 조회한다
            var jobTemplates = _repository.JobTemplates.GeyByOrderType(jobType, jobSubtype);
            //JobTemplates 조회가 되지않으면 //템플릿에 없는경우;
            if (jobTemplates == null || jobTemplates.Count == 0) return reValue;

            //도착지 포지션 조회
            destination = _repository.Positions.MiR_GetById(destinationId);
            if (destination == null) return reValue;

            //orderType 빈문자를제외후 대문자로 변환
            string orderType = jobType;

            //orderSubType 빈문자를제외후 대문자로 변환
            string orderSubType = jobSubtype;

            switch (orderType)
            {
                case nameof(JobType.TRANSPORT):
                case nameof(JobType.TRANSPORTCHEMICALSUPPLY):
                case nameof(JobType.TRANSPORTCHEMICALRECOVERY):
                case nameof(JobType.TRANSPORTSLURRYSUPPLY):
                case nameof(JobType.TRANSPORTSLURRYRECOVERY):
                case nameof(JobType.MOVE):
                case nameof(JobType.CHARGE):
                case nameof(JobType.WAIT):
                case nameof(JobType.RESET):
                    if (IsInvalid(sourceId))
                    {
                        //지정 Worker 조회
                        var worker = _repository.Workers.MiR_GetById(specifiedWorkerId);
                        if (worker == null) break;
                        //Worker그룹과 JobTemplate 그룹을 비교하여 조회한다
                        jobTemplates = jobTemplates.Where(jt => jt.group == worker.group).ToList();
                        //조회한내용이없으면 continue
                        if (jobTemplates == null || jobTemplates.Count == 0) break;
                        if (worker.mapId != destination.mapId)
                        {
                            jobTemplates = jobTemplates.Where(m => m.subType == $"{jobSubtype}WITHEV").ToList();
                            if (jobTemplates == null || jobTemplates.Count == 0) break;
                        }
                    }
                    else
                    {
                        //출발지 조회
                        source = _repository.Positions.MiR_GetById(sourceId);
                        if (source == null) break;

                        //그룹 조회
                        jobTemplates = jobTemplates.Where(m => m.group == source.group).ToList();
                        if (jobTemplates == null || jobTemplates.Count == 0) break;

                        if (source.mapId != destination.mapId)
                        {
                            jobTemplates = jobTemplates.Where(m => m.subType == $"{jobSubtype}WITHEV").ToList();
                            if (jobTemplates == null || jobTemplates.Count == 0) break;
                        }
                    }
                    selectJob = selectJobTemplate(jobTemplates, source, destination);

                    break;
            }
            if (selectJob != null)
            {
                if (source == null)
                {
                    _Queue.CreateJobMission(selectJob, orderId, carrierId, priority, drumKeyCode
                                            , null, null, null
                                            , destination.id, destination.name, destination.linkedFacility
                                            , specifiedWorkerId, assignedWorkerId);
                    reValue = true;
                }
                else
                {
                    _Queue.CreateJobMission(selectJob, orderId, carrierId, priority, drumKeyCode
                                            , source.id, source.name, source.linkedFacility
                                            , destination.id, destination.name, destination.linkedFacility
                                            , specifiedWorkerId, assignedWorkerId);
                    reValue = true;
                }
            }
            return reValue;
        }

        private JobTemplate selectJobTemplate(List<JobTemplate> jobTemplates, Position source, Position destination)
        {
            JobTemplate jobTemplate = null;

            var jobTemplateCancelAndReset = jobTemplates.FirstOrDefault(j => j.type == nameof(JobType.RESET));
            if (jobTemplateCancelAndReset != null)
            {
                jobTemplate = jobTemplateCancelAndReset;
            }
            else
            {
                //출발지가 Null인경우
                if (source == null)
                {
                    foreach (var job in jobTemplates)
                    {
                        //missionTemplate 중에 타입이 Move이고 서브타입이 충전Move이거나 WaitMove 이거나 ResetMove이거나 PositionMove이거나 DestinationMove 일때
                        var moveMissionTempleates = job.missionTemplates.Where(m => m.type == nameof(MissionType.MOVE)
                                                                        && (m.subType == nameof(MissionSubType.CHARGERMOVE)
                                                                            || m.subType == nameof(MissionSubType.WAITMOVE)
                                                                            || m.subType == nameof(MissionSubType.RESETMOVE)
                                                                            || m.subType == nameof(MissionSubType.POSITIONMOVE)
                                                                            || m.subType == nameof(MissionSubType.DESTINATIONMOVE))).ToList();

                        var parma = _repository.MissionTemplates.GetParametas(moveMissionTempleates).FirstOrDefault(p => p.key == "target" && p.value == destination.id);
                        if (parma != null)
                        {
                            jobTemplate = job;
                            break;
                        }
                    }

                    //위 파라메타가 목적지 id와 같은게 없는경우 파라메타가 null인것을 확인한다.
                    if (jobTemplate == null)
                    {
                        foreach (var job in jobTemplates)
                        {
                            var moveMissionTempleates = job.missionTemplates.Where(m => m.type == nameof(MissionType.MOVE)
                                                                        && (m.subType == nameof(MissionSubType.CHARGERMOVE)
                                                                            || m.subType == nameof(MissionSubType.WAITMOVE)
                                                                            || m.subType == nameof(MissionSubType.RESETMOVE)
                                                                            || m.subType == nameof(MissionSubType.POSITIONMOVE)
                                                                            || m.subType == nameof(MissionSubType.DESTINATIONMOVE))).ToList();

                            var parma = _repository.MissionTemplates.GetParametas(moveMissionTempleates).FirstOrDefault(p => p.key == "target" && p.value == null);
                            if (parma != null)
                            {
                                jobTemplate = job;
                                break;
                            }
                        }
                    }
                }
                else
                {
                    foreach (var job in jobTemplates)
                    {
                        var missionSource = job.missionTemplates.FirstOrDefault(s => s.type == nameof(MissionType.MOVE) && s.subType == nameof(MissionSubType.SOURCEMOVE));
                        var missionDest = job.missionTemplates.FirstOrDefault(s => s.type == nameof(MissionType.MOVE) && s.subType == nameof(MissionSubType.DESTINATIONMOVE));
                        if (missionSource != null && missionDest != null)
                        {
                            var paramSource = missionSource.parameters.FirstOrDefault(p => p.key == "target" && p.value == source.id);
                            var paramDest = missionDest.parameters.FirstOrDefault(p => p.key == "target" && p.value == destination.id);
                            if (paramSource != null && paramDest != null)
                            {
                                jobTemplate = job;
                                break;
                            }
                        }
                    }

                    if (jobTemplate == null)
                    {
                        foreach (var job in jobTemplates)
                        {
                            var missionSource = job.missionTemplates.FirstOrDefault(s => s.type == nameof(MissionType.MOVE) && s.subType == nameof(MissionSubType.SOURCEMOVE));
                            var missionDest = job.missionTemplates.FirstOrDefault(s => s.type == nameof(MissionType.MOVE) && s.subType == nameof(MissionSubType.DESTINATIONMOVE));
                            if (missionSource != null && missionDest != null)
                            {
                                var paramSource = missionSource.parameters.FirstOrDefault(p => p.key == "target" && p.value == null);
                                var paramDest = missionDest.parameters.FirstOrDefault(p => p.key == "target" && p.value == null);
                                if (paramSource != null && paramDest != null)
                                {
                                    jobTemplate = job;
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            return jobTemplate;
        }

        /// <summary>
        /// 충전 Job 생성
        /// </summary>
        private void createChargeControl()
        {
            var workers = _repository.Workers.MiR_GetByActive();    //worker 정보
            var batterySetting = _repository.Battery.GetAll();      //배터리 정보
            if (workers == null || workers.Count == 0) return;
            if (batterySetting == null) return;
            List<Position> selectPositions = new List<Position>();

            foreach (var worker in workers)
            {
                Position DestPosition = null;
                Position CrossPosition = null;
                Worker crossWorker = null;

                var jobFindAssignedWorker = _repository.Jobs.GetAll().FirstOrDefault(j => j.assignedWorkerId == worker.id || j.specifiedWorkerId == worker.id);
                //Job 이있으면 다음 Worker 할당
                if (jobFindAssignedWorker != null) continue;

                //해당worker가 충전시작 배터리 인지?
                if (worker.state == nameof(WorkerState.IDLE) && worker.batteryPercent < batterySetting.chargeStart)
                {
                    //충전기 점유하고 있지 않은 포지션을 확인
                    var NotOccupiedPositions = _repository.Positions.MiR_GetAll().Where(p => p.hasCharger == true && p.isOccupied == false).ToList();

                    //전부 점유 중일경우
                    if (NotOccupiedPositions == null || NotOccupiedPositions.Count == 0)
                    {
                        //크로스 충전이 가능한 workers를 확인한다
                        var crossWorkers = workers.Where(w => w.batteryPercent > batterySetting.chargeStart && w.batteryPercent > batterySetting.crossCharge).ToList();
                        if (crossWorkers == null || crossWorkers.Count == 0) continue;
                        //충전기 점유 하고 있는 포지션을 확인
                        var OccupiedPositions = _repository.Positions.MiR_GetAll().Where(p => p.hasCharger == true && p.isOccupied == true).ToList();
                        // 크로스가능한 워커가 있는 포지션을 확인
                        var crossChargePosition = OccupiedPositions.Where(w => crossWorkers.Select(c => c.PositionId).Contains(w.id)).ToList();

                        //충전 위치를 검색한다
                        DestPosition = _repository.Positions.FindNearestWayPoint(worker, crossChargePosition).FirstOrDefault();

                        if (DestPosition != null)
                        {
                            //크로스할 Worker를 검색한다
                            crossWorker = crossWorkers.FirstOrDefault(r => r.PositionId == DestPosition.id);
                            if (crossWorker != null)
                            {
                                //해당 워커 위치가 대기 위치인지 확인
                                var waitPositionOccupieds = _repository.Positions.MiR_GetIsOccupied(null, nameof(PositionSubType.WAIT));
                                var waitPositionOccupied = waitPositionOccupieds.FirstOrDefault(w => w.id == worker.PositionId);

                                if (waitPositionOccupied != null)
                                {
                                    CrossPosition = waitPositionOccupied;
                                }
                                else
                                {
                                    //점유하고 있지않은 대기위치를 선택하여 미션을 보낸다
                                    var notOccupiedPositions = _repository.Positions.MiR_GetAll().Where(c => c.subType == nameof(PositionSubType.WAIT) && c.isOccupied == false).ToList();
                                    if (notOccupiedPositions == null || notOccupiedPositions.Count == 0) continue;
                                    CrossPosition = _repository.Positions.FindNearestWayPoint(crossWorker, notOccupiedPositions).FirstOrDefault();
                                }
                            }
                        }
                    }
                    //점유중이지 않은 포지션이 있을경우
                    else
                    {
                        //충전 미션 전송
                        DestPosition = _repository.Positions.FindNearestWayPoint(worker, NotOccupiedPositions).FirstOrDefault();
                    }
                }
                if (DestPosition != null)
                {
                    var selectPosition = selectPositions.FirstOrDefault(s => s.id == DestPosition.id);
                    if (selectPosition == null)
                    {
                        var ChargeTemplates = _repository.JobTemplates.GeyByOrderType(nameof(JobType.CHARGE), nameof(JobSubType.CHARGE));
                        if (ChargeTemplates != null)
                        {
                            JobTemplate selectJob = null;

                            selectJob = selectJobTemplate(ChargeTemplates, null, DestPosition);
                            if (selectJob != null)
                            {
                                selectPositions.Add(DestPosition);
                                _Queue.CreateJobMission(selectJob, null, null, 0, null
                                                        , null, null, null
                                                        , DestPosition.id, DestPosition.name, DestPosition.linkedFacility
                                                        , worker.id, null);
                            }
                        }
                    }
                }
                if (CrossPosition != null)
                {
                    var selectPosition = selectPositions.FirstOrDefault(s => s.id == CrossPosition.id);
                    if (selectPosition == null)
                    {
                        //Wait템플릿 에서 확인후에 job을 setting
                        var WaitTemplates = _repository.JobTemplates.GeyByOrderType(nameof(JobType.WAIT), nameof(JobSubType.WAIT));
                        if (WaitTemplates != null)
                        {
                            JobTemplate selectJob = null;
                            selectJob = selectJobTemplate(WaitTemplates, null, CrossPosition);
                            if (selectJob != null)
                            {
                                selectPositions.Add(DestPosition);
                                //Job이랑 미션 생성
                                _Queue.CreateJobMission(selectJob, null, null, 0, null
                                                        , null, null, null
                                                        , CrossPosition.id, CrossPosition.name, CrossPosition.linkedFacility
                                                        , crossWorker.id, null);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 대기 위치 Job 생성
        /// </summary>
        private void createWaitControl()
        {
            var workers = _repository.Workers.MiR_GetByActive();    //worker 정보
            var batterySetting = _repository.Battery.GetAll();      //배터리 정보
            if (workers == null || workers.Count == 0) return;
            if (batterySetting == null) return;

            foreach (var worker in workers)
            {
                Position DestPosition = null;
                if (IsInvalid(worker.state)) continue;
                //Job이 없을때 진행한다
                var jobFindNotAssignedWorker = _repository.Jobs.GetAll().FirstOrDefault(j => j.group == worker.group && IsInvalid(j.assignedWorkerId));
                if (jobFindNotAssignedWorker != null) continue;

                ////worker 진행중인 Job이 있는지 확인
                var jobFindAssignedWorker = _repository.Jobs.GetByWorkerId(worker.id).FirstOrDefault();
                if (jobFindAssignedWorker != null) continue;

                //포지션중 점유하고 있지않은 대기위치를 검색한다
                var notOccupiedPositions = _repository.Positions.MiR_GetNotOccupied(null, nameof(PositionSubType.WAIT));
                if (notOccupiedPositions == null || notOccupiedPositions.Count == 0) continue;
                //Worker 상태 확인
                if (worker.state == nameof(WorkerState.IDLE))
                {
                    // Worker가 대기 위치에 있는지 확인
                    //해당 워커 위치가 대기 위치인지 확인
                    var waitPositionOccupieds = _repository.Positions.MiR_GetIsOccupied(null, nameof(PositionSubType.WAIT));
                    var waitPositionOccupied = waitPositionOccupieds.FirstOrDefault(w => w.id == worker.PositionId);
                    if (waitPositionOccupied == null)
                    {
                        //대기위치중 Worker가 갈수있는 대기위치를 선택한다.
                        DestPosition = _repository.Positions.GetAll().FirstOrDefault(r => r.linkedRobotId == worker.id);
                    }

                    if (DestPosition != null)
                    {
                        createJob_Mission(nameof(JobType.WAIT), nameof(JobSubType.WAIT), null, null, 0, null, null, DestPosition.id, worker.id, null);
                    }
                }
            }
        }
        */
    }
}