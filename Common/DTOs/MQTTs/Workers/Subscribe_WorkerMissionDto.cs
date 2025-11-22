namespace Common.DTOs.MQTTs.Workers
{
    public class Subscribe_WorkerMissionDto
    {
        public string missionId { get; set; }
        public string missionText { get; set; }
        public string status { get; set; }

        public override string ToString()
        {
            return
               $"missionId = {missionId,-5}" +
               $",missionText = {missionText,-5}" +
               $",status = {status,-5}";
        }
    }
}