using Common.Models.Bases;
using Common.Models.Jobs;
using log4net;

namespace Data.Repositorys.Middlewares
{
    public class MiddlewareRepogistory
    {
        private static readonly ILog logger = LogManager.GetLogger("Middleware"); //Function 실행관련 Log

        private readonly string connectionString;
        private readonly List<Middleware> _middlewares = new List<Middleware>(); // cached data
        private readonly object _lock = new object();

        public MiddlewareRepogistory(string connectionString)
        {
            this.connectionString = connectionString;
            //createTable();
            //Load();
        }

        private void Load()
        {
            _middlewares.Clear();
            //using (var con = new SqlConnection(connectionString))
            //{
            //    foreach (var data in con.Query<Worker>("SELECT * FROM [Waypoint]"))
            //    {
            //        _workers.Add(data);
            //    }
            //}
        }

        public void Add(Middleware add)
        {
            lock (_lock)
            {
                _middlewares.Add(add);
                logger.Info($"Add: {add}");
            }
        }

        public void Update(Middleware update)
        {
            lock (_lock)
            {
                logger.Info($"update: {update}");
            }
        }

        public void Delete()
        {
            lock (_lock)
            {
                _middlewares.Clear();
                logger.Info($"Delete");
            }
        }

        public void Remove(Middleware remove)
        {
            lock (_lock)
            {
                _middlewares.Remove(remove);
                logger.Info($"Remove: {remove}");
            }
        }

        public List<Middleware> GetAll()
        {
            lock (_lock)
            {
                return _middlewares.ToList();
            }
        }

        public List<Middleware> GetByActive()
        {
            lock (_lock)
            {
                return _middlewares.Where(m => m.isActive == true && m.isOnline == true).ToList();
            }
        }

        public List<Middleware> GetByConnect()
        {
            lock (_lock)
            {
                return _middlewares.Where(m => m.isOnline == true).ToList();
            }
        }

        public Middleware GetByWorkerId(string workerId)
        {
            lock (_lock)
            {
                return _middlewares.FirstOrDefault(m => m.workerId == workerId);
            }
        }
    }
}