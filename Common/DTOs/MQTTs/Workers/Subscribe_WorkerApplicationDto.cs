namespace Common.DTOs.MQTTs.Workers
{
    public class Subscribe_WorkerApplicationDto
    {
        public bool isActive { get; set; }

        public override string ToString()
        {
            return
                $"isActive = {isActive,-5}";
        }
    }
}
