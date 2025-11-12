using Common.Models.Bases;

namespace Data.Repositorys.Maps
{
    public partial class MapRepository
    {
        public List<Map> MiR_GetAll()
        {
            lock (_lock)
            {
                return _maps.Where(m => m.source == "mir").ToList();
            }
        }

        public Map MiR_GetById(string id)
        {
            lock (_lock)
            {
                return _maps.FirstOrDefault(m => m.source == "mir" && m.id == id);
            }
        }
    }
}