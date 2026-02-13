using Common.Models.Bases;
using Common.Models.Jobs;
using System.Reflection;

namespace JOB.Services
{
    public partial class SchedulerService
    {
        //// (전역/필드) 로그 스로틀(Throttle) 용도
        //// - 300ms 루프에서 같은 원인 로그가 무한히 찍히는 걸 막는다.
        //// - throttleKey(예: "RUNSKIP|workerId|state") 별로 마지막 로그 시간을 저장한다.
        //private static readonly object _throttleLock = new object();

        //private static readonly Dictionary<string, DateTime> _throttleMap = new Dictionary<string, DateTime>();

        ///// <summary>
        ///// 같은 원인(키)에 대해 intervalSeconds 동안 1번만 로그를 찍도록 허용
        ///// - true  : 이번에는 로그 찍어도 됨
        ///// - false : 아직 intervalSeconds 안 지났으니 로그 스킵
        ///// </summary>
        //private static bool ShouldLog(string throttleKey, int intervalSeconds)
        //{
        //    var now = DateTime.Now;

        //    lock (_throttleLock)
        //    {
        //        // 이전에 찍힌 기록이 있으면, 시간 간격을 체크
        //        if (_throttleMap.TryGetValue(throttleKey, out var last))
        //        {
        //            // 아직 intervalSeconds가 안 지났으면 로그를 찍지 않는다.
        //            if ((now - last).TotalSeconds < intervalSeconds)
        //                return false;
        //        }

        //        // 로그를 찍는다고 결정되면, 마지막 로그 시간을 갱신
        //        _throttleMap[throttleKey] = now;
        //        return true;
        //    }
        //}
        //private void MissionPostScheduler()
        //{
        //    // =========================
        //    // 0) 배터리 Setting 정보 조회 (현 코드 유지)
        //    // =========================
        //    var batterySetting = _repository.Battery.GetAll();

        //    // =========================
        //    // 1) 작업 가능한 Active Worker 순회
        //    // - 300ms 루프에서 worker 수 만큼 반복되므로
        //    //   "항상 로그"는 절대 금지 (로그 폭발)
        //    // =========================
        //    foreach (var worker in _repository.Workers.MiR_GetByActive())
        //    {
        //        // =========================
        //        // 2) (현 코드 유지) 충전 파라메터 초기화
        //        // - 현재 예시 코드에서는 사용하지 않지만, 기존 구조 유지를 위해 둠
        //        // =========================
        //        Parameter ChargeEquest = null;

        //        // =========================
        //        // 3) 현재 Worker에게 할당된 Mission 목록 조회
        //        // - sequence 기준 오름차순 정렬 (앞 순서 먼저 처리)
        //        // - 미션이 아예 없으면 할 게 없으므로 continue
        //        // =========================
        //        var missions = _repository.Missions
        //            .GetByAssignedWorkerId(worker.id)
        //            .OrderBy(r => r.sequence)
        //            .ToList();

        //        if (missions == null || missions.Count == 0)
        //        {
        //            // (선택) 너무 자주 찍히므로 기본은 로그 생략
        //            continue;
        //        }

        //        // =========================
        //        // 4) Middleware 정보 조회 (현 코드 유지)
        //        // - 현재 함수 내에서 middleware 변수를 사용하지 않지만
        //        //   기존 코드 흐름을 유지하기 위해 그대로 둠
        //        // =========================
        //        var middleware = _repository.Middlewares.GetByWorkerId(worker.id);

        //        // =========================
        //        // 5) "진행중(runmission)" 미션 확인
        //        // - runmission이 존재하면, WAITING 미션을 보내면 안 되는 정책이라면
        //        //   여기서 스킵된다.
        //        // - 문제는: runmission이 남아있으면 WAITING이 있어도 계속 스킵됨
        //        // - 따라서 "스킵될 때만" throttle 로그를 남겨 원인을 추적한다.
        //        // =========================
        //        var runmission = _repository.Missions.GetByRunMissions(missions).FirstOrDefault();

        //        if (runmission != null)
        //        {
        //            // ✅ throttleKey: worker + run state 기준으로 묶어서 N초에 1번만 경고 로그
        //            // - 같은 워커가 같은 runstate 때문에 계속 막히는지 확인 가능
        //            var key = $"RUNSKIP|{worker.id}|{runmission.state}";

        //            // ✅ 5초에 1번만 찍히게 제한(필요 시 10초/30초로 올려도 됨)
        //            if (ShouldLog(key, 2))
        //            {
        //                EventLogger.Warn(
        //                    $"[PostScheduler][SKIP_RUNMISSION] " +
        //                    $"worker={worker.id}({worker.name}) workerState={worker.state} isMiddleware={worker.isMiddleware} " +
        //                    $"runGuid={runmission.guid} runState={runmission.state} runName={runmission.name} runSeq={runmission.sequence}"
        //                );
        //            }

