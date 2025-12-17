using Common.Models.Jobs;

namespace JOB.Services
{
    public partial class SchedulerService
    {
        private readonly object _lock = new object();

        private void JobPlanner()
        {
            OrderJobs();
            ChargeJobs();
            WaitJobs();

            //OLD
            //createWaitControl();
            //createChargeControl();
        }

        //Job생성
        private void OrderJobs()
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

                    //[조회]목적지 조회.
                    destination = _repository.Positions.GetById(Order.destinationId);
                    if (destination == null) continue;
                    var carateJob = Createjob(Order, null, destination);
                    if (carateJob == false) continue;
                }
                else
                {
                    //[조회]출발지
                    source = _repository.Positions.MiR_GetById(Order.sourceId);
                    if (source == null) continue;
                    //[조회]목적지
                    destination = _repository.Positions.GetById(Order.destinationId);
                    if (destination == null) continue;

                    var carateJob = Createjob(Order, source, destination);
                    if (carateJob == false) continue;
                }
            }
        }

        private bool Createjob(Order order, Position source, Position destination)
        {
            // ------------------------------------------------------------
            // 1) 방어 코드
            // ------------------------------------------------------------

            if (order == null)
            {
                EventLogger.Error($"[Job][CREATE][ERROR] order is null → job creation aborted");
                return false;
            }
            if (destination == null)
            {
                EventLogger.Error($"[Job][CREATE][ERROR] destination is null → job creation aborted");
                return false;
            }
            if (source == null)
            {
                _Queue.Create_Job(destination.group, order.id, order.type, order.subType, order.carrierId, order.priority, order.drumKeyCode
                                    , null, null, null, destination.id, destination.name, destination.linkedFacility
                                    , order.specifiedWorkerId);
                updateOccupied(destination, true);

                // --------------------------------------------------------
                // 3) 생성 요청 성공 로그
                // --------------------------------------------------------
                EventLogger.Info($"[Job][CREATE] enqueue Soucre is Null job request: Group = {destination.group}, OrderId = {order.id}");

                return true;
            }
            else
            {
                _Queue.Create_Job(source.group, order.id, order.type, order.subType, order.carrierId, order.priority, order.drumKeyCode
                            , source.id, source.name, source.linkedFacility, destination.id, destination.name, destination.linkedFacility
                            , order.specifiedWorkerId);
                updateOccupied(source, true);
                updateOccupied(destination, true);

                // --------------------------------------------------------
                // 3) 생성 요청 성공 로그
                // --------------------------------------------------------
                EventLogger.Info($"[Job][CREATE] enqueue Soucre is Null job request: Group = {destination.group}, OrderId = {order.id}");

                return true;
            }
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

                //[조회] 현재 Job이 상태
                var jobFindNotAssignedWorker = _repository.Jobs.GetAll().FirstOrDefault(j => j.group == worker.group && IsInvalid(j.assignedWorkerId));
                if (jobFindNotAssignedWorker != null && worker.batteryPercent > batterySetting.chargeStart) continue;

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
                        _Queue.Create_Job(worker.group, null, nameof(JobType.CHARGE), nameof(JobSubType.CHARGE), null, 0, null,
                                          null, null, null, DestPosition.id, DestPosition.name, DestPosition.linkedFacility, worker.id);
                        updateOccupied(DestPosition, true);

                    }
                }
                if (CrossPosition != null)
                {
                    var selectPosition = selectPositions.FirstOrDefault(s => s.id == CrossPosition.id);
                    if (selectPosition == null)
                    {
                        _Queue.Create_Job(worker.group, null, nameof(JobType.WAIT), nameof(JobSubType.WAIT), null, 0, null,
                                          null, null, null, CrossPosition.id, CrossPosition.name, CrossPosition.linkedFacility, worker.id);
                        updateOccupied(CrossPosition, true);

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
                //[조회] 현재 Job이 상태
                var jobFindNotAssignedWorker = _repository.Jobs.GetAll().FirstOrDefault(j => j.group == worker.group && IsInvalid(j.assignedWorkerId));
                if (jobFindNotAssignedWorker != null && worker.batteryPercent > batterySetting.minimum) continue;

                //[조회] worker 진행중인 Job이 있는지
                var jobFindAssignedWorker = _repository.Jobs.GetByWorkerId(worker.id).FirstOrDefault();
                if (jobFindAssignedWorker != null) continue;

                //[조회]포지션중 점유하고 있지않은 대기위치를 검색한다
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
                        _Queue.Create_Job(worker.group, null, nameof(JobType.WAIT), nameof(JobSubType.WAIT), null, 0, null
                                          , null, null, null, DestPosition.id, DestPosition.name, DestPosition.linkedFacility
                                          , worker.id);
                    }
                }
            }
        }
    }
}