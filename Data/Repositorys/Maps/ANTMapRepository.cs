using Common.Models.Bases;

namespace Data.Repositorys.Maps
{
    public partial class MapRepository
    {
        public List<Map> ANT_GetAll()
        {
            lock (_lock)
            {
                return _maps.Where(m => m.source == "ant").ToList();
            }
        }

        public Map ANT_GetById(string id)
        {
            lock (_lock)
            {
                return _maps.FirstOrDefault(m => m.source == "ant" && m.id == id);
            }
        }
    }
}
