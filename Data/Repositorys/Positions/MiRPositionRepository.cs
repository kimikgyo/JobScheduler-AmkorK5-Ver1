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
            //          y + rough
            //          ▲
            //          │     후보로 남는 영역(사각형)
            // x - rough  ├───────────────┐  x + rough
            //          │       ● robot │
            //          └───────────────┘
            //          │
            //          ▼
            //        y - rough

            // ============================================================
            // [목적]
            // - 로봇의 현재 좌표(x,y)가 특정 Position의 "점유 영역" 안에 들어오면,
            //   그 Position들을 리스트로 반환한다.
            //
            // [전략]
            // - 전체 _positions를 전부 원형거리계산(dx^2+dy^2) 하면 느릴 수 있음.
            // - 그래서 2단계로 나눔:
            //   (1) rough(사각형)로 1차 후보를 빠르게 줄이고
            //   (2) 남은 후보에 대해서만 원형 점유 판정(정확 판정)
            // ============================================================

            //lock (_lock)
            //{
            //    double PositionTolerance = 0.06; // 오차범위 보통 미터 단위이므로 5cm 이면 0.05로 한다
            //    return _positions.Where(m => m.source == "mir" && m.mapId == mapid && Math.Abs(m.x - x) <= PositionTolerance && Math.Abs(m.y - y) <= PositionTolerance).ToList();

            //}
            lock (_lock)
            {
                // ============================================================
                // 1) 1차 후보 컷(사각형/Bounding Box)
                // ============================================================
                // rough:
                // - 로봇 좌표(x,y)를 중심으로
                //   (x±rough, y±rough) 범위 안에 들어오는 포지션만 후보로 뽑는다.
                //
                // 장점:
                // - abs 비교는 sqrt 거리 계산보다 싸고 빠름
                // - _positions 전체를 매번 거리 계산하는 일을 줄여 성능이 좋아짐
                //
                // 주의:
                // - rough를 너무 작게 잡으면, 실제로는 원 안에 들어오는 Position인데
                //   사각형 컷에서 탈락할 수 있음(누락)
                // - rough를 너무 크게 잡으면 후보가 많아져 성능이 떨어질 수 있음
                // ============================================================

                double rough = 0.07; // 1차 컷용 "사각형 반경"(현장/좌표 스케일에 맞게 튜닝)
                var candidates = _positions
                    .Where(p => p != null)  // null 방지
                    .Where(p => p.source == "mir" && p.mapId == mapid) // MiR 포지션 + 같은 맵만
                    .Where(p => Math.Abs(p.x - x) <= rough && Math.Abs(p.y - y) <= rough)  // 사각형 컷
                    .ToList();

                // ============================================================
                // 2) 정확 판정(원형 점유 판정)
                // ============================================================
                // candidates는 이미 줄어든 상태이므로,
                // 여기서 각 후보에 대해 "로봇이 점유 반경 안에 들어왔는지"를 체크한다.
                //
                // 반환값:
                // - 로봇이 점유했다고 판단되는 Position들의 리스트
                // ============================================================

                var result = new List<Position>();
                foreach (var pos in candidates)
                {
                    // IsOccupiedByRobot:
                    // - 로봇 중심(robotX,robotY)과 Position 중심(pos.x,pos.y)의 거리 비교
                    // - 거리 <= occR 이면 점유로 판단
                    if (IsOccupiedByRobot(pos, x, y))
                    {
                        result.Add(pos);
                    }
                }

                return result;
            }
        }

        private bool IsOccupiedByRobot(Position pos, double robotX, double robotY)
        {
            //       (원: occR 반경)
            //       .-~~~~~~~~~~~~-.
            //    .- ~~-.
            //   /      ● robot         \
            //  | (x, y) |
            //  |                        | pos ● 가 이 원 안이면 점유(true)
            //   \                      /
            //    `-._              _.- '
            //         `-._    _.- '
            //              `--'
            // ============================================================
            // [목적]
            // - 로봇 중심(robotX,robotY)이 Position(pos.x,pos.y)의 점유 반경(occR) 안이면 true
            //
            // [핵심 포인트]
            // - sqrt를 쓰지 않고 제곱거리 비교를 사용 (성능 + 정확도)
            //   실제 거리: sqrt(dx^2 + dy^2)
            //   비교만 필요하므로:
            //   dx^2 + dy^2 <= occR^2 로 계산 (sqrt 불필요)
            // ============================================================

            if (pos == null) return false;

            // ============================================================
            // 점유 반경 구성요소
            // ============================================================
            // robotRadius:
            // - 로봇을 원으로 단순화했을 때의 반지름(현장 실측값으로 넣는 게 가장 정확)
            //
            // positionRadius:
            // - Position도 점(0)으로 보기보다 약간의 "스팟 크기"가 있다고 보고 반경 부여
            //
            // safety:
            // - 오차/센서노이즈/좌표 흔들림 대비 마진
            // ============================================================

            double robotRadius = 0.03;     // 로봇 반경(예시)
            double positionRadius = 0.01;  // 포지션 스팟 반경(예시)
            double safety = 0.01;          // 안전 마진(예시)

            // 최종 점유 판정 반경(로봇 + 스팟 + 마진)
            double occR = robotRadius + positionRadius + safety;
            // 제곱 반경(거리 비교용)
            double occR2 = occR * occR;

            // 로봇과 포지션 중심 좌표 차이
            double dx = robotX - pos.x;
            double dy = robotY - pos.y;

            // 제곱거리 <= 제곱반경 이면 "점유"
            return (dx * dx + dy * dy) <= occR2;
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
                var aaa = _positions;
                return _positions.FirstOrDefault(m => m.source == "mir"
                                                && ((m.id == value)
                                                || (m.name == value)
                                                || (m.linkedFacility == value))
                                                );
            }
        }
    }
}