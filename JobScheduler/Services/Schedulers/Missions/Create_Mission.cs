using Common.Models.Bases;
using Common.Models.Jobs;
using Common.Templates;

namespace JOB.Services
{
    public partial class SchedulerService
    {
        /// <summary>
        /// Create_Mission
        /// ─────────────────────────────────────────────────────────
        /// 할당된 Worker가 해당 Job을 수행하기 위해 필요한 Mission(실행 단계) 목록을 생성한다.
        ///
        /// [목적]
        ///  - Worker의 현재 위치를 기준으로 Job의 출발지/목적지까지 이동 경로를 분석하고
        ///  - 경로 노드를 기반으로 Mission들을 자동으로 구성하여 DB에 저장한다.
        ///
        /// [동작 흐름]
        ///  1) Subscribe_Worker 기준 실제 출발 위치(Position) 계산
        ///  2) Job 출발지/목적지 Position 결정
        ///     - Job.sourceId 가 없으면 Subscribe_Worker 위치가 출발지 역할
        ///  3) Job 타입(TRANSPORT / CHARGE / WAIT)에 따라 Mission 구성 방식 분리
        ///  4) Resource API 에 이동 경로(RoutePlan) 요청
        ///  5) RoutePlan 노드를 순회하며 Mission 생성
        ///     - PICK / DROP / ELEVATOR / TRAFFIC / MOVE
        ///  6) 생성 완료 후 Job/Order 상태 업데이트
        ///
        /// [특징]
        ///  - 항상 Subscribe_Worker 기준 동선 설계 (재할당 시 로봇 현재 위치를 시작점으로 활용 가능)
        ///  - Mission 동적 구성: 지도 변경, 경로 변경에도 자동 대응
        ///  - Job 출발지가 없을 경우에도 유연하게 동작
        ///
        /// [예시 수행 시나리오]
        ///   Subscribe_Worker 위치 → Job Pick 위치 → Subscribe_Elevator → Destination → Drop
        ///
        /// [확장]
        ///  - 재할당 Job 미션 생성 시, 시작 위치 override 가능
        ///  - Mission 재정렬, Mission Skip 등 고급 미션 관리 구조와 연계 가능
        /// </summary>
        private bool Create_Mission(Job job, Worker worker)
        {
            bool CreateMission = false;
            // job 없음
            if (job == null) return CreateMission;
            // worker 없음
            if (worker == null) return CreateMission;
            // mission sequence 시작
            int seq = 1;

            // 1) 외부 Route API 핸들
            var resource = _repository.ServiceApis.GetAll().FirstOrDefault(s => s.type == nameof(Service.RESOURCE));
            // 외부 핸들 없음
            if (resource == null) return CreateMission;

            // 2) Subscribe_Worker 기준 시작 위치 + Job 출발/목적지 계산
            var workerStart = GetWorkerStartPosition(worker);
            // 시작점 계산 실패
            if (workerStart == null) return CreateMission;

            var jobSource = GetJobSourcePosition(job, workerStart);
            // 출발지 계산 실패
            if (jobSource == null) return CreateMission;

            var jobDestination = GetJobDestinationPosition(job);
            // 목적지 계산 실패

            if (jobDestination == null) return CreateMission;

            // 3) Job 타입별로 미션 구성 위임
            switch (job.type)
            {
                case nameof(JobType.TRANSPORT):
                case nameof(JobType.MANUALTRANSPORT):
                case nameof(JobType.TRANSPORT_SLURRY_SUPPLY):
                case nameof(JobType.TRANSPORT_SLURRY_RECOVERY):
                case nameof(JobType.TRANSPORT_CHEMICAL_SUPPLY):
                case nameof(JobType.TRANSPORT_CHEMICAL_RECOVERY):
                case nameof(JobType.TRANSPORT_AICERO_SUPPLY):
                case nameof(JobType.TRANSPORT_AICERO_RESOVERY):
                    if (workerStart.positionId == jobDestination.positionId)
                    {
                        CreateMission = BuildTransportMission_WorkerStartPOSSamejobDestination(job, worker, resource, workerStart, jobSource, jobDestination, seq);
                    }
                    else
                    {
                        CreateMission = BuildTransportMissions(job, worker, resource, workerStart, jobSource, jobDestination, seq);
                    }
                    break;

                case nameof(JobType.CHARGE):
                case nameof(JobType.WAIT):
                    if (workerStart.positionId == jobDestination.positionId)
                    {
                        CreateMission = BuildChargeWaitMissions_WorkerStartPOSSamejobDestination(job, worker, resource, workerStart, jobSource, jobDestination, seq);
                    }
                    else
                    {
                        CreateMission = BuildChargeWaitMissions(job, worker, resource, workerStart, jobDestination, seq);
                    }
                    break;
            }
            if (CreateMission)
            {
                // 4) Job / Order 상태 업데이트 (기존 로직 유지)
                job.assignedWorkerId = worker.id;
                job.assignedWorkerName = worker.name;
                updateStateJob(job, nameof(JobState.WORKERASSIGNED), job.terminateState, job.terminationType, job.terminator, true);

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
            return CreateMission;
        }

        // 0) Subscribe_Worker 기준 시작 위치 구하기
        //    - 최초 할당: mapId 기준 가장 가까운 Position
        //    - (나중에 재할당 시에는 별도 인자나 WorkerState에서 현재 위치를 가져오도록 확장 가능)
        private Position GetWorkerStartPosition(Worker worker)
        {
            if (worker == null) return null;

            // ------------------------------------------------------------
            // 1) 대상 포지션 로드 (worker.mapId 기준, 제외 nodeType 필터)
            // ------------------------------------------------------------
            var positions = _repository.Positions.MiR_GetByMapId(worker.mapId).Where(p => p != null && p.nodeType != nameof(NodeType.WORK) && p.nodeType != nameof(NodeType.ELEVATOR)
                                                                                    && p.nodeType != nameof(NodeType.CHARGER)).ToList();

            if (positions == null || positions.Count == 0) return null;

            // ------------------------------------------------------------
            // 2) worker.PositionId가 유효하면, 그 포지션을 우선 반환 (단, 제외 타입이면 무시)
            // ------------------------------------------------------------
            if (!IsInvalid(worker.PositionId))
            {
                var workerPos = _repository.Positions.GetById(worker.PositionId);

                if (workerPos != null &&
                    workerPos.nodeType != nameof(NodeType.WORK) &&
                    workerPos.nodeType != nameof(NodeType.ELEVATOR) &&
                    workerPos.nodeType != nameof(NodeType.CHARGER))
                {
                    return workerPos;
                }
            }

            // ------------------------------------------------------------
            // 3) 가장 가까운 후보 리스트(정렬된 리스트라고 가정) 구하기
            // ------------------------------------------------------------
            var nearestCandidates = _repository.Positions.FindNearestWayPoint(worker, positions);
            if (nearestCandidates == null || nearestCandidates.Count == 0) return null;

            // ------------------------------------------------------------
            // 4) 우선순위 linkedRobotId가 본인인 포지션 (이미 예약/연결된 자리) 이거나 점유 X + linkedRobotId 비어있는 가장 가까운 포지션
            // ------------------------------------------------------------

            var freeAndLinked = nearestCandidates.FirstOrDefault(p =>
                    p != null && p.linkedRobotId == worker.id
                || (p.isOccupied == false && string.IsNullOrWhiteSpace(p.linkedRobotId))
                );
            return freeAndLinked;
            // ------------------------------------------------------------
            // 4) 우선순위 1: linkedRobotId가 본인인 포지션 (이미 예약/연결된 자리)
            // ------------------------------------------------------------
            //var linked = nearestCandidates.FirstOrDefault(p => p != null && p.isOccupied == true && p.linkedRobotId == worker.id);
            //if (linked != null) return linked;

            // ------------------------------------------------------------
            // 5) 우선순위 2: 점유 X + linkedRobotId 비어있는 가장 가까운 포지션
            // ------------------------------------------------------------
            //var free = nearestCandidates.FirstOrDefault(p =>
            //    p != null &&
            //    p.isOccupied == false &&
            //    string.IsNullOrWhiteSpace(p.linkedRobotId));

            //return free;
        }

        // 1) Job 출발지 Position 구하기
        //    - job.sourceId 가 있으면: 그 Position
        //    - 없으면: WorkerStart 가 곧 출발지
        private Position GetJobSourcePosition(Job job, Position workerStart)
        {
            if (job == null) return null;

            // Job 출발지가 없는 경우 → Subscribe_Worker 위치가 출발지
            if (IsInvalid(job.sourceId)) return workerStart;

            // Job 출발지가 있는 경우 → 해당 Position
            var source = _repository.Positions.GetById(job.sourceId);
            return source;
        }

        // 2) Job 목적지 Position 구하기
        private Position GetJobDestinationPosition(Job job)
        {
            if (job == null) return null;
            if (IsInvalid(job.destinationId)) return null;

            var dest = _repository.Positions.GetById(job.destinationId);
            return dest;
        }

        private bool BuildTransportMission_WorkerStartPOSSamejobDestination(Job job, Worker worker, ServiceApi resource, Position workerStart, Position jobSource, Position jobDestination
                                           , int seq)
        {
            bool reValue = false;
            if (jobDestination.nodeType == nameof(NodeType.WAYPOINT))
            {
                if (job.type == nameof(JobType.TRANSPORT)) seq = template_GroupMission(job, jobDestination, worker, seq, nameof(MissionsTemplateGroup.TRANSPORTDROP));
                else seq = template_GroupMission(job, jobDestination, worker, seq, nameof(MissionsTemplateGroup.MANUALTRANSPORTDROP));
                reValue = true;
            }
            return reValue;
        }

        private bool BuildChargeWaitMissions_WorkerStartPOSSamejobDestination(Job job, Worker worker, ServiceApi resource, Position workerStart, Position jobSource, Position jobDestination
                                          , int seq)
        {
            bool reValue = false;
            if (jobDestination.nodeType == nameof(NodeType.WAYPOINT))
            {
                seq = template_SingleMission(job, jobDestination, worker, seq, nameof(MissionTemplateType.MOVE), nameof(MissionTemplateSubType.DESTINATIONMOVE));
            }
            return reValue;
        }

        // TRANSPORT 계열 Job 미션 구성
        // Segment A: WorkerStart → JobSource (MOVE만)
        // Segment B: JobSource   → JobDestination (PICK / DROP / ELEVATOR / TRAFFIC / MOVE)
        private bool BuildTransportMissions(Job job, Worker worker, ServiceApi resource, Position workerStart, Position jobSource, Position jobDestination
                                           , int seq)
        {
            bool reValue = false;
            // A) Segment A: WorkerStart → JobSource

            if (workerStart.positionId != jobSource.positionId)
            {
                var routesPlanRequest_A = _mapping.RoutesPlanas.Request(workerStart.positionId, jobSource.positionId);
                var routesPlanResponse_A = resource.Api.Post_Routes_Plan_Async(routesPlanRequest_A).Result;

                if (routesPlanResponse_A == null)
                {
                    EventLogger.Warn($"[ROUTE_A][PLAN][NO_ROUTE] response is null (path not found?), jobId={job?.guid}, from={workerStart?.positionId}, fromName={workerStart?.name}" +
                                     $", to={jobSource?.positionId}, toName = {jobSource?.name} ,WorkerId={worker?.id}, WorkerName={worker.name}");
                    return reValue;
                }
                if (routesPlanResponse_A.nodes == null)
                {
                    EventLogger.Warn($"[ROUTE_A][PLAN][NO_ROUTE] nodes is null/empty (path not found), jobId={job?.guid}, from={workerStart?.positionId}, fromName={workerStart?.name}" +
                                     $", to={jobSource?.positionId}, toName = {jobSource?.name} ,WorkerId={worker?.id}, WorkerName={worker.name}");
                    return reValue;
                }
                Position Segment_A_ElevatorSource = null;
                Position Segment_A_Elevatordest = null;

                foreach (var node in routesPlanResponse_A.nodes)
                {
                    var position = _repository.Positions.GetByPositionId(node.positionId);
                    if (position == null) continue;
                    if (position.id == jobSource.id) continue;

                    switch (node.nodeType.ToUpper())
                    {
                        case nameof(NodeType.WAYPOINT):
                            if (node.positionId == jobSource.positionId)
                            {
                                if (job.type == nameof(JobType.TRANSPORT)) seq = template_GroupMission(job, position, worker, seq, nameof(MissionsTemplateGroup.TRANSPORTPICK));
                                else seq = template_GroupMission(job, position, worker, seq, nameof(MissionsTemplateGroup.MANUALTRANSPORTPICK));
                            }
                            else if (node.positionId == jobDestination.positionId)
                            {
                                if (job.type == nameof(JobType.TRANSPORT)) seq = template_GroupMission(job, position, worker, seq, nameof(MissionsTemplateGroup.TRANSPORTDROP));
                                else seq = template_GroupMission(job, position, worker, seq, nameof(MissionsTemplateGroup.MANUALTRANSPORTDROP));
                            }
                            else
                            {
                                seq = template_SingleMission(job, position, worker, seq, nameof(MissionTemplateType.MOVE), nameof(MissionTemplateSubType.STOPOVERMOVE));
                            }
                            break;

                        case nameof(NodeType.TRAFFIC):

                            if (node.positionId == jobSource.positionId)
                            {
                                if (job.type == nameof(JobType.TRANSPORT)) seq = template_GroupMission(job, position, worker, seq, nameof(MissionsTemplateGroup.TRANSPORTPICK));
                                else seq = template_GroupMission(job, position, worker, seq, nameof(MissionsTemplateGroup.MANUALTRANSPORTPICK));

                                seq = template_SingleMission(job, position, worker, seq, nameof(MissionTemplateType.ACTION), nameof(MissionTemplateSubType.TRAFFIC));
                            }
                            else if (node.positionId == jobDestination.positionId)
                            {
                                if (job.type == nameof(JobType.TRANSPORT)) seq = template_GroupMission(job, position, worker, seq, nameof(MissionsTemplateGroup.TRANSPORTDROP));
                                else seq = template_GroupMission(job, position, worker, seq, nameof(MissionsTemplateGroup.MANUALTRANSPORTDROP));
                            }
                            else
                            {
                                seq = template_GroupMission(job, position, worker, seq, nameof(MissionsTemplateGroup.TRAFFIC));
                            }

                            break;

                        case nameof(NodeType.AUTODOOROPEN):
                            seq = template_GroupMission(job, position, worker, seq, nameof(MissionsTemplateGroup.AUTODOOROPEN));
                            break;

                        case nameof(NodeType.AUTODOORCLOSE):
                            seq = template_GroupMission(job, position, worker, seq, nameof(MissionsTemplateGroup.AUTODOORCLOSE));
                            break;

                        case nameof(NodeType.AUTODOOROPENREQUEST):
                            seq = template_GroupMission(job, position, worker, seq, nameof(MissionsTemplateGroup.AUTODOOROPENREQUEST));
                            break;

                        case nameof(NodeType.AUTODOORCLOSEREQUEST):
                            seq = template_GroupMission(job, position, worker, seq, nameof(MissionsTemplateGroup.AUTODOORCLOSEREQUEST));
                            break;

                        case nameof(NodeType.AIRSHOWER):
                            seq = template_GroupMission(job, position, worker, seq, nameof(MissionsTemplateGroup.AIRSHOWER));
                            break;

                        case nameof(NodeType.ELEVATOR):
                            if (Segment_A_ElevatorSource == null)
                            {
                                Segment_A_ElevatorSource = position;
                                seq = template_GroupMission(job, Segment_A_ElevatorSource, worker, seq, nameof(MissionsTemplateGroup.ELEVATORSOURCE));
                                //EventLogger.Info($"[ASSIGN][ASSIGN][ELEVATOR-GROUP][ELEVATORSOURCE] workerName={worker.name}, workerId={worker.id}, jobSecondId={job.guid}, seq={seq}");
                            }
                            else if (Segment_A_ElevatorSource != null)
                            {
                                Segment_A_Elevatordest = position;
                                seq = template_GroupMission(job, Segment_A_Elevatordest, worker, seq, nameof(MissionsTemplateGroup.ELEVATORDEST));
                                //EventLogger.Info($"[ASSIGN][ASSIGN][ELEVATOR-GROUP][ELEVATORDEST] workerName={worker.name}, workerId={worker.id}, jobSecondId={job.guid}, seq={seq}");
                            }
                            break;

                        case nameof(NodeType.CHARGER):

                            seq = template_SingleMission(job, position, worker, seq, nameof(MissionTemplateType.MOVE), nameof(MissionTemplateSubType.CHARGERMOVE));

                            break;
                    }

                    reValue = true;
                }
            }

            // B) Segment B: JobSource → JobDestination
            var routesPlanRequest_B = _mapping.RoutesPlanas.Request(jobSource.positionId, jobDestination.positionId);
            var routesPlanResponse_B = resource.Api.Post_Routes_Plan_Async(routesPlanRequest_B).Result;

            if (routesPlanResponse_B == null)
            {
                EventLogger.Warn($"[ROUTE_B][PLAN][NO_ROUTE] response is null (path not found?) jobId={job?.guid}, from={jobSource?.positionId}, fromName={jobSource?.name}, " +
                                    $"to={jobDestination?.positionId},toName = {jobDestination?.name} ,WorkerId={worker?.id}, WorkerName={worker.name}");
                return reValue;
            }
            if (routesPlanResponse_B.nodes == null)
            {
                EventLogger.Warn($"[ROUTE_B][PLAN][NO_ROUTE] nodes is null/empty (path not found) jobId={job?.guid}, from={jobSource?.positionId}, fromName={jobSource?.name}, " +
                                    $"to={jobDestination?.positionId},toName = {jobDestination?.name} ,WorkerId={worker?.id}, WorkerName={worker.name}");
                return reValue;
            }

            Position Segment_B_ElevatorSource = null;
            Position Segment_B_Elevatordest = null;

            foreach (var node in routesPlanResponse_B.nodes)
            {
                var position = _repository.Positions.GetByPositionId(node.positionId);

                // ELEVATOR 는 position 없이 처리할 수 있으므로 예외
                if (position == null && node.nodeType.ToUpper() != nameof(NodeType.ELEVATOR)) continue;

                switch (node.nodeType.ToUpper())
                {
                    case nameof(NodeType.WAYPOINT):
                        if (node.positionId == jobSource.positionId)
                        {
                            if (job.type == nameof(JobType.TRANSPORT)) seq = template_GroupMission(job, position, worker, seq, nameof(MissionsTemplateGroup.TRANSPORTPICK));
                            else seq = template_GroupMission(job, position, worker, seq, nameof(MissionsTemplateGroup.MANUALTRANSPORTPICK));
                        }
                        else if (node.positionId == jobDestination.positionId)
                        {
                            if (job.type == nameof(JobType.TRANSPORT)) seq = template_GroupMission(job, position, worker, seq, nameof(MissionsTemplateGroup.TRANSPORTDROP));
                            else seq = template_GroupMission(job, position, worker, seq, nameof(MissionsTemplateGroup.MANUALTRANSPORTDROP));
                        }
                        else
                        {
                            seq = template_SingleMission(job, position, worker, seq, nameof(MissionTemplateType.MOVE), nameof(MissionTemplateSubType.STOPOVERMOVE));
                        }
                        break;

                    case nameof(NodeType.TRAFFIC):

                        if (node.positionId == jobSource.positionId)
                        {
                            if (job.type == nameof(JobType.TRANSPORT)) seq = template_GroupMission(job, position, worker, seq, nameof(MissionsTemplateGroup.TRANSPORTPICK));
                            else seq = template_GroupMission(job, position, worker, seq, nameof(MissionsTemplateGroup.MANUALTRANSPORTPICK));

                            seq = template_SingleMission(job, position, worker, seq, nameof(MissionTemplateType.ACTION), nameof(MissionTemplateSubType.TRAFFIC));
                        }
                        else if (node.positionId == jobDestination.positionId)
                        {
                            if (job.type == nameof(JobType.TRANSPORT)) seq = template_GroupMission(job, position, worker, seq, nameof(MissionsTemplateGroup.TRANSPORTDROP));
                            else seq = template_GroupMission(job, position, worker, seq, nameof(MissionsTemplateGroup.MANUALTRANSPORTDROP));
                        }
                        else
                        {
                            seq = template_GroupMission(job, position, worker, seq, nameof(MissionsTemplateGroup.TRAFFIC));
                        }
                        break;

                    case nameof(NodeType.AUTODOOROPEN):
                        seq = template_GroupMission(job, position, worker, seq, nameof(MissionsTemplateGroup.AUTODOOROPEN));
                        break;

                    case nameof(NodeType.AUTODOORCLOSE):
                        seq = template_GroupMission(job, position, worker, seq, nameof(MissionsTemplateGroup.AUTODOORCLOSE));
                        break;

                    case nameof(NodeType.AUTODOOROPENREQUEST):
                        seq = template_GroupMission(job, position, worker, seq, nameof(MissionsTemplateGroup.AUTODOOROPENREQUEST));
                        break;

                    case nameof(NodeType.AUTODOORCLOSEREQUEST):
                        seq = template_GroupMission(job, position, worker, seq, nameof(MissionsTemplateGroup.AUTODOORCLOSEREQUEST));
                        break;

                    case nameof(NodeType.AIRSHOWER):
                        seq = template_GroupMission(job, position, worker, seq, nameof(MissionsTemplateGroup.AIRSHOWER));
                        break;

                    case nameof(NodeType.ELEVATOR):
                        // 3) ELEVATOR 노드 → Subscribe_Elevator 그룹 (한 번만)
                        if (Segment_B_ElevatorSource == null)
                        {
                            Segment_B_ElevatorSource = position;
                            seq = template_GroupMission(job, Segment_B_ElevatorSource, worker, seq, nameof(MissionsTemplateGroup.ELEVATORSOURCE));
                            //EventLogger.Info($"[ASSIGN][ASSIGN][ELEVATOR-GROUP][ELEVATORSOURCE] workerName={worker.name}, workerId={worker.id}, jobSecondId={job.guid}, seq={seq}");
                        }
                        else if (Segment_B_ElevatorSource != null)
                        {
                            Segment_B_Elevatordest = position;
                            seq = template_GroupMission(job, Segment_B_Elevatordest, worker, seq, nameof(MissionsTemplateGroup.ELEVATORDEST));
                            //EventLogger.Info($"[ASSIGN][ASSIGN][ELEVATOR-GROUP][ELEVATORDEST] workerName={worker.name}, workerId={worker.id}, jobSecondId={job.guid}, seq={seq}");
                        }
                        break;

                    case nameof(NodeType.CHARGER):

                        seq = template_SingleMission(job, position, worker, seq, nameof(MissionTemplateType.MOVE), nameof(MissionTemplateSubType.CHARGERMOVE));

                        break;
                }

                reValue = true;
            }
            return reValue;
        }

        // CHARGE / WAIT Job 미션 구성
        // WorkerStart → JobDestination 한 번만
        private bool BuildChargeWaitMissions(Job job, Worker worker, ServiceApi resource, Position workerStart, Position jobDestination
                                            , int seq)
        {
            bool reValue = false;
            var routesPlanRequest = _mapping.RoutesPlanas.Request(workerStart.positionId, jobDestination.positionId);
            var routesPlanResponse = resource.Api.Post_Routes_Plan_Async(routesPlanRequest).Result;

            if (routesPlanResponse == null)
            {
                EventLogger.Warn($"[ChargeOrWait][PLAN][NO_ROUTE] response is null (path not found?) jobId={job?.guid}, from={workerStart?.positionId}, fromName={workerStart?.name}" +
                                 $", to={jobDestination?.positionId},toName = {jobDestination?.name} ,WorkerId={worker?.id}, WorkerName={worker.name}");
                return reValue;
            }

            if (routesPlanResponse.nodes == null)
            {
                EventLogger.Warn($"[ChargeOrWait][PLAN][NO_ROUTE] nodes is null/empty (path not found) jobId={job?.guid}, from={workerStart?.positionId}, fromName={workerStart?.name}" +
                                 $", to={jobDestination?.positionId},toName = {jobDestination?.name} ,WorkerId={worker?.id}, WorkerName={worker.name}");

                return reValue;
            }

            Position ElevatorSource = null;
            Position Elevatordest = null;

            foreach (var node in routesPlanResponse.nodes)
            {
                var position = _repository.Positions.GetByPositionId(node.positionId);

                if (position == null && node.nodeType.ToUpper() != nameof(NodeType.ELEVATOR)) continue;

                switch (node.nodeType.ToUpper())
                {
                    case nameof(NodeType.WAYPOINT):
                        if (node.positionId == workerStart.positionId)
                        {
                            seq = template_SingleMission(job, position, worker, seq, nameof(MissionTemplateType.MOVE), nameof(MissionTemplateSubType.SOURCEMOVE));
                        }
                        else if (node.positionId == jobDestination.positionId)
                        {
                            seq = template_SingleMission(job, position, worker, seq, nameof(MissionTemplateType.MOVE), nameof(MissionTemplateSubType.DESTINATIONMOVE));
                        }
                        else
                        {
                            seq = template_SingleMission(job, position, worker, seq, nameof(MissionTemplateType.MOVE), nameof(MissionTemplateSubType.STOPOVERMOVE));
                        }
                        break;

                    case nameof(NodeType.TRAFFIC):
                        if (node.positionId == workerStart.positionId)
                        {
                            seq = template_SingleMission(job, position, worker, seq, nameof(MissionTemplateType.MOVE), nameof(MissionTemplateSubType.SOURCEMOVE));
                            seq = template_SingleMission(job, position, worker, seq, nameof(MissionTemplateType.ACTION), nameof(MissionTemplateSubType.TRAFFIC));
                        }
                        else if (node.positionId == jobDestination.positionId)
                        {
                            seq = template_SingleMission(job, position, worker, seq, nameof(MissionTemplateType.MOVE), nameof(MissionTemplateSubType.DESTINATIONMOVE));
                        }
                        else
                        {
                            seq = template_GroupMission(job, position, worker, seq, nameof(MissionsTemplateGroup.TRAFFIC));
                        }
                        break;

                    case nameof(NodeType.AUTODOOROPEN):
                        seq = template_GroupMission(job, position, worker, seq, nameof(MissionsTemplateGroup.AUTODOOROPEN));
                        break;

                    case nameof(NodeType.AUTODOORCLOSE):
                        seq = template_GroupMission(job, position, worker, seq, nameof(MissionsTemplateGroup.AUTODOORCLOSE));
                        break;

                    case nameof(NodeType.AUTODOOROPENREQUEST):
                        seq = template_GroupMission(job, position, worker, seq, nameof(MissionsTemplateGroup.AUTODOOROPENREQUEST));
                        break;

                    case nameof(NodeType.AUTODOORCLOSEREQUEST):
                        seq = template_GroupMission(job, position, worker, seq, nameof(MissionsTemplateGroup.AUTODOORCLOSEREQUEST));
                        break;

                    case nameof(NodeType.AIRSHOWER):
                        seq = template_GroupMission(job, position, worker, seq, nameof(MissionsTemplateGroup.AIRSHOWER));
                        break;

                    case nameof(NodeType.ELEVATOR):
                        if (ElevatorSource == null)
                        {
                            ElevatorSource = position;
                            seq = template_GroupMission(job, ElevatorSource, worker, seq, nameof(MissionsTemplateGroup.ELEVATORSOURCE));
                        }
                        else if (ElevatorSource != null)
                        {
                            Elevatordest = position;
                            seq = template_GroupMission(job, Elevatordest, worker, seq, nameof(MissionsTemplateGroup.ELEVATORDEST));
                        }
                        break;

                    case nameof(NodeType.CHARGER):

                        seq = template_SingleMission(job, position, worker, seq, nameof(MissionTemplateType.MOVE), nameof(MissionTemplateSubType.CHARGERMOVE));

                        break;
                }

                reValue = true;
            }
            return reValue;
        }
    }
}