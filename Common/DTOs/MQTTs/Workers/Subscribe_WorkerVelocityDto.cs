namespace Common.DTOs.MQTTs.Workers
{
    public class Subscribe_WorkerVelocityDto
    {
        public double? linear { get; set; }
        public double? angular { get; set; }

        public override string ToString()
        {
            return

                $"linear = {linear,-5}" +
                $",angular = {angular,-5}";
        }
    }
}
