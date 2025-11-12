using Common.Templates;

namespace Data.Repositorys.Templates
{
    public class JobTemplateRepository
    {
        //private static readonly ILog logger = LogManager.GetLogger("Worker"); //Function 실행관련 Log

        private readonly string connectionString;
        private readonly List<JobTemplate> _jobTemplates = new List<JobTemplate>(); // cached data
        private readonly object _lock = new object();

        public JobTemplateRepository(string connectionString)
        {
            this.connectionString = connectionString;
            //createTable();
            //Load();
        }

        private void Load()
        {
            _jobTemplates.Clear();
            //using (var con = new SqlConnection(connectionString))
            //{
            //    foreach (var data in con.Query<Worker>("SELECT * FROM [Waypoint]"))
            //    {
            //        _workers.Add(data);
            //    }
            //}
        }

        public (JobTemplate model, string msg) Add(JobTemplate add)
        {
            lock (_lock)
            {
                string massage = null;

                _jobTemplates.Add(add);
                //logger.Info($"Add: {add}");
                return (add, massage);
            }
        }

        //public (Worker model, string msg) Update(Worker update)
        //{
        //    lock (this)
        //    {
        //        string massage = null;
        //        try
        //        {
        //            return (update, massage);
        //        }
        //        catch (Exception ex)
        //        {
        //            massage = $"{nameof(Add)}+ \r\n +{ex.Message} + \r\n + {ex.InnerException}";
        //            return (null, massage);
        //        }
        //    }
        //}

        public void Delete()
        {
            lock (_lock)
            {
                string massage = null;
                _jobTemplates.Clear();
            }
        }

        public (JobTemplate model, string msg) Remove(JobTemplate remove)
        {
            lock (_lock)
            {
                string massage = null;
                //_workers.Remove(remove);
                return (remove, massage);
            }
        }

        public List<JobTemplate> GetAll()
        {
            lock (_lock)
            {
                return _jobTemplates.ToList();
            }
        }
        public List<JobTemplate>GeyByOrderType(string type,string subType)
        {
            lock (_lock)
            {
                return _jobTemplates.Where(m=>m.type == type && m.subType == subType).ToList();
            }
        }
        

    }
}