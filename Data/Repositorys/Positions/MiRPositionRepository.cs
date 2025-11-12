using Common.Models.Jobs;

namespace Data.Repositorys.Positions
{
    public partial class PositionRepository
    {
        public List<Position> MiR_GetAll()
        {
            lock (_lock)
            {
                return _positions.Where(m => m.source == "mir").ToList();
            }
        }


        //점유하고있지않은 포지션
        public List<Position> MiR_GetNotOccupied(string group, string subType)
        {
            lock (_lock)
            {
                return _positions.Where(m => m.source == "mir" && m.isEnabled == true && m.group == group && m.subType == subType &&m.isOccupied == false).ToList();
            }
        }

        public List<Position> MiR_GetByMapId(string mapid)
        {
            lock (_lock)
            {
                return _positions.Where(m => m.source == "mir" && m.isEnabled == true && m.mapId == mapid).ToList();
            }
        }

        public List<Position> MiR_GetByPosValue(double x, double y, string mapid)
        {
            lock (_lock)
            {
                return MiR_GetByMapId(mapid).Where(m => m.source == "mir" && m.x == x && m.y == y).ToList();
            }
        }

        public Position MiR_GetById(string id)
        {
            lock (_lock)
            {
                return _positions.FirstOrDefault(m => m.source == "mir" && m.id == id);
            }
        }

        public Position MiR_GetByname(string name)
        {
            lock (_lock)
            {
                return _positions.FirstOrDefault(m => m.source == "mir" && m.name == name);
            }
        }
    }
}