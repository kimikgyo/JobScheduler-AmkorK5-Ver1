using Common.Models.Jobs;

namespace JOB.Services
{
    public partial class SchedulerService
    {
        /// <summary>
        /// Cancel 제어
        /// </summary>
        private void cancelControl()
        {
            var jobs = _repository.Jobs.GetAll();

            terminateState_Null(jobs);
            terminateState_Inited(jobs);
            terminateState_Executing(jobs);
        }

        // ============================================================
        // 3) EXECUTING 처리 (deleteMission은 “현재 1개만”)
        // ============================================================

        /// <summary>
        /// terminateState == EXECUTING 처리
        /// - 관리자(INATECH): 현재 미션 locked 여부 상관없이 deleteMission 가능 (현재 1개만)
        /// - 비관리자: current.isLocked == false일 때만 deleteMission 가능 (현재 1개만)
        /// </summary>
        private void terminateState_Executing(List<Job> jobs)
        {
            if (jobs == null || jobs.Count == 0)
            {
                //EventLogger.Warn($"[TERM][EXECUTING][SKIP] jobs is null or empty");
                return;
            }

            var executingJobs = jobs.Where(j => j.terminateState == nameof(TerminateState.EXECUTING)).ToList();
            if (executingJobs == null || executingJobs.Count == 0)
            {
                //EventLogger.Info($"[TERM][EXECUTING][SKIP] no jobs in EXECUTING");
                return;
            }

            //EventLogger.Info($"[TERM][EXECUTING][START] jobs={executingJobs.Count}");

            foreach (var job in executingJobs)
            {
                if (job == null)
                {
                    EventLogger.Error($"[TERM][EXECUTING][ERROR] job is null in list");
                    continue;
                }

                var ordered = GetOrderedMissions(job.guid);
                if (ordered.Count == 0)
                {
                    EventLogger.Warn($"[TERM][EXECUTING][SKIP] no missions: jobGuid={job.guid}");
                    continue;
                }

                var current = FindCurrentActiveMission(ordered);
                if (current == null)
                {
                    EventLogger.Info($"[TERM][EXECUTING][NONE] no active mission: jobGuid={job.guid}");
                    continue;
                }

                bool isAdmin = IsAdminCancel(job);

                // 관리자면 locked 상관없이 delete 가능
                if (isAdmin)
                {
                    EventLogger.Info($"[TERM][EXECUTING][ADMIN][DELETE_TRY] jobGuid={job.guid}, seq={current.sequence}, isLocked={current.isLocked}");

                    bool deleted = deleteMission(current);

                    if (deleted) EventLogger.Info($"[TERM][EXECUTING][ADMIN][DELETE_OK] jobGuid={job.guid}, seq={current.sequence}");
                    else EventLogger.Warn($"[TERM][EXECUTING][ADMIN][DELETE_FAIL] jobGuid={job.guid}, seq={current.sequence}");

                    continue;
                }

                // 비관리자는 unlocked(false)일 때만 delete 가능
                if (current.isLocked == false)
                {
                    EventLogger.Info($"[TERM][EXECUTING][USER][DELETE_TRY] jobGuid={job.guid}, seq={current.sequence}");

                    bool deleted = deleteMission(current);

                    if (deleted) EventLogger.Info($"[TERM][EXECUTING][USER][DELETE_OK] jobGuid={job.guid}, seq={current.sequence}");
                    else EventLogger.Warn($"[TERM][EXECUTING][USER][DELETE_FAIL] jobGuid={job.guid}, seq={current.sequence}");
                }
                else
                {
                    EventLogger.Info($"[TERM][EXECUTING][USER][SKIP_LOCKED] locked mission cannot be deleted: jobGuid={job.guid}, seq={current.sequence}");
                }
            }

            EventLogger.Info($"[TERM][EXECUTING][END]");
        }

