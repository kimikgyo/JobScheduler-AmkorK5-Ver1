using Azure;
using Common.Models.Jobs;
using Common.Templates;

namespace JOB.Services
{
    public partial class SchedulerService
    {
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
                                updateStateMission(mission, nameof(MissionState.SKIPPED), "[skipMission]", true);
                                EventLogger.Info($"[PostMission][{nameof(Service.WORKER)}][SKIPPED], PositionId = {worker.PositionId}, PositionName = {worker.PositionName}, MissionId = {mission.guid}, AssignedWorkerId = {mission.assignedWorkerId}");
                                completed = true;
                            }
                        }
                    }
                    break;

                case nameof(MissionType.ACTION):
                    //if (mission.subType == nameof(MissionSubType.DOORCLOSE))
                    //{
                    //    var elevatorMoveMissions = _repository.Missions.GetAll().Where(r => r.subType == nameof(MissionSubType.ELEVATORWAITMOVE)
                    //                                                                    || r.subType == nameof(MissionSubType.ELEVATORENTERMOVE)
                    //                                                                    || r.subType == nameof(MissionSubType.ELEVATOREXITMOVE)
                    //                                                                    || r.subType == nameof(MissionSubType.RIGHTTURN)
                    //                                                                    || r.subType == nameof(MissionSubType.LEFTTURN)
                    //                                                                    || r.subType == nameof(MissionSubType.SWITCHINGMAP)).ToList();
                    //    var runmission = _repository.Missions.GetByRunMissions(elevatorMoveMissions).FirstOrDefault();

                    //    if (runmission != null)
                    //    {
                    //        updateStateMission(mission, nameof(MissionState.SKIPPED), true);
                    //        EventLogger.Info($"[PostMission][{nameof(Service.ELEVATOR)}][SKIPPED], MissionId = {mission.guid}, missionName = {mission.name} ,AssignedWorkerId = {mission.assignedWorkerId}");
                    //        completed = true;
                    //    }
                    //}
                    break;
            }

            return completed;
        }

        /// <summary>
        /// 미션 생성 완료 후 호출:
        /// - AUTODOOROPEN ↔ AUTODOORCLOSE 페어 보장
        /// - AUTODOOROPENREQUEST ↔ AUTODOORCLOSEREQUEST 페어 보장
        /// 짝이 안 맞는 미션은 state=SKIP
        /// </summary>
        public void PostProcess_SetSkip_AutoDoorPairs()
        {
            var jobs = _repository.Jobs.GetAll();
            if (jobs == null || jobs.Count == 0)
            {
                EventLogger.Warn($"[AUTODOOR][POST][SKIP] jobs is null Or empty");
                return;
            }
            foreach (var job in jobs)
            {
                var missions = _repository.Missions.GetByJobId(job.guid).OrderBy(r => r.sequence).ToList();
                if (missions == null || missions.Count == 0)
                {
                    EventLogger.Warn($"[AUTODOOR][POST][SKIP] missions is null Or empty");
                    return;
                }

                // (선택) 실행 순서 보장 필요하면 정렬해서 리스트로 만들어 처리
                // sequence가 실제 실행 순서라면 아래처럼 사용:
                // var ordered = missions.Where(x => x != null).OrderBy(x => x.sequence).ToList();
                // ApplyPairSkipRule(ordered, ...);  // 그리고 ordered의 state 변경이 원본에도 반영되게 참조형이면 OK

                ApplyPairSkipRule(missions, nameof(MissionSubType.AUTODOOROPEN), nameof(MissionSubType.AUTODOORCLOSE), "OPEN_CLOSE");
                ApplyPairSkipRule(missions, nameof(MissionSubType.AUTODOOROPENREQUEST), nameof(MissionSubType.AUTODOORCLOSEREQUEST), "OPENREQ_CLOSEREQ");
            }
        }

        /// <summary>
        /// 페어 규칙 적용(공용)
        /// - openType/closeType 페어를 강제하고, 짝이 안 맞는 미션은 state=SKIP 처리
        /// </summary>
        private void ApplyPairSkipRule(List<Mission> missions, string openType, string closeType, string tag)
        {
            if (missions == null || missions.Count == 0) return;

            EventLogger.Info($"[AUTODOOR][PAIR][BEGIN] tag={tag}, openType={openType}, closeType={closeType}, missionsCount={missions.Count}");

            int pendingOpenIdx = -1; // 아직 CLOSE를 못 만난 OPEN의 인덱스

            for (int i = 0; i < missions.Count; i++)
            {
                var m = missions[i];

                if (m == null)
                {
                    EventLogger.Warn($"[AUTODOOR][PAIR][NULL_MISSION] tag={tag}, idx={i}");
                    continue;
                }

                // 이미 SKIP이면 건너뜀
                if (m.state == nameof(MissionState.SKIPPED)) continue;

                // --- OPEN 처리 ---
                if (m.subType == openType)
                {
                    // 이전 OPEN이 아직 CLOSE를 못 만났는데 또 OPEN이 나왔다면
                    // => "OPEN 이후 다음 OPEN 전까지 CLOSE가 없었다" = 이전 OPEN은 규칙 위반 -> SKIP
                    if (pendingOpenIdx >= 0)
                    {
                        var prevOpen = missions[pendingOpenIdx];

                        // null 방어
                        if (prevOpen != null && prevOpen.state != nameof(MissionState.SKIPPED))
                        {
                            // (선택) INPROGRESS/COMPLETED는 건드리지 않게 하고 싶으면 여기서 조건 추가 가능
                            // if (prevOpen.state == MissionState.INPROGRESS || prevOpen.state == MissionState.COMPLETED) { ... }

                            updateStateMission(prevOpen, nameof(MissionState.SKIPPED), "[ApplyPairSkipRule]", true);

                            EventLogger.Warn(
                                $"[AUTODOOR][PAIR][OPEN_SKIP_NO_CLOSE_BEFORE_NEXT_OPEN] tag={tag}, " +
                                $"skipIdx={pendingOpenIdx}, skipSeq={prevOpen.sequence}, " +
                                $"newOpenIdx={i}, newOpenSeq={m.sequence}");
                        }
                    }

                    // 현재 OPEN을 pending으로 지정
                    pendingOpenIdx = i;

                    EventLogger.Info(
                        $"[AUTODOOR][PAIR][OPEN_SEEN] tag={tag}, idx={i}, seq={m.sequence}, state={m.state}");

                    continue;
                }

                // --- CLOSE 처리 ---
                if (m.subType == closeType)
                {
                    // OPEN 없이 CLOSE가 먼저 나왔다면(첫 미션이 CLOSE인 케이스 포함)
                    // => 규칙 위반 -> CLOSE SKIP
                    if (pendingOpenIdx < 0)
                    {
                        // (선택) INPROGRESS/COMPLETED는 건드리지 않게 하고 싶으면 여기서 조건 추가 가능
                        updateStateMission(m, nameof(MissionState.SKIPPED), "[ApplyPairSkipRule]", true);
                        EventLogger.Warn(
                            $"[AUTODOOR][PAIR][CLOSE_SKIP_NO_OPEN] tag={tag}, idx={i}, seq={m.sequence}, stateBefore=NOT_SKIP");

                        continue;
                    }

                    // pending OPEN이 있으므로 페어 성립: OPEN과 CLOSE 둘 다 유지(=SKIP 안함)
                    var open = missions[pendingOpenIdx];

                    EventLogger.Info(
                        $"[AUTODOOR][PAIR][PAIR_OK] tag={tag}, openIdx={pendingOpenIdx}, openSeq={(open != null ? open.sequence : -1)}, " +
                        $"closeIdx={i}, closeSeq={m.sequence}");

                    pendingOpenIdx = -1;
                    continue;
                }

                // --- 다른 subtype은 관심 없음 ---
            }

            // 루프가 끝났는데 pending OPEN이 남아있으면 => 마지막까지 CLOSE 못 만남 -> OPEN SKIP
            if (pendingOpenIdx >= 0)
            {
                var lastOpen = missions[pendingOpenIdx];
                if (lastOpen != null && lastOpen.state != nameof(MissionState.SKIPPED))
                {
                    updateStateMission(lastOpen, nameof(MissionState.SKIPPED), "[ApplyPairSkipRule]", true);
                    EventLogger.Warn(
                        $"[AUTODOOR][PAIR][OPEN_SKIP_END_NO_CLOSE] tag={tag}, idx={pendingOpenIdx}, seq={lastOpen.sequence}");
                }
            }

            EventLogger.Info($"[AUTODOOR][PAIR][END] tag={tag}, openType={openType}, closeType={closeType}");
        }
    }
}