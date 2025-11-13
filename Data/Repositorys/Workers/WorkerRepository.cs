using Common.Models.Jobs;
using log4net;

namespace Data.Repositorys.Workers
{
    public partial class WorkerRepository
    {
        private static readonly ILog logger = LogManager.GetLogger("Worker"); //Function 실행관련 Log

        private readonly string connectionString;
        private readonly List<Worker> _workers = new List<Worker>(); // cached data
        private readonly object _lock = new object();

        public WorkerRepository(string connectionString)
        {
            this.connectionString = connectionString;
            //createTable();
            //Load();
        }

        private void Load()
        {
            _workers.Clear();
            //using (var con = new SqlConnection(connectionString))
            //{
            //    foreach (var data in con.Query<Worker>("SELECT * FROM [Waypoint]"))
            //    {
            //        _workers.Add(data);
            //    }
            //}
        }

        public void Add(Worker add)
        {
            lock (_lock)
            {
                _workers.Add(add);
                logger.Info($"Add: {add}");
            }
        }

        public void Update(Worker update)
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
                _workers.Clear();
                logger.Info($"Delete");
            }
        }

        public void Remove(Worker remove)
        {
            lock (_lock)
            {
                _workers.Remove(remove);
                logger.Info($"Remove: {remove}");
            }
        }

        public List<Worker> GetAll()
        {
            lock (_lock)
            {
                return _workers.ToList();
            }
        }

        public List<Worker> GetByActive()
        {
            lock (_lock)
            {
                return _workers.Where(m => m.isActive == true && m.isOnline == true).ToList();
            }
        }

        public List<Worker> GetByConnect()
        {
            lock (_lock)
            {
                return _workers.Where(m => m.isOnline == true).ToList();
            }
        }

        public Worker GetById(string id)
        {
            lock (_lock)
            {
                return _workers.FirstOrDefault(m => m.id == id);
            }
        }

        /// <summary>
        /// 포지션에서 가장 가까운거리 Worker 찾기
        /// </summary>
        /// <param name="workers"></param>
        /// <param name="waypoint"></param>
        /// <returns></returns>
        public List<Worker> FindNearestWorker(List<Worker> workers, Position waypoint)
        {
            lock (_lock)
            {
                return workers.OrderBy(worker => GetDistance(worker, waypoint)).ToList();
            }
        }

        /// <summary>
        /// 포지션에서 가장 먼거리 Worker 찾기
        /// </summary>
        /// <param name="workers"></param>
        /// <param name="waypoint"></param>
        /// <returns></returns>
        public List<Worker> FindFarthestWorker(List<Worker> workers, Position waypoint)
        {
            lock (_lock)
            {
                return workers.OrderByDescending(worker => GetDistance(worker, waypoint)).ToList();
            }
        }

        public double GetDistance(Worker worker, Position waypoint)
        {
            lock (_lock)
            {
                return Math.Sqrt(Math.Pow(worker.position_X - waypoint.x, 2) + Math.Pow(worker.position_Y - waypoint.y, 2));
            }
        }
    }
}