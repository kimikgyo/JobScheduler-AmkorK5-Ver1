using Common.Models.Jobs;

namespace JOB.Services
{
    public partial class SchedulerService
    {
        private readonly object _lock = new object();

        private void JobPlanner()
        {
            OrderJobs();
            ChargeJobs();
            WaitJobs();
        }

        //Job생성
        private void OrderJobs()
        {
            Position source = null;
            Position destination = null;
            var Orders = _repository.Orders.GetByOrderStatus(nameof(OrderState.Queued));
            foreach (var Order in Orders)
            {
                var Job = _repository.Jobs.GetByOrderId(Order.id);
                if (Job != null) continue;

                if (IsInvalid(Order.sourceId))
                {
                    var worker = _repository.Workers.MiR_GetById(Order.specifiedWorkerId);
                    if (worker == null) continue;

                    //[조회]목적지 조회.
                    destination = _repository.Positions.GetById(Order.destinationId);
                    if (destination == null) continue;
                    var carateJob = Createjob(Order, null, destination);
                    if (carateJob == false) continue;
                }
                else
                {
                    //[조회]출발지
                    source = _repository.Positions.MiR_GetById(Order.sourceId);
                    if (source == null) continue;
                    //[조회]목적지
                    destination = _repository.Positions.GetById(Order.destinationId);
                    if (destination == null) continue;

                    var carateJob = Createjob(Order, source, destination);
                    if (carateJob == false) continue;
                }
            }
        }

        private bool Createjob(Order order, Position source, Position destination)
        {
            // ------------------------------------------------------------
            // 1) 방어 코드
            // ------------------------------------------------------------

            if (order == null)
            {
                EventLogger.Error($"[Job][CREATE][ERROR] order is null → job creation aborted");
                return false;
            }
            if (destination == null)
            {
                EventLogger.Error($"[Job][CREATE][ERROR] destination is null → job creation aborted");
                return false;
            }
            if (source == null)
            {
                _Queue.Create_Job(destination.group, order.id, order.type, order.subType, order.carrierId, order.priority, order.drumKeyCode
                                    , null, null, null, destination.id, destination.name, destination.linkedFacility
                                    , order.specifiedWorkerId);
                updateOccupied(destination, true, 0.5,"Order1");

                // --------------------------------------------------------
                // 3) 생성 요청 성공 로그
                // --------------------------------------------------------
                EventLogger.Info($"[Job][CREATE] enqueue job request: source name = IsNull, destName = {destination.name}, specifiedWorkerId = {order.specifiedWorkerId}, OrderId = {order.id}");

                return true;
            }
            else
            {
                _Queue.Create_Job(source.group, order.id, order.type, order.subType, order.carrierId, order.priority, order.drumKeyCode
                            , source.id, source.name, source.linkedFacility, destination.id, destination.name, destination.linkedFacility
                            , order.specifiedWorkerId);
                updateOccupied(source, true, 0.5, "Order2");
                updateOccupied(destination, true, 0.5, "Order3");

                // --------------------------------------------------------
                // 3) 생성 요청 성공 로그
                // --------------------------------------------------------
                EventLogger.Info($"[Job][CREATE] enqueue job request: source name = {source.name}, destName = {destination.name}, specifiedWorkerId = {order.specifiedWorkerId}, OrderId = {order.id}");

                return true;
            }
        }
    }
}