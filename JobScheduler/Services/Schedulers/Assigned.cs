using Common.Models.Bases;
using Common.Models.Jobs;
using Common.Models.Settings;
using Common.Templates;

namespace JOB.Services
{
    public partial class SchedulerService
    {
        private void WorkerAssined()
        {
            workerAssignedControl();
        }

        /// <summary>
        /// 워커 할당 제어
        /// </summary>
        private void workerAssignedControl()
        {
            //신규 Job 이있는지 확인
            var UnAssignedWorkerJobs = _repository.Jobs.GetByState(nameof(JobState.INIT)).Where(j => j.terminationType == null && string.IsNullOrWhiteSpace(j.assignedWorkerId)).ToList();
            UnAssignedWorkerJobs = UnAssignedWorkerJobs.OrderByDescending(r => r.priority).ToList();
            UnAssignedWorkerJobs = UnAssignedWorkerJobs.OrderBy(r => r.createdAt).ToList();
            if (UnAssignedWorkerJobs == null || UnAssignedWorkerJobs.Count == 0) return;
            var batterySetting = _repository.Battery.GetAll();
            if (batterySetting == null) return;

            //엘리베이터가 NOTAGV모드일경우 층간이송 워커에게할당하지 않는다.
            var elevator = _repository.Elevator.GetById("NO1");
            if (elevator == null || elevator.mode == "NOTAGVMODE" || elevator.state == "PROTOCOLERROR")
            {
                UnAssignedWorkerJobs = UnAssignedWorkerJobs.Where(r => r.subType.EndsWith("WITHEV") == false).ToList();
            }

            //순차적용
            //firstJob(UnAssignedWorkerJobs, batterySetting);
            //거리순 적용
            distance(UnAssignedWorkerJobs, batterySetting);
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
            Job returnJob = null;
            Worker returnWorker = null;

            foreach (var worker in _repository.Workers.MiR_GetByActive())
            {
                Job job = null;
                if (worker.state != nameof(WorkerState.IDLE)) return;

                //[조회]Job 이할당되어있는지
                var runjob = _repository.Jobs.GetByAssignWorkerId(worker.id).FirstOrDefault();
                if (runjob != null) continue;

                //[조회]그룹과 일치하는 Job이있는지
                UnAssignedWorkerJobs = UnAssignedWorkerJobs.Where(u => u.group == worker.group).ToList();

                //[조회]지정한 워커가 있는지 Job
                job = UnAssignedWorkerJobs.FirstOrDefault(j => j.specifiedWorkerId == worker.id);

                if (job == null)
                {
                    //[조회]지정한 워커가 없는 Job
                    job = UnAssignedWorkerJobs.FirstOrDefault(j => IsInvalid(j.specifiedWorkerId));
                }

                if (job != null)
                {
                    if (WorkerCondition(job, worker, batterySetting) == false) continue;
                    Create_Mission(job, worker);
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
            var idleWorkers = workers.Where(r => r.state == nameof(WorkerState.IDLE)).ToList();
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
                        if (WorkerCondition(job, worker, batterySetting) == false) continue;
                        Create_Mission(job, worker);
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
                        if (WorkerCondition(job, worker, batterySetting) == false) continue;
                        Create_Mission(job, worker);
                        workers.Remove(worker);
                    }
                }
            }
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

