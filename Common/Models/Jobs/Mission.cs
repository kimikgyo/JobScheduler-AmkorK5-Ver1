using Common.Templates;
using System.Data;
using System.Text.Json.Serialization;

namespace Common.Models.Jobs
{
    public enum Service
    {
        WORKER,
        ELEVATOR,
        MIDDLEWARE
    }

    public enum MissionType
    {
        NONE,
        MOVE,
        ACTION,
        SUPPLYACTION,
        RECOVERYACTION,
        EVENT,
    }

    //MISSION 상태
    public enum MissionState
    {
        INIT,
        WORKERASSIGNED,             // Scheduler 워커 할당
        WAITING,                    // Scheduler 대기중
        SKIPPED,                    //미션을 스킵하는경우
        COMMANDREQUEST,              // Scheduler Post진행시
        COMMANDREQUESTCOMPLETED,     // Scheduler Post진행시
        PENDING,                    // WorkerService 대기중
        EXECUTING,                 // Worker 작업중
        CANCELED,                   // Worker 작업취소
        FAILED,                     // Worker에서 실패했을경우
        COMPLETED,                  // Worker에서 성공 완료
        ABORTINITED,                 // SchedulerAbort 접수
        ABORTCOMPLETED,             // SchedulerAbort 완료
        ABORTFAILED,                // SchedulerAbort 실패
        CANCELINITED,                // SchedulerCancel 접수
        CANCELINITCOMPLETED,        // SchedulerCancel 완료
        CNACELFAILED                // SchedulerCancel 실패
    }

    public enum MissionSubType
    {
        //MOVE

        NONE,
        CHARGERMOVE,
        SOURCESTOPOVERMOVE,
        DESTINATIONSTOPOVERMOVE,
        WAITMOVE,
        RESETMOVE,
        POSITIONMOVE,
        SOURCEMOVE,
        STOPOVERMOVE,
        ELEVATORWAITMOVE,
        ELEVATORENTERMOVE,
        ELEVATOREXITMOVE,
        DESTINATIONMOVE,
        SWITCHINGMAP,
        RIGHTTURN,
        LEFTTURN,

        //ACTION
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

    public class Mission
    {
        [JsonPropertyOrder(1)] public string orderId { get; set; }
        [JsonPropertyOrder(2)] public string jobId { get; set; }
        [JsonPropertyOrder(3)] public string guid { get; set; }
        [JsonPropertyOrder(4)] public string carrierId { get; set; }              // 자재 ID (nullable)
        [JsonPropertyOrder(5)] public string name { get; set; }
        [JsonPropertyOrder(6)] public string service { get; set; }
        [JsonPropertyOrder(7)] public string type { get; set; }
        [JsonPropertyOrder(8)] public string subType { get; set; }
        [JsonPropertyOrder(9)] public string linkedFacility { get; set; }
        [JsonPropertyOrder(10)] public int sequence { get; set; }                   //현재 명령의 실행 순서 이 값은 실행 전 재정렬에 따라 변경될 수 있음
        [JsonPropertyOrder(11)] public bool isLocked { get; set; }                   // 취소 불가
        [JsonPropertyOrder(12)] public int sequenceChangeCount { get; set; } = 0;   // 시퀀스가 변경된 누적 횟수 예: 재정렬이 3번 발생했다면 3
        [JsonPropertyOrder(13)] public int retryCount { get; set; } = 0;            // 명령 실패 시 재시도한 횟수 (기본값은 0)
        [JsonPropertyOrder(14)] public string state { get; set; }
        [JsonPropertyOrder(15)] public string specifiedWorkerId { get; set; }            //order 지정된 Worker
        [JsonPropertyOrder(16)] public string assignedWorkerId { get; set; }             //할당된 Worker
        [JsonPropertyOrder(17)] public DateTime createdAt { get; set; }                  // 생성 시각
        [JsonPropertyOrder(18)] public DateTime? updatedAt { get; set; }
        [JsonPropertyOrder(19)] public DateTime? finishedAt { get; set; }
        [JsonPropertyOrder(20)] public DateTime? sequenceUpdatedAt { get; set; }  // 시퀀스가 마지막으로 변경된 시간 재정렬 발생 시 이 값이 갱신됨
        [JsonPropertyOrder(21)] public string parametersJson { get; set; }        // DB 파라메타를 저장하기 위하여
        [JsonPropertyOrder(22)] public List<Parameta> parameters { get; set; } = new List<Parameta>();          // 명령 실행 시 필요한 추가 옵션을 JSON 문자열로 저장  예: 속도, 방향, 특수 처리 조건 등
        [JsonPropertyOrder(23)] public string preReportsJson { get; set; }        //Mission 전에 보내지는 Report
        [JsonPropertyOrder(24)] public List<PreReport> preReports { get; set; } = new List<PreReport>();
        [JsonPropertyOrder(25)] public string postReportsJson { get; set; }        //Mission 이후에 보내지는 Report
        [JsonPropertyOrder(26)] public List<PostReport> postReports { get; set; } = new List<PostReport>();

        // 사람용 요약 (디버거/로그에서 보기 좋게)
        public override string ToString()
        {
            string parametersStr;
            string preReportsStr;
            string postReportsStr;

            if (parameters != null && parameters.Count > 0)
            {
                // 리스트 안의 Parameta 각각을 { ... } 모양으로 변환
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
                // 리스트 안의 Parameta 각각을 { ... } 모양으로 변환
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
                // 리스트 안의 Parameta 각각을 { ... } 모양으로 변환
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
                $" orderId = {orderId,-5}" +
                $",jobId = {jobId,-5}" +
                $",guid = {guid,-5}" +
                $",carrierId = {carrierId,-5}" +
                $",name = {name,-5}" +
                $",service = {service,-5}" +
                $",type = {type,-5}" +
                $",subType = {subType,-5}" +
                $",linkedFacility = {linkedFacility,-5}" +
                $",sequence = {sequence,-5}" +
                $",isLocked = {isLocked,-5}" +
                $",sequenceChangeCount = {sequenceChangeCount,-5}" +
                $",retryCount = {retryCount,-5}" +
                $",state = {state,-5}" +
                $",specifiedWorkerId = {specifiedWorkerId,-5}" +
                $",assignedWorkerId = {assignedWorkerId,-5}" +
                $",createdAt = {createdAt,-5}" +
                $",updatedAt = {updatedAt,-5}" +
                $",finishedAt = {finishedAt,-5}" +
                $",sequenceUpdatedAt = {sequenceUpdatedAt,-5}" +
                $",parametersJson = {parametersJson,-5}" +
                $",parameters = [{parametersStr,-5}]" +
                $",preReportsJson = {preReportsJson,-5}" +
                $",preReports = [{preReportsStr,-5}]" +
                $",postReportsJson = {postReportsJson,-5}" +
                $",postReports = [{postReportsStr,-5}]";
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