namespace Common.DTOs.Bases
{

    public class ApiPutRequstDtoBattery
    {
        public double minimum { get; set; }
        public double crossCharge { get; set; }
        public double chargeStart { get; set; }
        public double chargeEnd { get; set; }

        public override string ToString()
        {
            return
                $"minimum = {minimum,-5}" +
                $",chargeStart = {chargeStart,-5}" +
                $",chargeEnd = {chargeEnd,-5}";
        }
    }
}