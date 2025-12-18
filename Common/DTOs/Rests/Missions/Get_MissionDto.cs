using Common.Models.Bases;
using Common.Templates;
using System.Text.Json.Serialization;

namespace Common.DTOs.Rests.Missions
{
    public class Get_MissionDto
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
        [JsonPropertyOrder(17)] public string assignedWorkerName { get; set; }             //할당된 Worker
        [JsonPropertyOrder(18)] public DateTime createdAt { get; set; }                  // 생성 시각
        [JsonPropertyOrder(19)] public DateTime? updatedAt { get; set; }
        [JsonPropertyOrder(20)] public DateTime? finishedAt { get; set; }
        [JsonPropertyOrder(21)] public DateTime? sequenceUpdatedAt { get; set; }  // 시퀀스가 마지막으로 변경된 시간 재정렬 발생 시 이 값이 갱신됨
        [JsonPropertyOrder(21)] public List<Parameter> parameters { get; set; }
        [JsonPropertyOrder(22)] public List<PreReport> preReports { get; set; }
        [JsonPropertyOrder(23)] public List<PostReport> postReports { get; set; }

        public override string ToString()
        {
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
                $",assignedWorkerName = {assignedWorkerName,-5}" +
                $",createdAt = {createdAt,-5}" +
                $",updatedAt = {updatedAt,-5}" +
                $",finishedAt = {finishedAt,-5}" +
                $",sequenceUpdatedAt = {sequenceUpdatedAt,-5}" +
                $",parameters = {parameters,-5}" +
                $",preReports = {preReports,-5}" +
                $",postReports = {postReports,-5}";
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