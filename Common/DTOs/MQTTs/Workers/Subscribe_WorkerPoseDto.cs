namespace Common.DTOs.MQTTs.Workers
{
    public class Subscribe_WorkerPoseDto
    {
        public double? x { get; set; }
        public double? y { get; set; }
        public double? theta { get; set; }
        public string mapId { get; set; }

        public override string ToString()
        {
            return
                $"x = {x,-5}" +
                $",y = {y,-5}" +
                $",theta = {theta,-5}" +
                $",mapId = {mapId,-5}";
        }
    }
}