        /// <summary>
        /// UI상/외부 요인으로 Mission만 CANCELED가 되었는데 Job.terminateState가 null인 경우,
        /// JobScheduler가 terminate 플로우(INITED)로 진입시키는 보정 로직
        /// </summary>
        private void terminateState_Null(List<Job> jobs)
        {
            // ------------------------------------------------------------
            // [0] 방어 코드
            // ------------------------------------------------------------
            if (jobs == null || jobs.Count == 0)
            {
                //EventLogger.Warn($"[TerminateState][NULL][SKIP] jobs is null or empty");
                return;
            }

            // ------------------------------------------------------------
            // [1] terminateState == null Job만 추출
            // ------------------------------------------------------------
            var terminateState_Null_Jobs = jobs.Where(j => j.terminateState == null).ToList();
            if (terminateState_Null_Jobs == null || terminateState_Null_Jobs.Count == 0)
            {
                //EventLogger.Info($"[TerminateState][NULL][SKIP] no jobs with terminateState=null");
                return;
            }

            //EventLogger.Info($"[TerminateState][NULL][START] jobs={terminateState_Null_Jobs.Count}");

            // ------------------------------------------------------------
            // [2] Job 하위 미션 중 CANCELED가 하나라도 있으면 INITED로 보정
            // ------------------------------------------------------------
            foreach (var job in terminateState_Null_Jobs)
            {
                if (job == null)
                {
                    EventLogger.Error($"[TerminateState][NULL][ERROR] job is null in list");
                    continue;
                }

                var missions = _repository.Missions.GetByJobId(job.guid);

                if (missions == null || missions.Count == 0)
                {
                    //EventLogger.Warn($"[TerminateState][NULL][SKIP] no missions: jobGuid={job.guid}");
                    continue;
                }

                var canceledMission = missions.FirstOrDefault(r => r != null && r.state == nameof(MissionState.CANCELED));
                if (canceledMission == null)
                {
                    //EventLogger.Info($"[TerminateState][NULL][SKIP] no canceled mission: jobGuid={job.guid}");
                    continue;
                }

                updateStateJob(job, job.state, nameof(TerminateState.INITED), nameof(TerminateType.CANCEL), "JobScheduler", true);

                EventLogger.Info($"[TerminateState][NULL][FIXED] jobGuid={job.guid}, set terminateState=INITED, terminationType=CANCEL, canceledMissionSeq={canceledMission.sequence}");
            }

            //EventLogger.Info($"[TerminateState][NULL][END]");
        }

        // ============================================================
        // 2) INITED 처리 (규칙 반영 핵심)
        // ============================================================

        /// <summary>
        /// terminateState == INITED 처리
        ///
        /// [규칙]
        /// 1) 관리자(INATECH)
        ///  - 현재 미션 isLocked true/false 상관없이 취소 가능
        ///  - COMPLETED 제외 전부 CANCELED
        ///  - job.terminateState = EXECUTING 으로 넘김 (Executing 단계에서 deleteMission 수행)
        ///
        /// 2) 비관리자
        ///  - current.isLocked == false:
        ///     deleteMission(current) (현재 진행중 1개만)
        ///     current 뒤의 모든 미션 CANCELED
        ///
        ///  - current.isLocked == true:
        ///     next.isLocked == true (연달아 true,true,...):
        ///        isLocked == false 인 미션만 전부 CANCELED
        ///        isLocked == true 인 미션들은 “전부 처리(끝까지 진행)” → delete/cancel 하지 않음
        ///
        ///     next.isLocked == false (연달아 아님: true 다음 false):
        ///        current(true)만 끝까지 진행
        ///        next(false)부터 뒤의 모든 미션은 전부 CANCELED (뒤에 true가 있어도 전부 CANCELED)
        ///
        /// deleteMission 은 “현재 진행중 1개만 Delete”
        /// </summary>

