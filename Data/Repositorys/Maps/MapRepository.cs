using Common.Models.Bases;
using Common.Models.Jobs;
using Dapper;
using log4net;
using Microsoft.Data.SqlClient;

namespace Data.Repositorys.Maps
{
    public partial class MapRepository
    {
        private static readonly ILog logger = LogManager.GetLogger("Map"); //Function 실행관련 Log

        private readonly string connectionString;
        private readonly List<Map> _maps = new List<Map>(); // cached data
        private readonly object _lock = new object();

        public MapRepository(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public void Add(Map add)
        {
            lock (_lock)
            {
                _maps.Add(add);
                logger.Info($"Add: {add}");
            }
        }


        public void Delete()
        {
            lock (_lock)
            {
                _maps.Clear();
                logger.Info($"Delete");
            }
        }

        public void Remove(Map remove)
        {
            lock (_lock)
            {
                _maps.Remove(remove);
                logger.Info($"Remove: {remove}");
            }
        }

        public List<Map> GetAll()
        {
            lock (_lock)
            {
                return _maps.ToList();
            }
        }

        public Map GetById(string id)
        {
            lock (_lock)
            {
                return _maps.FirstOrDefault(m => m.id == id);
            }
        }
        public Map GetBymapId(string mapId)
        {
            lock (_lock)
            {
                return _maps.FirstOrDefault(m => m.mapId == mapId);
            }
        }
        public Map GetByName(string name)
        {
            lock (_lock)
            {
                return _maps.FirstOrDefault(m => m.name == name);
            }
        }
    }
}