        private bool WorkerCondition(Job job, Worker worker, Battery battery)
        {
            bool Condition = true;
            switch (job.type)
            {
                case nameof(JobType.TRANSPORT):
                case nameof(JobType.TRANSPORT_SLURRY_SUPPLY):
                case nameof(JobType.TRANSPORT_SLURRY_RECOVERY):
                case nameof(JobType.TRANSPORT_CHEMICAL_RECOVERY):
                case nameof(JobType.TRANSPORT_CHEMICAL_SUPPLY):
                    if (worker.batteryPercent > battery.minimum) Condition = false;
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

        private void Create_Mission(Job job, Worker worker)
        {
            int seq = 1;
            bool ElevatorMissionFlag = false;
            Position source = null;
            Position destination = null;

            var Resource = _repository.ServiceApis.GetAll().FirstOrDefault(s => s.type == "Resource");
            if (Resource == null) return;

            destination = _repository.Positions.GetById(job.destinationId);
            if (IsInvalid(job.sourceId))
            {
                var positions = _repository.Positions.MiR_GetByMapId(worker.mapId);
                if (positions == null || positions.Count() == 0) return;
                //워커에서 가장 가까운 포지션을 출발지 포지션으로 설정한다.
                source = _repository.Positions.FindNearestWayPoint(worker, positions).FirstOrDefault();
            }
            else
            {
                source = _repository.Positions.GetById(job.sourceId);
            }

            if (source == null) return;
            if (destination == null) return;

            var Routes_Plan = Resource.Api.Post_Routes_Plan_Async(_mapping.RoutesPlanas.Request(source.positionId, destination.positionId)).Result;

            if (Routes_Plan.nodes == null) return;

            switch (job.type)
            {
                case nameof(JobType.TRANSPORT):
                case nameof(JobType.TRANSPORT_SLURRY_SUPPLY):
                case nameof(JobType.TRANSPORT_SLURRY_RECOVERY):
                case nameof(JobType.TRANSPORT_CHEMICAL_SUPPLY):
                case nameof(JobType.TRANSPORT_CHEMICAL_RECOVERY):
                case nameof(JobType.TRANSPORT_AICERO_SUPPLY):
                case nameof(JobType.TRANSPORT_AICERO_RESOVERY):
                    foreach (var node in Routes_Plan.nodes)
                    {
                        var position = _repository.Positions.GetByPositionId(node.positionId);

                        if (node.positionId == source.positionId)
                        {
                            seq = create_GroupMission(job, position, worker, seq, nameof(MissionsTemplateGroup.PICK));
                        }
                        else if (node.positionId == destination.positionId)
                        {
                            seq = create_GroupMission(job, position, worker, seq, nameof(MissionsTemplateGroup.DROP));
                        }
                        else if (node.nodeType.ToUpper() == nameof(NodeType.ELEVATOR))
                        {
                            if (ElevatorMissionFlag == false)
                            {
                                seq = create_GroupMission(job, null, worker, seq, nameof(MissionsTemplateGroup.ELEVATOR));
                                ElevatorMissionFlag = true;
                            }
                            else continue;
                        }
                        else if (node.nodeType.ToUpper() == nameof(NodeType.TRAFFICPOINT))
                        {
                            seq = create_GroupMission(job, position, worker, seq, nameof(MissionsTemplateGroup.TRAFFIC));
                        }
                        else
                        {
                            seq = create_SingleMission(job, position, worker, seq, nameof(MissionTemplateType.MOVE), nameof(MissionTemplateSubType.STOPOVERMOVE));
                        }
                    }

                    break;

                case nameof(JobType.CHARGE):
                case nameof(JobType.WAIT):
                    foreach (var node in Routes_Plan.nodes)
                    {
                        var position = _repository.Positions.GetByPositionId(node.positionId);

                        if (node.positionId == source.positionId)
                        {
                            seq = create_SingleMission(job, position, worker, seq, nameof(MissionTemplateType.MOVE), nameof(MissionTemplateSubType.SOURCEMOVE));
                        }
                        else if (node.positionId == destination.positionId)
                        {
                            seq = create_SingleMission(job, position, worker, seq, nameof(MissionTemplateType.MOVE), nameof(MissionTemplateSubType.DESTINATIONMOVE));
                        }
                        else if (node.nodeType.ToUpper() == nameof(NodeType.ELEVATOR))
                        {
                            if (ElevatorMissionFlag == false)
                            {
                                seq = create_GroupMission(job, null, worker, seq, nameof(MissionsTemplateGroup.ELEVATOR));
                                ElevatorMissionFlag = true;
                            }
                            else continue;
                        }
                        else if (node.nodeType.ToUpper() == nameof(NodeType.TRAFFICPOINT))
                        {
                            seq = create_GroupMission(job, position, worker, seq, nameof(MissionsTemplateGroup.TRAFFIC));
                        }
                        else
                        {
                            seq = create_SingleMission(job, position, worker, seq, nameof(MissionTemplateType.MOVE), nameof(MissionTemplateSubType.STOPOVERMOVE));
                        }
                    }
                    break;
            }
            job.assignedWorkerId = worker.id;
            updateStateJob(job, nameof(JobState.WORKERASSIGNED), true);

            if (job.orderId != null)
            {
                var order = _repository.Orders.GetByid(job.orderId);
                if (order != null)
                {
                    order.assignedWorkerId = worker.id;
                    updateStateOrder(order, OrderState.Transferring, true);
                }
            }
        }

        private int create_SingleMission(Job job, Position position, Worker worker, int seq, string type, string subtype)
        {
            lock (_lock)
            {
                var template = _repository.MissionTemplates_Single.GetByType_SubType(type, subtype);
                if (template != null)
                {
                    var missionTemplate = _mapping.MissionTemplates.Create(template);
                    _Queue.Create_Mission(job, missionTemplate, position, worker, seq);
                    seq++;
                }
            }
            return seq;
        }

        private int create_GroupMission(Job job, Position position, Worker worker, int seq, string templateGroup)
        {
            lock (_lock)
            {
                var Templates = _repository.MissionTemplates_Group.GetByGroup(templateGroup);
                foreach (var template in Templates)
                {
                    var missionTemplate = _mapping.MissionTemplates.Create(template);
                    _Queue.Create_Mission(job, missionTemplate, position, worker, seq);
                    seq++;
                }
            }
            return seq;
        }
    }
}