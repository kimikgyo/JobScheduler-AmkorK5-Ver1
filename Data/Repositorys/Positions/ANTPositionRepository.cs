using Common.Models.Jobs;
using System.Data;

namespace Data.Repositorys.Positions
{
    public partial class PositionRepository
    {
        public List<Position> ANT_GetAll()
        {
            lock (_lock)
            {
                return _positions.Where(m => m.source == "ant" && m.isEnabled == true).ToList();
            }
        }

        public List<Position> ANT_GetIsOccupied(string group, string subType)
        {
            lock (_lock)
            {
                if (group == null)
                {
                    return _positions.Where(m => m.source == "ant" && m.isEnabled == true && m.subType == subType && m.isOccupied == true).ToList();
                }
                else
                {
                    return _positions.Where(m => m.source == "ant" && m.isEnabled == true && m.group == group && m.subType == subType && m.isOccupied == true).ToList();
                }
            }
        }

        public List<Position> ANT_linkedWorkerPosition(string workerId)
        {
            lock (_lock)
            {
                return _positions.Where(m => m.source == "ant" && m.isEnabled == true && m.linkedRobotId == workerId).ToList();
            }
        }

        //점유하고있지않은 포지션
        public List<Position> ANT_GetNotOccupied(string group, string subType)
        {
            lock (_lock)
            {
                if (group == null)
                {
                    return _positions.Where(m => m.source == "ant" && m.isEnabled == true && m.subType == subType && m.isOccupied == false).ToList();
                }
                else
                {
                    return _positions.Where(m => m.source == "ant" && m.isEnabled == true && m.group == group && m.subType == subType && m.isOccupied == false).ToList();
                }
            }
        }

        public List<Position> ANT_GetByMapId(string mapid)
        {
            lock (_lock)
            {
                return _positions.Where(m => m.source == "ant" && m.isEnabled == true && m.mapId == mapid).ToList();
            }
        }

        public List<Position> ANT_GetByPosValue(double x, double y, string mapid)
        {
            lock (_lock)
            {
                //오차범위
                double tolerance = 0.5;
                //ANT기준 오차범위 10m
                // 유클리드 거리 계산
                //포지션 X좌표와 Worker X좌표의 차이 (X축 거리)
                //X축 거리의 제곱
                //포지션 Y좌표와 Worker Y좌표의 차이 (Y축 거리)
                //Y축 거리의 제곱
                //X² + Y²
                //제곱근을 씌워 최종 거리 계산
                return _positions.Where(m => m.source == "ant" && m.mapId == mapid
                        && Math.Sqrt(Math.Pow(m.x - x, 2) + Math.Pow(m.y - y, 2)) <= tolerance).ToList();
            }
        }

        public Position ANT_GetById(string id)
        {
            lock (_lock)
            {
                return _positions.FirstOrDefault(m => m.source == "ant" && m.id == id);
            }
        }

        public Position ANT_GetById_Name_linkedFacility(string value)
        {
            lock (_lock)
            {
                return _positions.FirstOrDefault(m => m.source == "ant"
                                                && ((m.id == value)
                                                || (m.name == value)
                                                || (m.linkedFacility == value))
                                                );
            }
        }

        public Position ANT_GetByname(string name)
        {
            lock (_lock)
            {
                return _positions.FirstOrDefault(m => m.source == "ant" && m.name == name);
            }
        }
    }
}