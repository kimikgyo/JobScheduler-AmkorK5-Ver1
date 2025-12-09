using Common.Models.Bases;
using Common.Models.Jobs;
using Common.Models.Settings;

namespace JOB.Services
{
    public partial class SchedulerService
    {
        private void WorkerAssined()
        {
            JobAssigned_Normal();
            JobReassigned();
        }

        /// <summary>
        /// JobAssigned_Normal
        /// ------------------------------------------------------------
        /// 1) UnAssigned Job 목록 조회 (priority 내림차순 + createdAt 오름차순)
        /// 2) 엘리베이터 상태에 따라 EV 관련 Job 필터링
        /// 3) 거리 기반으로 Worker 에 Job 어사인 시도 (distance 방식)
        /// </summary>
        private void JobAssigned_Normal()
        {
            // [LOG] 노멀 어사인 시작
            //EventLogger.Info("[ASSIGN][NORMAL][START] JobAssigned_Normal called.");

            // 1) UnAssigned Job 목록 조회
            // 우선순위 높은 Job 우선
            // 같은 우선순위 내에서는 먼저 생성된 Job 우선
            var unAssignedWorkerJobs = _repository.Jobs.UnAssignedJobs().OrderByDescending(r => r.priority).ThenBy(r => r.createdAt).ToList();

            if (unAssignedWorkerJobs == null || unAssignedWorkerJobs.Count == 0)
            {
                //EventLogger.Info("[ASSIGN][NORMAL][NO-JOB] Unassigned jobs not found.");
                return;
            }

            // 2) Battery 설정 조회 (워커 조건 검사에 사용)
            var batterySetting = _repository.Battery.GetAll();
            if (batterySetting == null)
            {
                EventLogger.Warn("[ASSIGN][NORMAL][ABORT] Battery setting not found.");
                return;
            }

            // 3) 엘리베이터 상태 확인
            //    - 엘리베이터가 NOTAGVMODE 또는 PROTOCOLERROR 인 경우
            //      층간이송(WithEV) Job 은 필터링
            var elevator = _repository.Elevator.GetById("NO1");
            if (elevator == null || elevator.mode == "NOTAGVMODE" || elevator.state == "PROTOCOLERROR")
            {
                // 1) 층간 이동 Job 목록 추출
                var crossFloorJobs = unAssignedWorkerJobs.Where(job => IsSameFloorJob(job) == false).ToList();
                // 2) 같은 층 Job 만 남기기
                unAssignedWorkerJobs = unAssignedWorkerJobs.Where(job => IsSameFloorJob(job)).ToList();
                EventLogger.Info($"[ASSIGN][FILTER] Elevator unavailable. Removed {crossFloorJobs.Count} cross-floor jobs. " + $"Remaining={unAssignedWorkerJobs.Count}");
            }

            if (unAssignedWorkerJobs == null || unAssignedWorkerJobs.Count == 0)
            {
                EventLogger.Info("[ASSIGN][NORMAL][NO-JOB] After EV filter, no assignable jobs.");
                return;
            }

            // 4) 거리 기반 어사인 로직 수행
            distance(unAssignedWorkerJobs, batterySetting);

            // [LOG] 노멀 어사인 종료
            //EventLogger.Info("[ASSIGN][NORMAL][DONE] JobAssigned_Normal finished.");
        }

        private bool IsSameFloorJob(Job job)
        {
            bool reValue = false;
            var src = _repository.Positions.GetById(job.sourceId);
            var dst = _repository.Positions.GetById(job.destinationId);

            // Position 없으면 필터에서 제외(=false)
            if (src == null || dst == null) reValue = false;
            else if (src.mapId == dst.mapId) reValue = true;

            return reValue;
        }

