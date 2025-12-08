using Common.Models.Areas;
using Common.Models.Jobs;
using log4net;

namespace Data.Repositorys.Areas
{
    public class ACS_AreaRepository
    {
        private static readonly ILog logger = LogManager.GetLogger("ACS_Area"); //Function 실행관련 Log

        private readonly string connectionString;
        private readonly List<ACSArea>  _aCS_Areas = new List<ACSArea>(); // cached data
        private readonly object _lock = new object();

        public ACS_AreaRepository(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public void Add(ACSArea add)
        {
            lock (_lock)
            {
                _aCS_Areas.Add(add);
                logger.Info($"Add: {add}");
            }
        }
        public void update(ACSArea update)
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
                _aCS_Areas.Clear();
                logger.Info($"Delete");
            }
        }

        public void Remove(ACSArea remove)
        {
            lock (_lock)
            {
                _aCS_Areas.Remove(remove);
                logger.Info($"Remove: {remove}");
            }
        }

        public List<ACSArea> GetAll()
        {
            lock (_lock)
            {
                return _aCS_Areas.ToList();
            }
        }
    }
}
