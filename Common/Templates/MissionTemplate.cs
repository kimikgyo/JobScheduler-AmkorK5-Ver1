using Common.Models.Bases;
using System.Text.Json.Serialization;

namespace Common.Templates
{
    public enum Service
    {
        WORKER,
        ELEVATOR,
        MIDDLEWARE,
        TRAFFIC
    }

    public enum MissionTemplateSubType
    {
        //Move

        NONE,
        CHARGERMOVE,
        WAITMOVE,
        RESETMOVE,
        POSITIONMOVE,
        SOURCEMOVE,
        DESTINATIONMOVE,
        STOPOVERMOVE,
        SOURCESTOPOVERMOVE,
        DESTINATIONSTOPOVERMOVE,
        ELEVATORWAITMOVE,
        ELEVATORENTERMOVE,
        ELEVATOREXITMOVE,
        SWITCHINGMAP,
        RIGHTTURN,
        LEFTTURN,

        //Action
        SOURCEACTION,

        DESTINATIONACTION,
        ELEVATORSOURCEFLOOR,
        ELEVATORDESTINATIONFLOOR,
        PICK,
        DROP,
        WAIT,
        CHARGE,
        CHARGECOMPLETE,
        RESET,
        CANCEL,
        CALL,
        DOOROPEN,
        ENTERCOMPLETE,
        DOORCLOSE,
        EXITCOMPLETE,
    }

    public enum MissionTemplateType
    {
        NONE,
        MOVE,
        ACTION,
        SUPPLYACTION,
        RECOVERYACTION,
    }

    public class MissionTemplate
    {
        [JsonPropertyOrder(2)] public string name { get; set; }
        [JsonPropertyOrder(3)] public string service { get; set; }
        [JsonPropertyOrder(4)] public string type { get; set; }
        [JsonPropertyOrder(5)] public string subType { get; set; }
        [JsonPropertyOrder(6)] public bool isLook { get; set; }
        [JsonPropertyOrder(7)] public List<Parameter> parameters { get; set; } = new List<Parameter>();
        [JsonPropertyOrder(9)] public List<PreReport> preReports { get; set; } = new List<PreReport>();
        [JsonPropertyOrder(11)] public List<PostReport> postReports { get; set; } = new List<PostReport>();

        // 사람용 요약 (디버거/로그에서 보기 좋게)
        public override string ToString()
        {
            string parametersStr;
            string preReportsStr;
            string postReportsStr;

            if (parameters != null && parameters.Count > 0)
            {
                // 리스트 안의 Parameter 각각을 { ... } 모양으로 변환
                var items = parameters
                    .Select(p => $"{{ key={p.key}, value={p.value} }}");

                // 여러 개 항목을 ", " 로 이어붙임
                parametersStr = string.Join(", ", items);
            }
            else
            {
                // 값이 없으면 빈 중괄호로 표시
                parametersStr = "{}";
            }

            if (preReports != null && preReports.Count > 0)
            {
                // 리스트 안의 Parameter 각각을 { ... } 모양으로 변환
                var items = preReports
                    .Select(p => $"{{ ceid={p.ceid}, eventName={p.eventName},rptid = {p.rptid} }}");

                // 여러 개 항목을 ", " 로 이어붙임
                preReportsStr = string.Join(", ", items);
            }
            else
            {
                preReportsStr = "{}";
            }

            if (postReports != null && postReports.Count > 0)
            {
                // 리스트 안의 Parameter 각각을 { ... } 모양으로 변환
                var items = postReports
                    .Select(p => $"{{ ceid={p.ceid}, eventName={p.eventName},rptid = {p.rptid} }}");

                // 여러 개 항목을 ", " 로 이어붙임
                postReportsStr = string.Join(", ", items);
            }
            else
            {
                postReportsStr = "{}";
            }
            return
                $"name = {name,-5}" +
                $"service = {service,-5}" +
                $",type = {type,-5}" +
                $",subType = {subType,-5}" +
                $",isLook = {isLook,-5}" +
                $",parameters = [{parametersStr,-5}]" +
                $",preReports = [{preReportsStr,-5}]" +
                $",postReports = [{postReportsStr,-5}]";
        }
    }
}