        /// <summary>
        /// firstJob
        /// ------------------------------------------------------------
        /// - UnAssignedWorkerJobs 목록에서
        ///   각 워커에게 1개씩 순차적으로 Job 을 할당하는 방식
        ///
        /// 규칙:
        ///   1) 워커는 Active + IDLE 상태만 대상
        ///   2) 이미 Job 이 할당된 워커는 건너뜀
        ///   3) Job 은 워커의 group 과 동일한 Job 중에서 선택
        ///      3-1) 우선: 지정 워커 Job (specifiedWorkerId == worker.id)
        ///      3-2) 없다면: 미지정 Job (specifiedWorkerId 비어있는 Job)
        ///   4) WorkerCondition / ChangeWaitDeleteMission 통과 시 Create_Mission 실행
        /// </summary>
        private void firstJob(List<Job> unAssignedWorkerJobs, Battery batterySetting)
        {
            // 기본 방어 코드
            if (unAssignedWorkerJobs == null || unAssignedWorkerJobs.Count == 0)
                return;

            if (batterySetting == null)
                return;

            // 1) Active Worker 목록 조회
            var workers = _repository.Workers.MiR_GetByActive();
            if (workers == null || workers.Count == 0)
                return;

            foreach (var worker in workers)
            {
                // 1-1) IDLE 상태가 아니면 이 워커는 건너뛰기
                if (worker.state != nameof(WorkerState.IDLE))
                    continue;

                // 1-2) 이미 Job 이 할당된 워커이면 건너뜀
                var runJob = _repository.Jobs.GetByAssignWorkerId(worker.id).FirstOrDefault();

                if (runJob != null)
                    continue;

                // 2) 이 워커의 group 과 동일한 Job 목록만 필터링
                var jobsByGroup = unAssignedWorkerJobs.Where(u => u.group == worker.group).ToList();

                if (jobsByGroup == null || jobsByGroup.Count == 0)
                    continue;

                Job job = null;

                // 3) 지정 워커 Job 우선 선택
                job = jobsByGroup.FirstOrDefault(j => j.specifiedWorkerId == worker.id);

                // 4) 지정 워커 Job 이 없으면, 미지정 Job (specifiedWorkerId 없는 Job) 선택
                if (job == null)
                {
                    job = jobsByGroup.FirstOrDefault(j => IsInvalid(j.specifiedWorkerId));
                }

                // 5) 그래도 Job 이 없으면 이 워커는 패스
                if (job == null)
                    continue;

                // 6) 워커 상태/배터리 등 조건 검사
                if (!WorkerCondition(job, worker, batterySetting))
                    continue;

                // 7) 기존 WAIT 미션 정리 (필요시)
                if (!ChangeWaitDeleteMission(worker))
                    continue;

                // 8) 최종 Job 할당 및 미션 생성
                Create_Mission(job, worker);

                EventLogger.Info($"[ASSIGN][NORMAL][FIRSTJOB][ASSIGNED] workerId={worker.id}, workerName={worker.name}, jobId={job.guid}, jobType={job.type}, group={job.group}");

                // 9) 이 Job 은 더 이상 UnAssigned 가 아니므로 리스트에서 제거
                unAssignedWorkerJobs.Remove(job);
            }
        }

