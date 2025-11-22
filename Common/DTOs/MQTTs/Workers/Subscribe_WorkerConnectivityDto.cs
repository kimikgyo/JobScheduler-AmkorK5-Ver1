namespace Common.DTOs.MQTTs.Workers
{
    public class Subscribe_WorkerConnectivityDto
    {
        public bool online { get; set; }
        public int? rssi { get; set; }

        public override string ToString()
        {
            return
               $"online = {online,-5}" +
               $",rssi = {rssi,-5}";
        }
    }
}
