using System.Text.Json.Serialization;

namespace Common.DTOs.MQTTs.Missions
{
    public class Subscribe_MissionRowDataDto
    {
        [JsonPropertyOrder(1)] public string missionid { get; set; }
        [JsonPropertyOrder(2)] public string missiontype { get; set; }
        [JsonPropertyOrder(3)] public string parameters { get; set; }
        [JsonPropertyOrder(4)] public int state { get; set; }
        [JsonPropertyOrder(5)] public int navigationstate { get; set; }
        [JsonPropertyOrder(6)] public int transportstate { get; set; }
        [JsonPropertyOrder(7)] public string fromnode { get; set; }
        [JsonPropertyOrder(8)] public string tonode { get; set; }
        [JsonPropertyOrder(9)] public bool isloaded { get; set; }
        [JsonPropertyOrder(10)] public string payload { get; set; }
        [JsonPropertyOrder(11)] public int priority { get; set; }
        [JsonPropertyOrder(12)] public string assignedto { get; set; }
        [JsonPropertyOrder(13)] public string payloadstatus { get; set; }
        [JsonPropertyOrder(14)] public DateTime deadline { get; set; }
        [JsonPropertyOrder(15)] public DateTime dispatchtime { get; set; }
        [JsonPropertyOrder(16)] public string timetodestination { get; set; }
        [JsonPropertyOrder(17)] public DateTime arrivingtime { get; set; }
        [JsonPropertyOrder(18)] public int totalmissiontime { get; set; }
        [JsonPropertyOrder(19)] public int schedulerstate { get; set; }
        [JsonPropertyOrder(20)] public int stateinfo { get; set; }
        [JsonPropertyOrder(21)] public string askedforcancellation { get; set; }
        [JsonPropertyOrder(22)] public string cancellationreason { get; set; }
        [JsonPropertyOrder(23)] public int groupid { get; set; }
        [JsonPropertyOrder(24)] public string missionrule { get; set; }
        [JsonPropertyOrder(25)] public bool istoday { get; set; }

        public override string ToString()
        {
            return
               $" missionid  = {missionid,-5}" +
               $",missiontype  = {missiontype,-5}" +
               $",parameters  = {parameters,-5}" +
               $",state  = {state,-5}" +
               $",navigationstate  = {navigationstate,-5}" +
               $",transportstate  = {transportstate,-5}" +
               $",fromnode  = {fromnode,-5}" +
               $",tonode  = {tonode,-5}" +
               $",isloaded  = {isloaded,-5}" +
               $",payload  = {payload,-5}" +
               $",priority  = {priority,-5}" +
               $",assignedto  = {assignedto,-5}" +
               $",payloadstatus  = {payloadstatus,-5}" +
               $",deadline  = {deadline,-5}" +
               $",dispatchtime  = {dispatchtime,-5}" +
               $",timetodestination  = {timetodestination,-5}" +
               $",arrivingtime  = {arrivingtime,-5}" +
               $",totalmissiontime  = {totalmissiontime,-5}" +
               $",schedulerstate  = {schedulerstate,-5}" +
               $",stateinfo  = {stateinfo,-5}" +
               $",askedforcancellation  = {askedforcancellation,-5}" +
               $",cancellationreason  = {cancellationreason,-5}" +
               $",groupid  = {groupid,-5}" +
               $",missionrule  = {missionrule,-5}" +
               $",istoday  = {istoday,-5}";
        }
    }

}
