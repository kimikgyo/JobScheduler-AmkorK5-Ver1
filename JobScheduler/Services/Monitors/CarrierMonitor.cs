namespace JOB.Services
{
    public partial class SchedulerService
    {
        private void CarrierControl()
        {
            carrierRemoveContorl();
        }

        /// <summary>
        /// 케리어 삭제 제어
        /// </summary>
        private void carrierRemoveContorl()
        {
            var carriers = _repository.Carriers.GetAll();
            foreach (var carrier in carriers)
            {
                if (IsInvalid(carrier.workerId))
                {
                    _repository.Carriers.Remove(carrier);
                }
            }
        }
    }
}