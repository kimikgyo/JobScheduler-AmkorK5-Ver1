using System.Text.Json.Serialization;

namespace Common.Models.Jobs
{
    public enum WorkerState
    {
        NONE,
        IDLE,             // 대기중
        PARKED,       // 미션 전 Or 후 대기중
        MOVEING,      // 이동중
        WORKING,    // TopModule 작업중
        CHARGING,   // 충전중
        MANUAL,     // 메뉴얼상태
        PAUSE,      // 일시정지
        ERROR,
        OFFLINE     //
    }

    public class Worker
    {
        [JsonPropertyOrder(1)] public string id { get; set; }                           //RobotId
        [JsonPropertyOrder(2)] public string source { get; set; }                       //사용처
        [JsonPropertyOrder(3)] public bool isOnline { get; set; }                       //Online인지
        [JsonPropertyOrder(4)] public bool isActive { get; set; }                       //사용활성화 되어있는지
        [JsonPropertyOrder(5)] public bool isMiddleware { get; set; } = false;          //미들웨어를 사용중인지
        [JsonPropertyOrder(6)] public string missionId { get; set; }                    //Mission 진행중인지
        [JsonPropertyOrder(7)] public string missionName { get; set; }                  //Mission Name
        [JsonPropertyOrder(8)] public string group { get; set; } = "";                  //그룹이 지정되어있는지
        [JsonPropertyOrder(9)] public string name { get; set; }                         //Robot 이름
        [JsonPropertyOrder(10)] public string mapId { get; set; }                        //Robot 맵Id
        [JsonPropertyOrder(12)] public string state { get; set; }                       //Robot 상태
        [JsonPropertyOrder(13)] public double batteryPercent { get; set; }              //Robot 배터리
        [JsonPropertyOrder(14)] public double position_X { get; set; }                  //Robot 위치값
        [JsonPropertyOrder(15)] public double position_Y { get; set; }                  //Robot 위치값
        [JsonPropertyOrder(16)] public double position_Orientation { get; set; }        //Robot 위치값
        [JsonPropertyOrder(17)] public string PositionId { get; set; }                  //Robot 점유하고있는 포지션Id
        [JsonPropertyOrder(18)] public string PositionName { get; set; }                //Robot 점유하고 있는 포지션 이름

        // 사람용 요약 (디버거/로그에서 보기 좋게)
        public override string ToString()
        {
            return
                $"id = {id,-5}" +
                $",source = {source,-5}" +
                $",isOnline = {isOnline,-5}" +
                $",isActive = {isActive,-5}" +
                $",isMiddleware = {isMiddleware,-5}" +
                $",missionId = {missionId,-5}" +
                $",missionName = {missionName,-5}" +
                $",group = {group,-5}" +
                $",name = {name,-5}" +
                $",mapId = {mapId,-5}" +
                $",state = {state,-5}" +
                $",batteryPercent = {batteryPercent,-5}" +
                $",position_X = {position_X,-5}" +
                $",position_Y = {position_Y,-5}" +
                $",position_Orientation = {position_Orientation,-5}" +
                $",PositionId = {PositionId,-5}" +
                $",PositionName = {PositionName,-5}";
        }

        // 기계용 JSON (전송/저장에만 사용)
        //public string ToJson(bool indented = false)
        //{
        //    return JsonSerializer.Serialize(
        //        this,
        //        new JsonSerializerOptions
        //        {
        //            IncludeFields = true,
        //            WriteIndented = indented
        //        });
        //}
    }
}