        private void terminateState_Inited(List<Job> jobs)
        {
            if (jobs == null || jobs.Count == 0)
            {
                //EventLogger.Warn($"[TerminateState][INITED][SKIP] jobs is null or empty");
                return;
            }

            var initedJobs = jobs.Where(j => j.terminateState == nameof(TerminateState.INITED)).ToList();
            if (initedJobs == null || initedJobs.Count == 0)
            {
                //EventLogger.Info($"[TerminateState][INITED][SKIP] no jobs in INITED");
                return;
            }

            EventLogger.Info($"[TerminateState][INITED][START] jobs={initedJobs.Count}");

            foreach (var job in initedJobs)
            {
                if (job == null)
                {
                    EventLogger.Error($"[TerminateState][INITED][ERROR] job is null in list");
                    continue;
                }

                var ordered = GetOrderedMissions(job.guid);
                if (ordered.Count == 0)
                {
                    EventLogger.Warn($"[TerminateState][INITED][SKIP] no missions: jobGuid={job.guid}");
                    continue;
                }

                bool isAdmin = IsAdminCancel(job);
                var current = FindCurrentActiveMission(ordered);

                EventLogger.Info($"[TerminateState][INITED][JOB] jobGuid={job.guid}, orderId={job.orderId}, isAdmin={isAdmin}");

                // ------------------------------------------------------------
                // 1) 관리자: COMPLETED 제외 전부 CANCELED + EXECUTING으로 전환
                // ------------------------------------------------------------
                if (isAdmin)
                {
                    // ⚠️ 운영 팁:
                    // - current(진행중)까지 CANCELED로 바꾸면 Executing에서 active를 못 찾을 수 있음
                    // - 하지만 네가 원래 쓰던 방식 유지(요청대로) -> 그대로 둠
                    var cancelTargets = ordered.Where(m => m.state != nameof(MissionState.COMPLETED)).ToList();
                    CancelMissions(cancelTargets, "ADMIN_ALL_CANCEL", job.guid);

                    updateStateJob(job, job.state, nameof(TerminateState.EXECUTING), job.terminationType, job.terminator, true);

                    EventLogger.Info($"[TerminateState][INITED][ADMIN] job updated: jobGuid={job.guid}, terminateState=EXECUTING");

                    UpdateOrderTerminating(job, "ADMIN");
                    continue;
                }

                // ------------------------------------------------------------
                // 2) 비관리자
                //    (※ 여기서는 deleteMission 절대 호출하지 않음)
                // ------------------------------------------------------------
                if (current == null)
                {
                    // active 미션이 없으면 안전하게 unlock(false)만 취소
                    var cancelUnlocked = ordered
                        .Where(m => m.isLocked == false && m.state != nameof(MissionState.COMPLETED))
                        .ToList();

                    CancelMissions(cancelUnlocked, "USER_NO_ACTIVE_CANCEL_UNLOCKED_ONLY", job.guid);

                    TouchJobTerminatingAt(job, "USER_NO_ACTIVE");
                    UpdateOrderTerminating(job, "USER_NO_ACTIVE");
                    continue;
                }

                // ------------------------------------------------------------
                // (A) current unlocked(false)
                // - current는 Executing에서 deleteMission 대상으로 남겨두기 위해
                //   여기서는 current를 cancel하지 않고, 뒤만 cancel
                // ------------------------------------------------------------
                if (current.isLocked == false)
                {
                    var cancelAfter = ordered
                        .Where(m => m.sequence > current.sequence && m.state != nameof(MissionState.COMPLETED))
                        .ToList();

                    CancelMissions(cancelAfter, "USER_UNLOCKED_CANCEL_AFTER_ALL", job.guid);

                    job.terminateState = nameof(TerminateState.EXECUTING);   // ✅ delete는 Executing에서만
                    TouchJobTerminatingAt(job, "USER_UNLOCKED_SET_EXECUTING");
                    UpdateOrderTerminating(job, "USER_UNLOCKED");
                    continue;
                }

                // ------------------------------------------------------------
                // (B) current locked(true)
                //
                // 규칙(수정 반영):
                // - locked(true) 연속 구간은 전부 처리되어야 함 (취소/삭제 금지)
                // - 연속 구간이 끝난 뒤 "첫 unlocked(false)"부터는
                //   뒤에 locked(true)가 다시 있어도 전부 Cancel 처리
                //   예) true, true, false, true  => 3번부터 4번까지 전부 Cancel
                // ------------------------------------------------------------

                // current 이후에서 "연속 locked(true) 구간"을 스킵한 뒤 첫 unlocked(false) 찾기
                //SkipWhile(x => 조건)앞에서부터 차례대로 보면서 조건 == true인 요소는 버림(스킵) 조건 == false가 처음 나오면 그 요소부터 끝까지 전부 반환
                //이후에 다시 조건 == true가 나와도 그건 더 이상 스킵하지 않음

                //TakeWhile // 연속 locked(true)만 추출
                var firstUnlockedAfterLockedChain = ordered
                    .Where(m => m != null && m.sequence > current.sequence)
                    .OrderBy(m => m.sequence)
                    .SkipWhile(m => m.isLocked == true)
                    .FirstOrDefault();

                if (firstUnlockedAfterLockedChain == null)
                {
                    // 뒤에 unlocked(false)가 없다 => 끝까지 locked만 존재
                    // => 취소할 대상이 없고, locked는 계속 처리되어야 함
                    EventLogger.Info($"[TerminateState][INITED][USER][LOCKED_CHAIN][NO_UNLOCKED_AFTER] jobGuid={job.guid}, currentSeq={current.sequence}");

                    TouchJobTerminatingAt(job, "USER_LOCKED_CHAIN_NO_UNLOCKED_AFTER");
                    UpdateOrderTerminating(job, "USER_LOCKED_CHAIN_NO_UNLOCKED_AFTER");
                    continue;
                }

                int fromSeq = firstUnlockedAfterLockedChain.sequence;

                EventLogger.Info($"[TerminateState][INITED][USER][LOCKED_CHAIN][CANCEL_FROM_FIRST_UNLOCKED] jobGuid={job.guid}, currentSeq={current.sequence}, fromSeq={fromSeq}");

                var cancelFromFirstUnlocked = ordered.Where(m => m.sequence >= fromSeq && m.state != nameof(MissionState.COMPLETED)).ToList();

                CancelMissions(cancelFromFirstUnlocked, "USER_LOCKED_CHAIN_CANCEL_FROM_FIRST_UNLOCKED_ALL", job.guid);

                // current locked는 delete 금지이므로 EXECUTING 불필요
                TouchJobTerminatingAt(job, "USER_LOCKED_CHAIN_CANCEL_FROM_FIRST_UNLOCKED_ALL");
                UpdateOrderTerminating(job, "USER_LOCKED_CHAIN_CANCEL_FROM_FIRST_UNLOCKED_ALL");
            }

            EventLogger.Info($"[TerminateState][INITED][END]");
        }

