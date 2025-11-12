using Common.Models.Bases;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Text.Json.Serialization;

namespace Common.Models.Jobs
{
    public enum OrderType
    {
     
    }

    public enum OrderSubType
    {
     
    }

    public enum OrderState
    {
        None,                   // Order 완료
        Queued,                 // Acknowledge -> TransferInitiated Init 상태
        Transferring,           // Job 실행시
        Paused,                 // Job 정지시
        Canceling,              // TransferCancelInitiated -> TransferCancelCompleted
        Aborting,               // TransferAbortInitiated  -> TransferAbortCompleted 까지
        Waiting,                // TransferInitiated -> VehicleArrived 까지 Job생성시
        JobTemplateNotFind
    }

    public enum ScenarioMessage
    {
        TransferInitiated,                      // 전송시작
        VehicleAssigned,                        // 차량할당
        VehicleDeparted,                        // 차량출발
        VehicleArrived,                         // 차량도착
        Transferring,                           // 전송중
        VehicleUnassigned,                      // 차량해제
        TransferCompleted,                      // 전송완료
        VehicleAcquireStarted,                  // 차량 PICK 시작
        VehicleAcquireCompleted,                // 차량 PICK 완료
        VehicleDepositStarted,                  // 차량 DROP 시작
        VehicleDepositCompleted,                // 차량 DROP 완료
        VehicleCouplerLoosenStarted,            // 커플러 풀기 시작
        VehicleCouplerLoosenComplete,           // 커플러 풀기 완료
        VehicleCapFastenStarted,                // 캡 고정 시작
        VehicleCapFastenCompleted,              // 캡 고정 완료
        VehicleCapLoosenStarted,                // 캡 풀기 시작
        VehicleCapLoosenCompleted,              // 캡 풀기 완료
        VehicleCouplerFastenStarted,            // 커플러 고정 시작
        VehicleCouplerFastenComplete,           // 커플러 고정 완료
        CarrierInstalled,                       // 케리어 PICK
        CarrierRemoved,                         // 케리어 DROP
        CarrierIDRead,                          // 케리어 Id 읽기
        OperatorInitiatedAction,                // CANCEL,Abort
        TransferAbortInitiated,
        TransferAbortCompleted,
        TransferUpdateCompleted,                //출발지 목적지를 변경할경우
        TransferCancelInitiated,
        TransferCancelCompleted
    }

    public class Order
    {
        [JsonPropertyOrder(1)] public string id { get; set; }                   // 고유 식별자
        [JsonPropertyOrder(2)] public string type { get; set; }                 // 운반, 충전, 대기 등
        [JsonPropertyOrder(3)] public string subType { get; set; }              // 운반, 충전, 대기 등
        [JsonPropertyOrder(4)] public string sourceId { get; set; }             // 출발 위치 ID
        [JsonPropertyOrder(5)] public string destinationId { get; set; }        // 도착 위치 ID
        [JsonPropertyOrder(6)] public string carrierId { get; set; }            // 자재 ID (nullable)
        [JsonPropertyOrder(7)] public string drumKeyCode { get; set; }          // 자재 KeyCode
        [JsonPropertyOrder(8)] public string orderedBy { get; set; }            // 지시자 ID 또는 이름
        [JsonPropertyOrder(9)] public DateTime orderedAt { get; set; }
        [JsonPropertyOrder(10)] public int priority { get; set; } = 0;          // 기본 우선순위
        [JsonPropertyOrder(11)] public OrderState stateCode { get; set; }
        [JsonPropertyOrder(12)] public string state { get; set; }               // 상태: 대기, 진행중, 완료 등
        [JsonPropertyOrder(13)] public string specifiedWorkerId { get; set; }   //지정된 Worker
        [JsonPropertyOrder(14)] public string assignedWorkerId { get; set; }    //할당된 Worker
        [JsonPropertyOrder(15)] public DateTime createdAt { get; set; }         // 생성 시각
        [JsonPropertyOrder(16)] public DateTime? updatedAt { get; set; }
        [JsonPropertyOrder(17)] public DateTime? finishedAt { get; set; }

        // 사람용 요약 (디버거/로그에서 보기 좋게)
        public override string ToString()
        {
            return

                $" id = {id,-5}" +
                $",type = {type,-5}" +
                $",subType = {subType,-5}" +
                $",sourceId = {sourceId,-5}" +
                $",destinationId = {destinationId,-5}" +
                $",carrierId = {carrierId,-5}" +
                $",drumKeyCode = {drumKeyCode,-5}" +
                $",orderedBy = {orderedBy,-5}" +
                $",orderedAt = {orderedAt,-5}" +
                $",priority = {priority,-5}" +
                $",stateCode = {stateCode,-5}" +
                $",state = {state,-5}" +
                $",specifiedWorkerId = {specifiedWorkerId,-5}" +
                $",assignedWorkerId = {assignedWorkerId,-5}" +
                $",createdAt = {createdAt,-5}" +
                $",updatedAt = {updatedAt,-5}" +
                $",finishedAt = {finishedAt,-5}";
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