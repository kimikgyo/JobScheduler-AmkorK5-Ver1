using Common.Models.Jobs;

namespace JOB.Services
{
    public partial class SchedulerService
    {
        private void StatusChangeControl()
        {
            inProgressControl();
            cancelAbortCompleteControl();
            jobCompleteControl();
            MissionCompleteControl();
        }

        private void inProgressControl()
        {
            //미션중 하나의미션이라도 Robot에게 전달이 되어 있을경우! Job 및 Order 상태를 변경한다.
            foreach (var mission in _repository.Missions.GetAll().Where(m => m.state == nameof(MissionState.COMMANDREQUESTCOMPLETED) || m.state == nameof(MissionState.SKIPPED)).ToList())
            {
                var job = _repository.Jobs.GetByid(mission.jobId);
                if (job != null)
                {
                    updateStateJob(job, nameof(JobState.INPROGRESS), true);

                    var order = _repository.Orders.GetByid(mission.orderId);
                    if (order != null && order.state != nameof(OrderState.Transferring))
                    {
                        updateStateOrder(order, OrderState.Transferring, true);
                    }
                }
            }
        }

        private void cancelAbortCompleteControl()
        {
            var cancelAbortJobs = _repository.Jobs.GetAll().Where(j => (j.terminateState == nameof(TerminateState.EXECUTING)) || (j.terminateState == nameof(TerminateState.COMPLETED))).ToList();
            foreach (var cancelAbortJob in cancelAbortJobs)
            {
                var missions = _repository.Missions.GetByJobId(cancelAbortJob.guid);
                var notFinishMission = missions.FirstOrDefault(m => m.finishedAt == null);
                if (notFinishMission == null)
                {
                    if (cancelAbortJob.terminateState != nameof(TerminateState.COMPLETED))
                    {
                        switch (cancelAbortJob.terminationType)
                        {
                            case nameof(TerminateType.CANCEL):
                                cancelAbortJob.terminateState = nameof(TerminateState.COMPLETED);
                                cancelAbortJob.terminatedAt = DateTime.Now;
                                updateStateJob(cancelAbortJob, nameof(JobState.CANCELCOMPLETED), true);
                                break;

                            case nameof(TerminateType.ABORT):
                                cancelAbortJob.terminateState = nameof(TerminateState.COMPLETED);
                                cancelAbortJob.terminatedAt = DateTime.Now;
                                updateStateJob(cancelAbortJob, nameof(JobState.ABORTCOMPLETED), true);
                                break;
                        }
                    }
                    var order = _repository.Orders.GetByid(cancelAbortJob.orderId);
                    if (order != null)
                    {
                        updateStateOrder(order, OrderState.None);
                        _Queue.Remove_Order(order, DateTime.Now);
                    }
                    else
                    {
                        _Queue.Remove_Job(cancelAbortJob, DateTime.Now);
                    }
                }
            }
        }

        private void jobCompleteControl()
        {
            foreach (var job in _repository.Jobs.GetAll().Where(x => x.state == nameof(JobState.INPROGRESS)))
            {
                var missions = _repository.Missions.GetByJobId(job.guid);
                if (missions == null || missions.Count == 0) continue;

                var mission = missions.FirstOrDefault(s => s.state != nameof(MissionState.COMPLETED) && s.state != nameof(MissionState.SKIPPED));
                if (mission == null)
                {
                    var order = _repository.Orders.GetByid(job.orderId);
                    if (order != null)
                    {
                        updateStateJob(job, nameof(JobState.COMPLETED));
                        updateStateOrder(order, OrderState.None);
                        _Queue.Remove_Order(order, DateTime.Now);
                    }
                    else
                    {
                        updateStateJob(job, nameof(JobState.COMPLETED));
                        _Queue.Remove_Job(job, DateTime.Now);
                    }
                }
            }
        }

        private void MissionCompleteControl()
        {

            var missions = _repository.Missions.GetAll().Where(r => r.jobId == null).ToList();
            if (missions.Count == 0 || missions == null) return;

            foreach (var mission in missions)
            {
                if (mission.state == nameof(MissionState.COMPLETED) || mission.state == nameof(MissionState.SKIPPED)) 
                _repository.Missions.Remove(mission);
            }

        }
    }
}