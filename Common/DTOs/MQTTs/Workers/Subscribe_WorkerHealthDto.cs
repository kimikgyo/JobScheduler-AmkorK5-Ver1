namespace Common.DTOs.MQTTs.Workers
{
    public class Subscribe_WorkerHealthDto
    {
        public List<string> alarms { get; set; }

        public override string ToString()
        {
            return
                $"alarms = {alarms,-5}";
        }
    }
}
