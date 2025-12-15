namespace Common.Models.Settings
{
    public class Battery
    {
        public double minimum { get; set; }         //Job을 받을수있는 최소 배터리
        public double crossCharge { get; set; }     //스위칭[한대가 충전하고있으면 현재충전하고있는 로봇이 End 배터리 이상이면 교차하여 충전]
        public double chargeStart { get; set; }     //충전 시작 배터리
        public double chargeEnd { get; set; }       //충전 완료 배터리
        public DateTime createAt { get; set; }
        public DateTime? updatedAt { get; set; }

        // 사람용 요약 (디버거/로그에서 보기 좋게)
        public override string ToString()
        {
            return
                $"minimum = {minimum,-5}" +
                $",crossCharge = {crossCharge,-5}" +
                $",chargeStart = {chargeStart,-5}" +
                $",chargeEnd = {chargeEnd,-5}" +
                $",createAt = {createAt,-5}" +
                $",updatedAt = {updatedAt,-5}";
        }
    }
}