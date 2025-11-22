namespace Common.DTOs.MQTTs.Workers
{
    public class Subscribe_WorkerPayloadDto
    {
        public bool isLoaded { get; set; }
        public double? weightKg { get; set; }

        public override string ToString()
        {
            return

                $"isLoaded = {isLoaded,-5}" +
                $",weightKg = {weightKg,-5}";
        }
    }
}
