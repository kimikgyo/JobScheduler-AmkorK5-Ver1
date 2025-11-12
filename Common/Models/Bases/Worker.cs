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
        [JsonPropertyOrder(1)] public string  id { get; set; }
        [JsonPropertyOrder(2)] public string  source { get; set; }
        [JsonPropertyOrder(3)] public bool    isOnline { get; set; }
        [JsonPropertyOrder(4)] public bool    isActive { get; set; }
        [JsonPropertyOrder(5)] public bool    isMiddleware { get; set; } = false;
        [JsonPropertyOrder(6)] public string  acsmissionId { get; set; }
        [JsonPropertyOrder(7)] public string  group { get; set; } = "";
        [JsonPropertyOrder(8)] public string  name { get; set; }
        [JsonPropertyOrder(9)] public string  mapId { get; set; }
        [JsonPropertyOrder(10)] public string mapName { get; set; }
        [JsonPropertyOrder(11)] public string state { get; set; }
        [JsonPropertyOrder(12)] public double batteryPercent { get; set; }
        [JsonPropertyOrder(13)] public double position_X { get; set; }
        [JsonPropertyOrder(14)] public double position_Y { get; set; }
        [JsonPropertyOrder(15)] public double position_Orientation { get; set; }
        [JsonPropertyOrder(16)] public string PositionId{ get; set; }
        [JsonPropertyOrder(17)] public string PositionName { get; set; }

        // 사람용 요약 (디버거/로그에서 보기 좋게)
        public override string ToString()
        {
            return
                $"id = {id,-5}" +
                $",source = {source,-5}" +
                $",isOnline = {isOnline,-5}" +
                $",isActive = {isActive,-5}" +
                $",isMiddleware = {isMiddleware,-5}" +
                $",acsmissionId = {acsmissionId,-5}" +
                $",group = {group,-5}" +
                $",name = {name,-5}" +
                $",mapId = {mapId,-5}" +
                $",mapName = {mapName,-5}" +
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