        //            // runmission이 있으면 이 워커는 이번 틱에서는 처리하지 않음
        //            continue;
        //        }

        //        // =========================
        //        // 6) Worker 조건 확인
        //        // - 미들웨어 사용 중이고, Worker 상태가 IDLE일 때만 전송 시도
        //        // =========================
        //        bool c1 = worker.isMiddleware == true;
        //        bool c2 = worker.state == nameof(WorkerState.IDLE);

        //        if (!(c1 && c2))
        //        {
        //            // 조건 미충족은 흔히 발생하므로 기본은 로그 생략
        //            continue;
        //        }

        //        // =========================
        //        // 7) ✅ 정책 반영: WAITING 상태 미션만 전송 대상
        //        // - "간헐적으로 안 보내짐" 원인 파악을 위해
        //        //   반드시 WAITING만 집어서 전송하도록 단순화한다.
        //        // =========================
        //        var mission = missions
        //            .Where(m => m != null
        //                     && m.jobId != null
        //                     && m.state == nameof(MissionState.WAITING))
        //            .OrderBy(m => m.sequence)
        //            .FirstOrDefault();

        //        if (mission == null)
        //        {
        //            // WAITING이 없을 때만, throttle로 가끔 찍어서 확인 가능
        //            // - "정말 WAITING이 없는지" vs "쿼리/업데이트 타이밍 문제인지" 구분
        //            var key = $"NOWAITING|{worker.id}";
        //            if (ShouldLog(key, 2))
        //            {
        //                EventLogger.Info($"[PostScheduler][NO_WAITING] worker={worker.id}({worker.name}) missions={missions.Count}");
        //            }
        //            continue;
        //        }

        //        // =========================
        //        // 8) Job 조회 및 Cancel/Terminate 여부 확인
        //        // - terminateState == null 인 경우만 전송 가능
        //        // - 사용자가 이미 이 조건이 맞다고 확인했지만,
        //        //   "간헐적"일 수 있으므로 "막힐 때만" throttle 로그를 남겨둔다.
        //        // =========================
        //        var job = _repository.Jobs.GetByid(mission.jobId);

        //        if (job == null)
        //        {
        //            // job이 null이면 전송 불가. 이 경우는 데이터 정합성 이슈일 수 있음.
        //            var key = $"JOBNULL|{worker.id}|{mission.jobId}";
        //            if (ShouldLog(key, 2))
        //            {
        //                EventLogger.Warn(
        //                    $"[PostScheduler][SKIP_JOB_NULL] worker={worker.id}({worker.name}) " +
        //                    $"missionGuid={mission.guid} missionSeq={mission.sequence} jobId={mission.jobId}"
        //                );
        //            }
        //            continue;
        //        }

        //        if (job.terminateState != null)
        //        {
        //            // Cancel/Terminate 중이면 전송하지 않음
        //            var key = $"JOBTERM|{worker.id}|{job.terminateState}";
        //            if (ShouldLog(key, 2))
        //            {
        //                EventLogger.Warn(
        //                    $"[PostScheduler][SKIP_JOB_TERMINATING] worker={worker.id}({worker.name}) " +
        //                    $"missionGuid={mission.guid} jobId={job.guid} terminateState={job.terminateState} jobState={job.state}"
        //                );
        //            }
        //            continue;
        //        }

        //        // =========================
        //        // 9) skipMission 체크
        //        // - PositionId/점유/구역/정책 조건 등에 의해 전송을 막을 수 있음
        //        // - "간헐적"으로 true가 나오는 경우가 많으니, true일 때만 throttle 로그
        //        // =========================
        //        if (skipMission(mission, worker))
        //        {
        //            var key = $"SKIPMISSION|{worker.id}|{mission.guid}";
        //            if (ShouldLog(key, 2))
        //            {
        //                EventLogger.Warn(
        //                    $"[PostScheduler][SKIP_BY_SKIPMISSION] worker={worker.id}({worker.name}) " +
        //                    $"missionGuid={mission.guid} missionState={mission.state} missionName={mission.name} seq={mission.sequence}"
        //                );
        //            }
        //            continue;
        //        }

        //        // =========================
        //        // 10) 최종 전송(postMission)
        //        // - "호출했는데도 안 들어간다"는 케이스를 잡기 위해
        //        //   try/catch + 최소 로그(시도/완료/예외)를 남긴다.
        //        // - 단, 이 로그도 너무 많을 수 있으므로 throttle를 걸 수 있다.
        //        //   (전송 자체가 자주 일어나면 1~2초 throttle 권장)
        //        // =========================
        //        var postKey = $"POSTTRY|{worker.id}|{mission.guid}";