        /// <summary>
        /// distance
        /// ------------------------------------------------------------
        /// 1) Active Worker 조회 → IDLE 상태인 워커만 대상
        /// 2) 이미 Job 이 할당된 워커 중, CHARGE/WAIT 가 아닌 Job 을 수행중인 워커는 제외
        /// 3) 지정 워커 Job / 비지정 워커 Job 으로 분리하여 어사인
        ///    - 지정 워커 Job : 워커 기준으로 가장 가까운 Job 선택
        ///    - 비지정 Job    : Job 기준으로 가장 가까운 워커 선택
        /// </summary>
        private void distance(List<Job> unAssignedWorkerJobs, Battery batterySetting)
        {
            // 0) 유효성 체크
            if (unAssignedWorkerJobs == null || unAssignedWorkerJobs.Count == 0)
                return;
            if (batterySetting == null)
                return;

            // 1) Active Worker 목록 조회
            var workers = _repository.Workers.MiR_GetByActive();
            if (workers == null || workers.Count == 0)
            {
                EventLogger.Info("[ASSIGN][NORMAL][DISTANCE], No active workers.");
                return;
            }

            // IDLE 상태인 워커만 필터링
            var idleWorkers = workers.Where(r => r.state == nameof(WorkerState.IDLE)).ToList();

            if (idleWorkers == null || idleWorkers.Count == 0)
            {
                EventLogger.Info("[ASSIGN][NORMAL][DISTANCE], No IDLE workers.");
                return;
            }

            // 작업 대상 워커 리스트는 idleWorkers 기준으로 사용
            workers = idleWorkers;

            // 2) 이미 Job 이 할당된 워커 중, CHARGE/WAIT 아닌 Job 실행중인 워커 제거
            var runningJobs = _repository.Jobs.GetAll().Where(r => r.assignedWorkerId != null).ToList();

            foreach (var runJob in runningJobs)
            {
                // [조회] Job 을 수행중인 워커
                var runJobWorker = workers.FirstOrDefault(r => r.id == runJob.assignedWorkerId);

                // [조건] 워커가 존재하고, 해당 Job 이 CHARGE/WAIT 가 아닐 경우
                if (runJobWorker != null && runJob.type != nameof(JobType.WAIT) && runJob.type != nameof(JobType.CHARGE))
                {
                    // 이 워커는 이미 다른 작업 중이므로 대상에서 제외
                    workers.Remove(runJobWorker);
                }
            }

            if (workers == null || workers.Count == 0)
            {
                EventLogger.Info("[ASSIGN][NORMAL][DISTANCE], All workers are busy.");
                return;
            }

            // 3) Job 을 지정 워커 / 비지정 워커로 분리
            var findSpecifiedWorkerJobs = unAssignedWorkerJobs.Where(j => IsInvalid(j.specifiedWorkerId) == false).ToList();
            var findNotSpecifiedWorkerJobs = unAssignedWorkerJobs.Where(j => IsInvalid(j.specifiedWorkerId) == true).ToList();

            // --------------------------------------------------------
            // 3-1) 지정 워커 Job 처리
            // --------------------------------------------------------
            if (findSpecifiedWorkerJobs != null && findSpecifiedWorkerJobs.Count > 0)
            {
                var assignedWorkers = new List<Worker>();

                foreach (var worker in workers)
                {
                    // 이 워커에게 지정된 Job 목록
                    var specifiedWorkerJobs = findSpecifiedWorkerJobs.Where(r => r.specifiedWorkerId == worker.id).ToList();

                    if (specifiedWorkerJobs == null || specifiedWorkerJobs.Count == 0)
                        continue;

                    // 워커 기준으로 가장 가까운 Job 선택
                    var job = SelectNearestJobForWorker(worker, specifiedWorkerJobs);

                    if (job == null)
                        continue;

                    // 워커 상태/배터리 등 조건 체크
                    if (!WorkerCondition(job, worker, batterySetting))
                        continue;

                    // 기존 WAIT 미션 정리 (필요 시)
                    if (!ChangeWaitDeleteMission(worker))
                    {
                        EventLogger.Warn($"[ASSIGN][NORMAL][DISTANCE][SPECIFIED][ABORT], ChangeWaitDeleteMission failed. workerId={worker.id}, workerName={worker.name}");
                        continue;
                    }

                    // 실제 Job 할당 및 Mission 생성
                    Create_Mission(job, worker);

                    EventLogger.Info($"[ASSIGN][NORMAL][DISTANCE][SPECIFIED][ASSIGNED], workerId={worker.id}, workerName={worker.name}, jobId={job.guid}, jobType={job.type}, group={job.group}");

                    assignedWorkers.Add(worker);
                }

                // 위에서 이미 할당된 워커는 이후 비지정 Job 에서 제외
                foreach (var assignedWorker in assignedWorkers)
                {
                    workers.Remove(assignedWorker);
                }
            }

            // 지정 워커 Job 처리가 끝났는데 워커가 하나도 남지 않았다면 종료
            if (workers == null || workers.Count == 0)
            {
                EventLogger.Info("[ASSIGN][NORMAL][DISTANCE], No workers left after specified jobs.");
                return;
            }

            // --------------------------------------------------------
            // 3-2) 비지정 워커 Job 처리
            // --------------------------------------------------------
            if (findNotSpecifiedWorkerJobs != null && findNotSpecifiedWorkerJobs.Count > 0)
            {
                foreach (var job in findNotSpecifiedWorkerJobs)
                {
                    // 이 Job 의 group 에 맞는 워커만 필터링 (원본 workers 를 건드리지 않기 위해 별도 리스트 사용)
                    var candidates = workers.Where(w => w.group == job.group).ToList();

                    if (candidates == null || candidates.Count == 0)
                        continue;

                    // Job 기준으로 가장 가까운 Worker 선택
                    var worker = SelectNearestWorkerForJob(candidates, job);
                    if (worker == null)
                        continue;

                    // 워커 상태/배터리 등 조건 체크
                    if (!WorkerCondition(job, worker, batterySetting))
                        continue;

                    // 기존 WAIT 미션 정리
                    if (!ChangeWaitDeleteMission(worker))
                    {
                        EventLogger.Warn($"[ASSIGN][NORMAL][DISTANCE][UNSPECIFIED][ABORT], ChangeWaitDeleteMission failed. workerId={worker.id}, workerName={worker.name}");
                        continue;
                    }

                    // 실제 Job 할당 및 Mission 생성
                    Create_Mission(job, worker);
                    EventLogger.Info($"[ASSIGN][NORMAL][DISTANCE][UNSPECIFIED][ASSIGNED], workerId={worker.id}, workerName={worker.name}, jobId={job.guid}, jobType={job.type}, group={job.group}");
                }
            }
        }

        //충전이나 대기위치 미션을 삭제한다.
        private bool ChangeWaitDeleteMission(Worker worker)
        {
            bool reValue = true;
            var runjob = _repository.Jobs.GetByAssignWorkerId(worker.id).FirstOrDefault();
            if (runjob != null)
            {
                if ((runjob.type == nameof(JobType.WAIT)) || (runjob.type == nameof(JobType.CHARGE)))
                {
                    runjob.terminator = "JobScheduler";
                    runjob.terminationType = "CANCEL";
                    runjob.terminatedAt = DateTime.Now;
                    reValue = false;
                }
            }
            return reValue;
        }

        /// <summary>
        /// [Sub] AssignedControl
        /// 지정이 아닌 특정조건으로 인하여 Job 전택
        /// </summary>
        /// <param name="workers"></param>
        /// <param name="job"></param>
        /// <returns></returns>
        private Worker SelectNearestWorkerForJob(List<Worker> workers, Job job)
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
        private Job SelectNearestJobForWorker(Worker worker, List<Job> jobs)
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