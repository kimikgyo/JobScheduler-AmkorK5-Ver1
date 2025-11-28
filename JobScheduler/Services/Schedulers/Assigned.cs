using Common.Models.Bases;
using Common.Models.Jobs;
using Common.Models.Settings;

namespace JOB.Services
{
    public partial class SchedulerService
    {
        private void JobAssined()
        {
            workerAssignedControl();
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

            //엘리베이터가 NOTAGV모드일경우 층간이송 워커에게할당하지 않는다.
            var elevator = _repository.Elevator.GetById("NO1");
            if(elevator == null || elevator.mode == "NOTAGVMODE" || elevator.state == "PROTOCOLERROR")
            {
                UnAssignedWorkerJobs = UnAssignedWorkerJobs.Where(r => r.subType.EndsWith("WITHEV") == false).ToList();
            }


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
    }
}