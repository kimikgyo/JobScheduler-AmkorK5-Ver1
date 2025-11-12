using Common.Models.Bases;
using log4net;
using Microsoft.Extensions.Configuration;
using System.Threading;
using static log4net.Appender.FileAppender;

namespace Data.Repositorys.Services
{
    public class ServiceApiRepository
    {
        private static readonly ILog ApiLogger = LogManager.GetLogger("ApiEvent"); //Function 실행관련 Log

        private readonly string connectionString;
        private readonly List<ServiceApi> _serviceApis = new List<ServiceApi>(); // cached data
        private readonly object _lock = new object();

        public ServiceApiRepository(string connectionString)
        {
            this.connectionString = connectionString;
            //createTable();
            Load();
        }

        private void Load()
        {
            _serviceApis.Clear();
        }

        public void Add(ServiceApi add)
        {
            lock (_lock)
            {
                string massage = null;

                _serviceApis.Add(add);
                ApiLogger.Info(
                                 $"Add: " +
                                 $"type = {add.type,-5}" +
                                 $",subType = {add.subType,-5}" +
                                 $",ip = {add.ip,-5}" +
                                 $",port = {add.port,-5}" +
                                 $",connectId = {add.connectId,-5}" +
                                 $",connectPassword = {add.connectPassword,-5}" +
                                 $",timeOut = {add.timeOut,-5}"
                              );
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
                _serviceApis.Clear();
            }
        }

        public (ServiceApi model, string msg) Remove(ServiceApi remove)
        {
            lock (_lock)
            {
                string massage = null;
                _serviceApis.Remove(remove);
                return (remove, massage);
            }
        }

        public List<ServiceApi> GetAll()
        {
            lock (_lock)
            {
                return _serviceApis.ToList();
            }
        }

        public ServiceApi GetByIpPort(string ip, string port)
        {
            lock (_lock)
            {
                return _serviceApis.FirstOrDefault(m => m.ip == ip && m.port == port);
            }
        }

        public List<ServiceApi> GetBytype(string type)
        {
            lock (_lock)
            {
                return _serviceApis.Where(m => m.type == type).ToList();
            }
        }
    }
}