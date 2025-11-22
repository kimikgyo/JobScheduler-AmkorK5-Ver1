using Common.Models.Bases;
using Common.Templates;

namespace Data.Repositorys.Templates
{
    public class MissionTemplateRepository
    {  //private static readonly ILog logger = LogManager.GetLogger("Worker"); //Function 실행관련 Log
        private readonly string connectionString;
        private readonly List<MissionTemplate> _missionTemplates = new List<MissionTemplate>(); // cached data
        private readonly object _lock = new object();

        public MissionTemplateRepository(string connectionString)
        {
            this.connectionString = connectionString;
            //createTable();
            //Load();
        }

        private void Load()
        {
            _missionTemplates.Clear();
            //using (var con = new SqlConnection(connectionString))
            //{
            //    foreach (var data in con.Query<Worker>("SELECT * FROM [Waypoint]"))
            //    {
            //        _workers.Add(data);
            //    }
            //}
        }

        public (MissionTemplate model, string msg) Add(MissionTemplate add)
        {
            lock (_lock)
            {
                string massage = null;

                _missionTemplates.Add(add);
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
                _missionTemplates.Clear();
            }
        }

        public (MissionTemplate model, string msg) Remove(MissionTemplate remove)
        {
            lock (_lock)
            {
                string massage = null;
                //_workers.Remove(remove);
                return (remove, massage);
            }
        }

        public List<MissionTemplate> GetAll()
        {
            lock (_lock)
            {
                return _missionTemplates.ToList();
            }
        }

        public List<Parameter> GetParametas(List<MissionTemplate> missionTemplates)
        {
            //파라메타 내용을 찾을때 사용
            //1. parameters 가 null 인 Mission은 제외
            //2. List<Mission> → 모든 parameters 를 하나의 열로 평탄화
            //3. List<Parameter> 로 리턴
            lock (_lock)
            {
                return missionTemplates.Where(m => m.parameters != null).SelectMany(m => m.parameters).ToList();
            }
        }
    }
}