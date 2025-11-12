using Common.Models.Jobs;

namespace Data.Repositorys.Workers
{
    public partial class WorkerRepository
    {
        public List<Worker> MiR_GetAll()
        {
            lock (_lock)
            {
                return _workers.Where(m => m.source == "mir").ToList();
            }
        }

        public List<Worker> MiR_GetByActive()
        {
            lock (_lock)
            {
                return _workers.Where(m => m.source == "mir" && m.isActive == true && m.isOnline == true).ToList();
            }
        }

        public List<Worker> MiR_GetByConnect()
        {
            lock (_lock)
            {
                return _workers.Where(m => m.source == "mir" && m.isOnline == true).ToList();
            }
        }

        public Worker MiR_GetById(string id)
        {
            lock (_lock)
            {
                return _workers.FirstOrDefault(m => m.source == "mir" && m.id == id);
            }
        }
    }
}