        /// <summary>
        /// MissionState가 "진행중(active)" 으로 판단되는지 검사
        /// - HashSet 사용 안함
        /// - deleteMission 대상으로 판단할 상태만 true
        /// </summary>
        private bool IsActiveMissionState(string state)
        {
            if (string.IsNullOrEmpty(state)) return false;

            return state == nameof(MissionState.PENDING)
                || state == nameof(MissionState.EXECUTING)
                || state == nameof(MissionState.COMMANDREQUESTCOMPLETED);
        }

        /// <summary>
        /// JobGuid로 Mission을 조회하고 sequence 기준으로 정렬해서 반환
        /// - null 방어 포함
        /// </summary>
        private List<Mission> GetOrderedMissions(string jobGuid)
        {
            var missions = _repository.Missions.GetByJobId(jobGuid);

            if (missions == null) return new List<Mission>();
            if (missions.Count == 0) return new List<Mission>();

            return missions.Where(m => m != null).OrderBy(m => m.sequence).ToList();
        }

        /// <summary>
        /// 정렬된 목록에서 "현재 진행중(active)" 미션 1개 찾기
        /// </summary>
        private Mission FindCurrentActiveMission(List<Mission> ordered)
        {
            if (ordered == null) return null;
            if (ordered.Count == 0) return null;

            return ordered.FirstOrDefault(m => IsActiveMissionState(m.state));
        }