        //        // ✅ 같은 미션에 대해 2초에 1번만 "전송시도" 로그 (중복 루프 방지 확인)
        //        if (ShouldLog(postKey, 2))
        //        {
        //            EventLogger.Info(
        //                $"[PostScheduler][POST_TRY] worker={worker.id}({worker.name}) " +
        //                $"missionGuid={mission.guid} seq={mission.sequence} name={mission.name} jobId={mission.jobId}"
        //            );
        //        }

        //        postMission(mission);

        //        // 성공 로그도 너무 많으면 throttle 적용 가능
        //        var okKey = $"POSTOK|{worker.id}|{mission.guid}";
        //        if (ShouldLog(okKey, 2))
        //        {
        //            EventLogger.Info(
        //                $"[PostScheduler][POST_OK] worker={worker.id}({worker.name}) missionGuid={mission.guid}"
        //            );
        //        }
        //    }
        //}


        /// <summary>
        /// 미션 전송 제어
        /// </summary>
        private void MissionPostScheduler()
        {
            //[조회] 배터리 Setting 정보
            var batterySetting = _repository.Battery.GetAll();

            //[조회] 작업이 가능한 Subscribe_Worker
            foreach (var worker in _repository.Workers.MiR_GetByActive()/*.Where(m => m.state == nameof(WorkerState.IDLE) && m.acsmissionId == null */)
            {
                //[초기화] 충전 파라메터
                Parameter ChargeEquest = null;

                //[조회] 현재 Worker에게 할당된 Mission
                var missions = _repository.Missions.GetByAssignedWorkerId(worker.id).OrderBy(r => r.sequence).ToList();
                if (missions == null || missions.Count == 0) continue;

                //[조회] Middlewares 정보
                var middleware = _repository.Middlewares.GetByWorkerId(worker.id);

                //[조회] 현재 진행중인 Mission
                var runmission = _repository.Missions.GetByRunMissions(missions).FirstOrDefault();

                if (runmission != null)
                {
                    // throttleKey: worker + run state 기준으로 묶어서 N초에 1번만 경고 로그
                    // - 같은 워커가 같은 runstate 때문에 계속 막히는지 확인 가능
                    //var key = $"RUNSKIP|{worker.id}|{runmission.state}";

                    //// 5초에 1번만 찍히게 제한(필요 시 10초/30초로 올려도 됨)
                    //if (ShouldLog(key, 5))
                    //{
                    //    EventLogger.Warn(
                    //        $"[PostScheduler][SKIP_RUNMISSION] " +
                    //        $"worker={worker.id}({worker.name}) workerState={worker.state} isMiddleware={worker.isMiddleware} " +
                    //        $"runGuid={runmission.guid} runState={runmission.state} runName={runmission.name} runSeq={runmission.sequence}"
                    //    );
                    //}

                    // runmission이 있으면 이 워커는 이번 틱에서는 처리하지 않음
                    continue;
                }

                bool c1 = worker.isMiddleware == true;

                bool c2 = worker.state == nameof(WorkerState.IDLE);

                if (c1 && c2)
                {
                    //[조건] 전송 실패시 재전송 또는 대기중인 미션전송
                    var mission = missions.Where(m => m.jobId != null
                                             && (m.state == nameof(MissionState.WAITING) || m.state == nameof(MissionState.FAILED) || m.state == nameof(MissionState.COMMANDREQUEST))).FirstOrDefault();
                    if (mission != null)
                    {
                        //CancelJob진행중일경우 보내지 않는다.
                        var job = _repository.Jobs.GetByid(mission.jobId);
                        if (job == null)
                        {
                            //EventLogger.Warn($"[MissionPostScheduler] Job not found for missionId={mission.guid}");
                            continue;
                        }

                        if (job.terminateState == null)
                        {
                            if (skipMission(mission, worker))
                            {
                                //EventLogger.Info($"[MissionPostScheduler] Mission skipped for missionId={mission.guid}, workerId={worker.id}");
                                continue;
                            }

                            //EventLogger.Info($"[MissionPostScheduler] Sending missionId={mission.guid} for workerId={worker.id}");
                            postMission(mission);
                        }
                        else
                        {
                            //EventLogger.Warn($"[MissionPostScheduler] Job terminateState is not null for jobId={job.guid}");
                        }
                    }
                    else
                    {
                        //EventLogger.Info($"[MissionPostScheduler] No matching mission found for workerId={worker.id}");
                        continue;
                    }
                }
            }
        }
    }
}