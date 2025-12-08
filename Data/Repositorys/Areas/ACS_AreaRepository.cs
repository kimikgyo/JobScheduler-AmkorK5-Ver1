using Common.Models.Areas;
using Common.Models.Jobs;
using log4net;

namespace Data.Repositorys.Areas
{
    public class ACS_AreaRepository
    {
        private static readonly ILog logger = LogManager.GetLogger("ACS_Area"); //Function 실행관련 Log

        private readonly string connectionString;
        private readonly List<ACSArea>  _aCS_Areas = new List<ACSArea>(); // cached data
        private readonly object _lock = new object();

        public ACS_AreaRepository(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public void Add(ACSArea add)
        {
            lock (_lock)
            {
                _aCS_Areas.Add(add);
                logger.Info($"Add: {add}");
            }
        }
        public void update(ACSArea update)
        {
            lock (_lock)
            {
                logger.Info($"update: {update}");
            }
        }
        public void Delete()
        {
            lock (_lock)
            {
                _aCS_Areas.Clear();
                logger.Info($"Delete");
            }
        }

        public void Remove(ACSArea remove)
        {
            lock (_lock)
            {
                _aCS_Areas.Remove(remove);
                logger.Info($"Remove: {remove}");
            }
        }

        public List<ACSArea> GetAll()
        {
            lock (_lock)
            {
                return _aCS_Areas.ToList();
            }
        }
        /// <summary>
        /// 로봇 좌표 (rx, ry)가 Area 사각형 안에 있는지 체크
        /// </summary>
        public bool IsInsideArea(double rx, double ry, ACSArea area, double margin = 0.0)
        {
            // ----------------------------------------------
            // 1) 사각형의 절반 크기 계산
            // ----------------------------------------------
            // area.Width   : 전체 가로 길이(m)
            // area.Height  : 전체 세로 길이(m)
            //
            // 사각형은 중심을 기준으로 양쪽으로 퍼져 있으므로
            // 절반 크기(half width/height)를 계산한다.
            //
            // margin       : 좌표 오차를 허용하기 위해 살짝 확장할 여유 값
            //                ex) margin = 0.2 면 사각형을 0.2m 확대해서 체크
            //
            double halfW = area.width / 2.0 + margin;   // 중심 기준 가로 반경
            double halfH = area.height / 2.0 + margin;  // 중심 기준 세로 반경

            // ----------------------------------------------
            // 2) X축 방향으로 Area 안에 있는지 확인
            // ----------------------------------------------
            // rx - area.CenterX  : 로봇이 중심에서 좌우로 얼마나 떨어져 있는지 계산
            // Math.Abs(...)      : 음수/양수 제거 → 거리만 비교
            // <= halfW           : 가로 반경(halfW) 안이면 X축 기준으로 '안에 있음'
            //
            bool insideX = Math.Abs(rx - area.x) <= halfW;

            // ----------------------------------------------
            // 3) Y축 방향으로 Area 안에 있는지 확인
            // ----------------------------------------------
            // ry - area.CenterY  : 로봇이 중심에서 위/아래로 얼마나 떨어져 있는지
            // halfH              : 세로 반경
            //
            bool insideY = Math.Abs(ry - area.y) <= halfH;

            // ----------------------------------------------
            // 4) 최종 판정
            // ----------------------------------------------
            // 사각형 내부에 있으려면 X, Y 축 모두 범위 안에 있어야 한다.
            // 즉, 두 조건을 모두 만족해야 inside = true.
            //
            // insideX = true AND insideY = true → Area 내부
            // 하나라도 false → Area 바깥
            //
            return insideX && insideY;
        }
    }
}
