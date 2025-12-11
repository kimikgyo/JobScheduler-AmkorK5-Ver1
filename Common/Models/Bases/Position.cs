using System.Text.Json.Serialization;

namespace Common.Models.Jobs
{
    public enum PositionSubType
    {
        ALLOWALL,       //모두허용
        SOURCE,         //출발지
        DESTINATION,    //목적지
        ELEVATOR,       //엘리베이터
        WAIT,           //대기
        CHARGE,          //충전
        ELEVATORWAIT,
        ELEVATORENTER,
        ELEVATOREXIT,
    }

    public enum NodeType
    {
        WAYPOINT,
        TRAFFIC,
        WORK,
        ELEVATOR,
    }

    public class Position
    {
        [JsonPropertyOrder(1)] public string id { get; set; }
        [JsonPropertyOrder(2)] public string positionId { get; set; }
        [JsonPropertyOrder(3)] public string source { get; set; }
        [JsonPropertyOrder(4)] public string group { get; set; }
        [JsonPropertyOrder(5)] public string type { get; set; }
        [JsonPropertyOrder(6)] public string subType { get; set; }
        [JsonPropertyOrder(7)] public string mapId { get; set; }
        [JsonPropertyOrder(8)] public string name { get; set; }
        [JsonPropertyOrder(9)] public double x { get; set; }
        [JsonPropertyOrder(10)] public double y { get; set; }
        [JsonPropertyOrder(11)] public double theth { get; set; }
        [JsonPropertyOrder(12)] public bool isDisplayed { get; set; }
        [JsonPropertyOrder(13)] public bool isEnabled { get; set; }
        [JsonPropertyOrder(14)] public bool isOccupied { get; set; }
        [JsonPropertyOrder(15)] public string linkedArea { get; set; }
        [JsonPropertyOrder(16)] public string linkedFacility { get; set; }
        [JsonPropertyOrder(17)] public string linkedRobotId { get; set; }
        [JsonPropertyOrder(18)] public bool hasCharger { get; set; }
        [JsonPropertyOrder(19)] public string nodeType { get; set; }

        public override string ToString()
        {
            return
                $"id = {id,-5}" +
                $",positionId = {positionId,-5}" +
                $",source = {source,-5}" +
                $",group = {group,-5}" +
                $",type = {type,-5}" +
                $",subType = {subType,-5}" +
                $",mapId = {mapId,-5}" +
                $",name = {name,-5}" +
                $",x = {x,-5}" +
                $",y = {y,-5}" +
                $",theth = {theth,-5}" +
                $",isDisplayed = {isDisplayed,-5}" +
                $",isEnabled = {isEnabled,-5}" +
                $",isOccupied = {isOccupied,-5}" +
                $",linkedArea = {linkedArea,-5}" +
                $",linkedFacility = {linkedFacility,-5}" +
                $",linkedRobotId = {linkedRobotId,-5}" +
                $",hasCharger = {hasCharger,-5}" +
                $",nodeType = {nodeType,-5}";
        }

        //public string ToJson(bool indented = false)
        //{
        //    return JsonSerializer.Serialize(this, new JsonSerializerOptions
        //    {
        //        IncludeFields = true,
        //        WriteIndented = indented
        //    });
        //}
    }
}