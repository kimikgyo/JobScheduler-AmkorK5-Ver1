using Common.Models.Jobs;
using Dapper;
using log4net;
using Microsoft.Data.SqlClient;
using System.Diagnostics;

namespace Data.Repositorys.Positions
{
    public partial class PositionRepository
    {
        private static readonly ILog logger = LogManager.GetLogger("Position"); //Function 실행관련 Log

        private readonly string connectionString;
        private readonly List<Position> _positions = new List<Position>(); // cached data
        private readonly object _lock = new object();

        public PositionRepository(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public void Add(Position add)
        {
            lock (_lock)
            {
                _positions.Add(add);
                logger.Info($"Add: {add}");
            }
        }
        public void update(Position update)
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
                _positions.Clear();
                logger.Info($"Delete");
            }
        }

        public void Remove(Position remove)
        {
            lock (_lock)
            {
                _positions.Remove(remove);
                logger.Info($"Remove: {remove}");
            }
        }

        public List<Position> GetAll()
        {
            lock (_lock)
            {
                return _positions.ToList();
            }
        }
       

        public Position GetOccupied(string group, string subType, string workerId)
        {
            lock (_lock)
            {
                return _positions.FirstOrDefault(m => m.isEnabled == true && m.group == group && m.subType == subType &&m.isOccupied == true);
            }
        }

        //점유하고있지않은 포지션
        public List<Position> GetNotOccupied(string group, string subType)
        {
            lock (_lock)
            {
                return _positions.Where(m => m.isEnabled == true && m.group == group && m.subType == subType && m.isOccupied == false).ToList();
            }
        }

        public List<Position> GetByMapId(string mapid)
        {
            lock (_lock)
            {
                return _positions.Where(m => m.isEnabled == true && m.mapId == mapid).ToList();
            }
        }

        public List<Position> GetByPosValue(double x, double y, string mapid)
        {
            lock (_lock)
            {
                return GetByMapId(mapid).Where(m => m.x == x && m.y == y).ToList();
            }
        }

        public Position GetByPositionId(string positionId)
        {
            lock (_lock)
            {
                return _positions.FirstOrDefault(m => m.positionId == positionId);
            }
        }

        public Position GetById(string id)
        {
            lock (_lock)
            {
                return _positions.FirstOrDefault(m => m.id == id);
            }
        }

        public Position GetByname(string name)
        {
            lock (_lock)
            {
                return _positions.FirstOrDefault(m => m.name == name);
            }
        }

        /// <summary>
        /// 가장 가까운거리 WayPoint 찾기
        /// </summary>
        /// <param name="worker"></param>
        /// <param name="waypoints"></param>
        /// <returns></returns>
        public List<Position> FindNearestWayPoint(Worker worker, List<Position> waypoints)
        {
            lock (_lock)
            {
                return waypoints.OrderBy(pos => GetDistance(pos, worker)).ToList();
            }
        }

        /// <summary>
        /// 가장 먼거리 WayPoint 찾기
        /// </summary>
        /// <param name="worker"></param>
        /// <param name="waypoints"></param>
        /// <returns></returns>
        public List<Position> FindFarthestWayPoint(Worker worker, List<Position> waypoints)
        {
            lock (_lock)
            {
                return waypoints.OrderByDescending(pos => GetDistance(pos, worker)).ToList();
            }
        }

        public double GetDistance(Position waypoint, Worker worker)
        {
            lock (_lock)
            {
                return Math.Sqrt(Math.Pow(waypoint.x - worker.position_X, 2) + Math.Pow(waypoint.y - worker.position_Y, 2));
            }
        }
    }
}