namespace Common.DTOs.MQTTs.Workers
{
    public class Subscribe_WorkerBatteryDto
    {
        public double? percent { get; set; }
        public bool isCharging { get; set; }

        public override string ToString()
        {
            return
                $"percent = {percent,-5}" +
                $",isCharging = {isCharging,-5}";
        }
    }
}