        /// <summary>
        /// (COMPLETED 제외) 지정된 미션들을 CANCELED로 변경
        /// </summary>
        private void CancelMissions(List<Mission> targets, string reasonTag, string jobGuid)
        {
            if (targets == null || targets.Count == 0)
            {
                EventLogger.Info($"[TERM][CANCEL][SKIP] no targets: reason={reasonTag}, jobGuid={jobGuid}");
                return;
            }

            EventLogger.Info($"[TERM][CANCEL][START] targets={targets.Count}: reason={reasonTag}, jobGuid={jobGuid}");

            foreach (var m in targets)
            {
                if (m == null) continue;

                // COMPLETED는 상태 꼬임 방지를 위해 절대 Cancel 처리하지 않음
                if (m.state == nameof(MissionState.COMPLETED))
                    continue;

                updateStateMission(m, nameof(MissionState.CANCELED));
            }

            EventLogger.Info($"[TERM][CANCEL][END] reason={reasonTag}, jobGuid={jobGuid}");
        }

        /// <summary>
        /// Job의 terminatingAt만 갱신(너가 말한 “상태 변경 안 함” 정책용)
        /// </summary>
        private void TouchJobTerminatingAt(Job job, string tag)
        {
            if (job == null) return;

            job.terminatingAt = DateTime.Now;
            updateStateJob(job, job.state, job.terminateState, job.terminationType, job.terminator, true);

                EventLogger.Info($"[TERM][JOB][TOUCH] tag={tag}, jobGuid={job.guid}, terminatingAt={job.terminatingAt:O}");
        }

        /// <summary>
        /// Order 상태를 Canceling / Aborting 으로 갱신
        /// </summary>
        private void UpdateOrderTerminating(Job job, string tag)
        {
            if (job == null) return;

            var order = _repository.Orders.GetByid(job.orderId);
            if (order == null)
            {
                EventLogger.Warn($"[TERM][ORDER][SKIP] order not found: tag={tag}, orderId={job.orderId}, jobGuid={job.guid}");
                return;
            }

            if (job.terminationType == nameof(TerminateType.CANCEL))
            {
                updateStateOrder(order, OrderState.Canceling, true);
                EventLogger.Info($"[TERM][ORDER][UPDATE] tag={tag}, orderId={job.orderId}, state=Canceling, jobGuid={job.guid}");
                return;
            }

            if (job.terminationType == nameof(TerminateType.ABORT))
            {
                updateStateOrder(order, OrderState.Aborting, true);
                EventLogger.Info($"[TERM][ORDER][UPDATE] tag={tag}, orderId={job.orderId}, state=Aborting, jobGuid={job.guid}");
                return;
            }

            EventLogger.Warn($"[TERM][ORDER][UNKNOWN_TYPE] tag={tag}, terminationType={job.terminationType}, orderId={job.orderId}, jobGuid={job.guid}");
        }

        /// <summary>
        /// 관리자(INATECH) 여부 판단 (null-safe)
        /// </summary>
        private bool IsAdminCancel(Job job)
        {
            bool revalue = false;
            if (job == null) return false;

            string terminator = job.terminator;
            if (terminator == null) terminator = string.Empty;

            if (terminator.ToUpper() == "INATECH") revalue = true;

            return revalue;
        }
    }
}