using Common.Models.Bases;
using Common.Models.Jobs;
using Common.Templates;
using log4net;

namespace Data.Repositorys.Carriers
{
    public class CarrierRepository
    {
        private static readonly ILog logger = LogManager.GetLogger("Carrier"); //Function 실행관련 Log

        private readonly string connectionString;
        private readonly List<Carrier> _Carriers = new List<Carrier>(); // cached data
        private readonly object _lock = new object();

        public CarrierRepository(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public (Carrier model, string msg) Add(Carrier add)
        {
            lock (_lock)
            {
                string massage = null;

                _Carriers.Add(add);
                logger.Info($"Add: {add}");
                return (add, massage);
            }
        }

        public void Delete()
        {
            lock (_lock)
            {
                string massage = null;
                _Carriers.Clear();
            }
        }

        public void Remove(Carrier carrier)
        {
            lock (_lock)
            {
                string massage = null;
                _Carriers.Remove(carrier);
                logger.Info($"Remove: {carrier}");
            }
        }

        public List<Carrier> GetAll()
        {
            lock (_lock)
            {
                return _Carriers.ToList();
            }
        }

        public List<Carrier> GetByWorkerId(string workerId)
        {
            lock (_lock)
            {
                return _Carriers.Where(c => c.workerId == workerId).ToList();
            }
        }

        public List<Carrier> GetById(string carrierId)
        {
            lock (_lock)
            {
                return _Carriers.Where(c => c.carrierId == carrierId).ToList();
            }
        }
    }
}