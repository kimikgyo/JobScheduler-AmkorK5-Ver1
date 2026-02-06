using Common.Models.Bases;
using Common.Models.Jobs;

namespace JOB.Services
{
    public partial class SchedulerService
    {
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

                bool c1 = worker.isMiddleware == true;

                bool c2 = worker.state == nameof(WorkerState.IDLE) && runmission == null;

                if (c1 && c2)
                {
                    //[조건] 전송 실패시 재전송 또는 대기중인 미션전송
                    var mission = missions.Where(m => m.jobId != null
                                             && (m.state == nameof(MissionState.WAITING) || m.state == nameof(MissionState.FAILED) || m.state == nameof(MissionState.COMMANDREQUEST))).FirstOrDefault();
                    if (mission != null)
                    {
                        //CancelJob진행중일경우 보내지 않는다.
                        var job = _repository.Jobs.GetByid(mission.jobId);
                        if (job != null && job.terminateState == null)
                        {
                            if (skipMission(mission, worker)) continue;
                            postMission(mission);
                        }
                    }
                }
            }
        }
    }
}