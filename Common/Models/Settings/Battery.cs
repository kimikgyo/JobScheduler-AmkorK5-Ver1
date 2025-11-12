namespace Common.Models.Settings
{
    public class Battery
    {
        public double minimum { get; set; }
        public double crossCharge { get; set; }
        public double chargeStart { get; set; }
        public double chargeEnd { get; set; }
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