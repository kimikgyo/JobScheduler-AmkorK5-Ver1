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
        CHARGER,
    }

    public class Position
    {
        [JsonPropertyOrder(1)] public string id { get; set; }                   //ACS 포지션 Id
        [JsonPropertyOrder(2)] public string positionId { get; set; }           //Fleet 포지션Id
        [JsonPropertyOrder(3)] public string source { get; set; }               //어떠한 Robot인지
        [JsonPropertyOrder(4)] public string group { get; set; }                //ACS 설정한그룹
        [JsonPropertyOrder(5)] public string type { get; set; }                 //포지션 타입
        [JsonPropertyOrder(6)] public string subType { get; set; }              //포지션 서브타입
        [JsonPropertyOrder(7)] public string mapId { get; set; }                //포지션 맵Id
        [JsonPropertyOrder(8)] public string name { get; set; }                 //포지션 이름
        [JsonPropertyOrder(9)] public double x { get; set; }                    //포지션 위치값
        [JsonPropertyOrder(10)] public double y { get; set; }                   //포지션 위치값
        [JsonPropertyOrder(11)] public double theth { get; set; }               //포지션 방향
        [JsonPropertyOrder(12)] public bool isDisplayed { get; set; }           //화면 설정
        [JsonPropertyOrder(13)] public bool isEnabled { get; set; }             //사용유무
        [JsonPropertyOrder(14)] public bool isOccupied { get; set; }            //점유 상태
        [JsonPropertyOrder(15)] public string linkedArea { get; set; }          //포지션 관련 Area
        [JsonPropertyOrder(16)] public string linkedZone { get; set; }          //포지션 관련 Area
        [JsonPropertyOrder(17)] public string linkedFacility { get; set; }      //포지션 관련 장비
        [JsonPropertyOrder(18)] public string linkedRobotId { get; set; }       //포지션 지정 Robot
        [JsonPropertyOrder(19)] public bool hasCharger { get; set; }            //충전기에 대한 포지션인지
        [JsonPropertyOrder(20)] public string nodeType { get; set; }            //포지션 로드타입

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
                $",linkedZone = {linkedZone,-5}" +
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