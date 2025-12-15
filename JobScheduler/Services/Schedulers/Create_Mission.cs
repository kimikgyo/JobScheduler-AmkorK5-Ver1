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
        ///  1) Worker 기준 실제 출발 위치(Position) 계산
        ///  2) Job 출발지/목적지 Position 결정
        ///     - Job.sourceId 가 없으면 Worker 위치가 출발지 역할
        ///  3) Job 타입(TRANSPORT / CHARGE / WAIT)에 따라 Mission 구성 방식 분리
        ///  4) Resource API 에 이동 경로(RoutePlan) 요청
        ///  5) RoutePlan 노드를 순회하며 Mission 생성
        ///     - PICK / DROP / ELEVATOR / TRAFFIC / MOVE
        ///  6) 생성 완료 후 Job/Order 상태 업데이트
        ///
        /// [특징]
        ///  - 항상 Worker 기준 동선 설계 (재할당 시 로봇 현재 위치를 시작점으로 활용 가능)
        ///  - Mission 동적 구성: 지도 변경, 경로 변경에도 자동 대응
        ///  - Job 출발지가 없을 경우에도 유연하게 동작
        ///
        /// [예시 수행 시나리오]
        ///   Worker 위치 → Job Pick 위치 → Elevator → Destination → Drop
        ///
        /// [확장]
        ///  - 재할당 Job 미션 생성 시, 시작 위치 override 가능
        ///  - Mission 재정렬, Mission Skip 등 고급 미션 관리 구조와 연계 가능
        /// </summary>
        private void Create_Mission(Job job, Worker worker)
        {
            if (job == null) return;
            if (worker == null) return;

            int seq = 1;

            // 1) 외부 Route API 핸들
            var resource = _repository.ServiceApis.GetAll().FirstOrDefault(s => s.type == "Resource");
            if (resource == null) return;

            // 2) Worker 기준 시작 위치 + Job 출발/목적지 계산
            var workerStart = GetWorkerStartPosition(worker);
            if (workerStart == null) return;

            var jobSource = GetJobSourcePosition(job, workerStart);
            if (jobSource == null) return;

            var jobDestination = GetJobDestinationPosition(job);
            if (jobDestination == null) return;

            // 3) Job 타입별로 미션 구성 위임
            switch (job.type)
            {
                case nameof(JobType.TRANSPORT):
                case nameof(JobType.TRANSPORT_SLURRY_SUPPLY):
                case nameof(JobType.TRANSPORT_SLURRY_RECOVERY):
                case nameof(JobType.TRANSPORT_CHEMICAL_SUPPLY):
                case nameof(JobType.TRANSPORT_CHEMICAL_RECOVERY):
                case nameof(JobType.TRANSPORT_AICERO_SUPPLY):
                case nameof(JobType.TRANSPORT_AICERO_RESOVERY):

                    BuildTransportMissions(job, worker, resource, workerStart, jobSource, jobDestination, seq);
                    break;

                case nameof(JobType.CHARGE):
                case nameof(JobType.WAIT):

                    BuildChargeWaitMissions(job, worker, resource, workerStart, jobDestination, seq);
                    break;
            }

            // 4) Job / Order 상태 업데이트 (기존 로직 유지)
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

        // 0) Worker 기준 시작 위치 구하기
        //    - 최초 할당: mapId 기준 가장 가까운 Position
        //    - (나중에 재할당 시에는 별도 인자나 WorkerState에서 현재 위치를 가져오도록 확장 가능)
        private Position GetWorkerStartPosition(Worker worker)
        {
            if (worker == null) return null;

            // 해당 워커가 있는 맵의 모든 포지션 가져오기
            var positions = _repository.Positions.MiR_GetByMapId(worker.mapId).Where(r => r.nodeType != nameof(NodeType.WORK) && r.nodeType != nameof(NodeType.ELEVATOR)).ToList();
            if (positions == null || positions.Count == 0) return null;

            // 워커 기준 가장 가까운 포지션 선택
            var nearest = _repository.Positions.FindNearestWayPoint(worker, positions).FirstOrDefault();
            return nearest;
        }

        // 1) Job 출발지 Position 구하기
        //    - job.sourceId 가 있으면: 그 Position
        //    - 없으면: WorkerStart 가 곧 출발지
        private Position GetJobSourcePosition(Job job, Position workerStart)
        {
            if (job == null) return null;

            // Job 출발지가 없는 경우 → Worker 위치가 출발지
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

        // TRANSPORT 계열 Job 미션 구성
        // Segment A: WorkerStart → JobSource (MOVE만)
        // Segment B: JobSource   → JobDestination (PICK / DROP / ELEVATOR / TRAFFIC / MOVE)
        private void BuildTransportMissions(Job job, Worker worker, ServiceApi resource, Position workerStart, Position jobSource, Position jobDestination
                                           , int seq)
        {
            // A) Segment A: WorkerStart → JobSource
            if (workerStart.positionId != jobSource.positionId)
            {
                var routesA = resource.Api.Post_Routes_Plan_Async(_mapping.RoutesPlanas.Request(workerStart.positionId, jobSource.positionId)).Result;

                if (routesA == null) return;
                if (routesA.nodes == null) return;

                Position Segment_A_ElevatorSource = null;
                Position Segment_A_Elevatordest = null;

                foreach (var node in routesA.nodes)
                {
                    var position = _repository.Positions.GetByPositionId(node.positionId);
                    if (position == null) continue;
                    if (position.id == jobSource.positionId) continue;
                    // 3) ELEVATOR 노드 → Elevator 그룹 (한 번만)
                    if (node.nodeType.ToUpper() == nameof(NodeType.ELEVATOR))
                    {
                        if (Segment_A_ElevatorSource == null)
                        {
                            Segment_A_ElevatorSource = position;
                            seq = create_GroupMission(job, Segment_A_ElevatorSource, worker, seq, nameof(MissionsTemplateGroup.ELEVATORSOURCE));
                            //EventLogger.Info($"[ASSIGN][ASSIGN][ELEVATOR-GROUP][ELEVATORSOURCE] workerName={worker.name}, workerId={worker.id}, jobSecondId={job.guid}, seq={seq}");
                        }
                        else if (Segment_A_ElevatorSource != null)
                        {
                            Segment_A_Elevatordest = position;
                            seq = create_GroupMission(job, Segment_A_Elevatordest, worker, seq, nameof(MissionsTemplateGroup.ELEVATORDEST));
                            //EventLogger.Info($"[ASSIGN][ASSIGN][ELEVATOR-GROUP][ELEVATORDEST] workerName={worker.name}, workerId={worker.id}, jobSecondId={job.guid}, seq={seq}");
                        }
                    }
                    // 4) TRAFFIC → TRAFFIC 그룹
                    else if (node.nodeType.ToUpper() == nameof(NodeType.TRAFFIC))
                    {
                        seq = create_GroupMission(job, position, worker, seq, nameof(MissionsTemplateGroup.TRAFFIC));
                    }
                    else
                    {
                        // 순수 이동 구간이므로 MOVE(STOPOVERMOVE)만 생성
                        seq = create_SingleMission(job, position, worker, seq, nameof(MissionTemplateType.MOVE), nameof(MissionTemplateSubType.STOPOVERMOVE));
                    }
                }
            }

            // B) Segment B: JobSource → JobDestination
            var routesB = resource.Api.Post_Routes_Plan_Async(_mapping.RoutesPlanas.Request(jobSource.positionId, jobDestination.positionId)).Result;

            if (routesB == null) return;
            if (routesB.nodes == null) return;

            Position Segment_B_ElevatorSource = null;
            Position Segment_B_Elevatordest = null;

            foreach (var node in routesB.nodes)
            {
                var position = _repository.Positions.GetByPositionId(node.positionId);

                // ELEVATOR 는 position 없이 처리할 수 있으므로 예외
                if (position == null && node.nodeType.ToUpper() != nameof(NodeType.ELEVATOR)) continue;

                // 1) 출발지 → PICK 그룹
                if (node.positionId == jobSource.positionId)
                {
                    seq = create_GroupMission(job, position, worker, seq, nameof(MissionsTemplateGroup.PICK));
                }
                // 2) 목적지 → DROP 그룹
                else if (node.positionId == jobDestination.positionId)
                {
                    seq = create_GroupMission(job, position, worker, seq, nameof(MissionsTemplateGroup.DROP));
                }

                // 3) ELEVATOR 노드 → Elevator 그룹 (한 번만)
                else if (node.nodeType.ToUpper() == nameof(NodeType.ELEVATOR))
                {
                    if (Segment_B_ElevatorSource == null)
                    {
                        Segment_B_ElevatorSource = position;
                        seq = create_GroupMission(job, Segment_B_ElevatorSource, worker, seq, nameof(MissionsTemplateGroup.ELEVATORSOURCE));
                        //EventLogger.Info($"[ASSIGN][ASSIGN][ELEVATOR-GROUP][ELEVATORSOURCE] workerName={worker.name}, workerId={worker.id}, jobSecondId={job.guid}, seq={seq}");
                    }
                    else if (Segment_B_ElevatorSource != null)
                    {
                        Segment_B_Elevatordest = position;
                        seq = create_GroupMission(job, Segment_B_Elevatordest, worker, seq, nameof(MissionsTemplateGroup.ELEVATORDEST));
                        //EventLogger.Info($"[ASSIGN][ASSIGN][ELEVATOR-GROUP][ELEVATORDEST] workerName={worker.name}, workerId={worker.id}, jobSecondId={job.guid}, seq={seq}");
                    }
                }
                // 4) TRAFFIC → TRAFFIC 그룹
                else if (node.nodeType.ToUpper() == nameof(NodeType.TRAFFIC))
                {
                    seq = create_GroupMission(job, position, worker, seq, nameof(MissionsTemplateGroup.TRAFFIC));
                }
                // 5) 나머지 → MOVE(STOPOVERMOVE)
                else
                {
                    seq = create_SingleMission(job, position, worker, seq, nameof(MissionTemplateType.MOVE), nameof(MissionTemplateSubType.STOPOVERMOVE));
                }
            }
        }

        // CHARGE / WAIT Job 미션 구성
        // WorkerStart → JobDestination 한 번만
        private void BuildChargeWaitMissions(Job job, Worker worker, ServiceApi resource, Position workerStart, Position jobDestination
                                            , int seq)
        {
            var routes = resource.Api.Post_Routes_Plan_Async(_mapping.RoutesPlanas.Request(workerStart.positionId, jobDestination.positionId)).Result;

            if (routes == null) return;
            if (routes.nodes == null) return;

            Position ElevatorSource = null;
            Position Elevatordest = null;

            foreach (var node in routes.nodes)
            {
                var position = _repository.Positions.GetByPositionId(node.positionId);

                if (position == null && node.nodeType.ToUpper() != nameof(NodeType.ELEVATOR)) continue;

                // 1) 시작 위치 → SOURCEMOVE
                if (node.positionId == workerStart.positionId)
                {
                    seq = create_SingleMission(job, position, worker, seq, nameof(MissionTemplateType.MOVE), nameof(MissionTemplateSubType.SOURCEMOVE));
                }
                // 2) 목적지 → DESTINATIONMOVE
                else if (node.positionId == jobDestination.positionId)
                {
                    seq = create_SingleMission(job, position, worker, seq, nameof(MissionTemplateType.MOVE), nameof(MissionTemplateSubType.DESTINATIONMOVE));
                }
                // 3) ELEVATOR 그룹 (한 번만)
                else if (node.nodeType.ToUpper() == nameof(NodeType.ELEVATOR))
                {
                    if (ElevatorSource == null)
                    {
                        ElevatorSource = position;
                        seq = create_GroupMission(job, ElevatorSource, worker, seq, nameof(MissionsTemplateGroup.ELEVATORSOURCE));
                    }
                    else if (ElevatorSource != null)
                    {
                        Elevatordest = position;
                        seq = create_GroupMission(job, Elevatordest, worker, seq, nameof(MissionsTemplateGroup.ELEVATORDEST));
                    }
                }
                // 4) TRAFFIC → TRAFFIC 그룹
                else if (node.nodeType.ToUpper() == nameof(NodeType.TRAFFIC))
                {
                    seq = create_GroupMission(job, position, worker, seq, nameof(MissionsTemplateGroup.TRAFFIC));
                }
                // 5) 나머지 → STOPOVERMOVE
                else
                {
                    seq = create_SingleMission(job, position, worker, seq, nameof(MissionTemplateType.MOVE), nameof(MissionTemplateSubType.STOPOVERMOVE));
                }
            }
        }
    }
}