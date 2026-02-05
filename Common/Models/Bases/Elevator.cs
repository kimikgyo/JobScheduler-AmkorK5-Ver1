using System.Text.Json.Serialization;

namespace Common.Models.Bases
{
    public enum ElevatorMode
    {
        AGVMODE,
        AGVMODE_CHANGING_NOTAGVMODE,
        NOTAGVMODE,
        NOTAGVMODE_CHANGING_AGVMODE
    }
    public enum ElevatorState
    {
        DISCONNECT,
        CONNECT,
        PROTOCOLERROR,
        PAUSE,
        RESUME,
        DOOROPEN_B1F,
        DOOROPEN_1F,
        DOOROPEN_2F,
        DOOROPEN_3F,
        DOOROPEN_4F,
        DOOROPEN_5F,
        DOOROPEN_6F,
        DOORCLOSE_B1F,
        DOORCLOSE_1F,
        DOORCLOSE_2F,
        DOORCLOSE_3F,
        DOORCLOSE_4F,
        DOORCLOSE_5F,
        DOORCLOSE_6F,
        UPDRIVING_B1F,
        UPDRIVING_1F,
        UPDRIVING_2F,
        UPDRIVING_3F,
        UPDRIVING_4F,
        UPDRIVING_5F,
        UPDRIVING_6F,
        DOWNDRIVING_B1F,
        DOWNDRIVING_1F,
        DOWNDRIVING_2F,
        DOWNDRIVING_3F,
        DOWNDRIVING_4F,
        DOWNDRIVING_5F,
        DOWNDRIVING_6F,
    }

    public class Elevator
    {
        [JsonPropertyOrder(1)] public string id { get; set; }
        [JsonPropertyOrder(2)] public string name { get; set; }
        [JsonPropertyOrder(3)] public string state { get; set; }
        [JsonPropertyOrder(4)] public string mode { get; set; }
        [JsonPropertyOrder(5)] public string modeChangeRequest { get; set; }
        [JsonPropertyOrder(6)] public DateTime createAt { get; set; }
        [JsonPropertyOrder(7)] public DateTime? updateAt { get; set; }

        public override string ToString()
        {
            return
                $" id = {id,-5}" +
                $",name = {name,-5}" +
                $",state = {state,-5}" +
                $",mode = {mode,-5}" +
                $",modeChangeRequest = {modeChangeRequest,-5}" +
                $",createAt = {createAt,-5}" +
                $",updateAt = {updateAt,-5}";
        }

    }
}
