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

        public List<Position> MiR_GetIsOccupied(string group, string subType)
        {
            lock (_lock)
            {
                if (group == null)
                {
                    return _positions.Where(m => m.source == "mir" && m.isEnabled == true && m.subType == subType && m.isOccupied == true).ToList();
                }
                else
                {
                    return _positions.Where(m => m.source == "mir" && m.isEnabled == true && m.group == group && m.subType == subType && m.isOccupied == true).ToList();
                }
            }
        }

   

        //점유하고있지않은 포지션
        public List<Position> MiR_GetNotOccupied(string group, string subType)
        {
            lock (_lock)
            {
                if (group == null)
                {
                    return _positions.Where(m => m.source == "mir" && m.isEnabled == true && m.subType == subType && m.isOccupied == false).ToList();
                }
                else
                {
                    return _positions.Where(m => m.source == "mir" && m.isEnabled == true && m.group == group && m.subType == subType && m.isOccupied == false).ToList();
                }
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
                double PositionTolerance = 0.05; // 오차범위 보통 미터 단위이므로 5cm 이면 0.05로 한다
                return _positions.Where(m => m.source == "mir" && m.mapId == mapid && Math.Abs(m.x - x) <= PositionTolerance && Math.Abs(m.y - y) <= PositionTolerance).ToList();
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

        public List<Position> MiR_GetBySubType(string subtype)
        {
            lock (_lock)
            {
                return _positions.Where(m => m.source == "mir" && m.isEnabled == true && m.subType == subtype).ToList();
            }
        }

        public Position MiR_GetById_Name_linkedFacility(string value)
        {
            lock (_lock)
            {
                return _positions.FirstOrDefault(m => m.source == "mir"
                                                && ((m.id == value)
                                                || (m.name == value)
                                                || (m.linkedFacility == value))
                                                );
            }
        }
    }
}