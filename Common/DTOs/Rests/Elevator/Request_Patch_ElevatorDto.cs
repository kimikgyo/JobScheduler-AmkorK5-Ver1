namespace Common.DTOs.Rests.Elevator
{



    public class Request_Patch_ElevatorDto
    {
        public string elevatorId {  get; set; }
        public string action { get; set; }
        public string targetMode { get; set; }
    }
}
