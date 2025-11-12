using Common.Models.Jobs;

namespace Data.Repositorys.Workers
{
    public partial class WorkerRepository
    {
        public List<Worker> ANT_GetAll()
        {
            lock (_lock)
            {
                return _workers.Where(m => m.source == "ant").ToList();
            }
        }

        public List<Worker> ANT_GetByActive()
        {
            lock (_lock)
            {
                //ANT Robot을 제외시키면 X,Y 좌표값이 0이 된다
                return _workers.Where(m => m.source == "ant" && m.isOnline == true && m.isActive == true && m.state != null && m.position_X !=0 && m.position_Y != 0).ToList();
            }
        }

        public List<Worker> ANT_GetByConnect()
        {
            lock (_lock)
            {
                return _workers.Where(m => m.source == "ant" && m.isOnline == true).ToList();
            }
        }

        public Worker ANT_GetById(string id)
        {
            lock (_lock)
            {
                return _workers.FirstOrDefault(m => m.source == "ant" && m.id == id);
            }
        }
    }
}