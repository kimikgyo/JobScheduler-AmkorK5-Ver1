using Common.Templates;
using System.Data;
using System.Text.Json.Serialization;

namespace Common.DTOs.Jobs
{
    public class ResponseDtoMission
    {
        [JsonPropertyOrder(1)] public string orderId { get; set; }
        [JsonPropertyOrder(2)] public string jobId { get; set; }
        [JsonPropertyOrder(3)] public string guid { get; set; }
        [JsonPropertyOrder(4)] public string carrierId { get; set; }              // 자재 ID (nullable)
        [JsonPropertyOrder(5)] public string name { get; set; }
        [JsonPropertyOrder(6)] public string service { get; set; }
        [JsonPropertyOrder(7)] public string type { get; set; }
        [JsonPropertyOrder(8)] public string subType { get; set; }
        [JsonPropertyOrder(9)] public int sequence { get; set; }                   //현재 명령의 실행 순서 이 값은 실행 전 재정렬에 따라 변경될 수 있음
        [JsonPropertyOrder(10)] public string linkedFacility { get; set; }
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
        [JsonPropertyOrder(21)] public List<Parameta> parameters { get; set; }
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
                $",sequence = {sequence,-5}" +
                $",linkedFacility = {linkedFacility,-5}" +
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

    public class PatchDtoMission
    {
        [JsonPropertyOrder(0)] public string orderId { get; set; }
        [JsonPropertyOrder(1)] public string destinationId { get; set; }

        public override string ToString()
        {
            return
            $"orderId = {orderId,-5}" +
            $"destinationId = {destinationId,-5}";
        }
    }

    public class ApiRequstDtoDeleteMission
    {
        [JsonPropertyOrder(1)] public string guid { get; set; }

        public override string ToString()
        {
            return
                $"guid = {guid,-5}";
        }
    }

    public class ApiRequestDtotPostMission
    {
        [JsonPropertyOrder(1)] public string orderId { get; set; }
        [JsonPropertyOrder(2)] public string jobId { get; set; }
        [JsonPropertyOrder(3)] public string guid { get; set; }
        [JsonPropertyOrder(4)] public string carrierId { get; set; }              // 자재 ID (nullable)
        [JsonPropertyOrder(5)] public string service { get; set; }
        [JsonPropertyOrder(6)] public string type { get; set; }
        [JsonPropertyOrder(7)] public string subType { get; set; }
        [JsonPropertyOrder(8)] public int sequence { get; set; }
        [JsonPropertyOrder(8)] public string linkedFacility { get; set; }
        [JsonPropertyOrder(9)] public bool isLocked { get; set; }
        [JsonPropertyOrder(10)] public int sequenceChangeCount { get; set; } = 0;
        [JsonPropertyOrder(11)] public int retryCount { get; set; } = 0;
        [JsonPropertyOrder(12)] public string state { get; set; }
        [JsonPropertyOrder(14)] public string specifiedWorkerId { get; set; }
        [JsonPropertyOrder(15)] public string assignedWorkerId { get; set; }
        [JsonPropertyOrder(16)] public List<Parameta> parameters { get; set; }

        // 사람용 요약 (디버거/로그에서 보기 좋게)
        public override string ToString()
        {
            string parametersStr;

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
                parametersStr = "";
            }
            return

              $" orderId = {orderId,-5}" +
              $",jobId = {jobId,-5}" +
              $",guid = {guid,-5}" +
              $",carrierId = {carrierId,-5}" +
              $",service = {service,-5}" +
              $",type = {type,-5}" +
              $",subType = {subType,-5}" +
              $",sequence = {sequence,-5}" +
              $",linkedFacility = {linkedFacility,-5}" +
              $",isLocked = {isLocked,-5}" +
              $",sequenceChangeCount = {sequenceChangeCount,-5}" +
              $",retryCount = {retryCount,-5}" +
              $",state = {state,-5}" +
              $",specifiedWorkerId = {specifiedWorkerId,-5}" +
              $",assignedWorkerId = {assignedWorkerId,-5}" +

                $",parameters = {parametersStr,-5}";
        }
    }

    public class MqttPublishDtoMission
    {
        [JsonPropertyOrder(1)] public string orderId { get; set; }
        [JsonPropertyOrder(2)] public string jobId { get; set; }
        [JsonPropertyOrder(3)] public string guid { get; set; }
        [JsonPropertyOrder(4)] public string carrierId { get; set; }              // 자재 ID (nullable)
        [JsonPropertyOrder(5)] public string name { get; set; }
        [JsonPropertyOrder(6)] public string service { get; set; }
        [JsonPropertyOrder(7)] public string type { get; set; }
        [JsonPropertyOrder(8)] public string subType { get; set; }
        [JsonPropertyOrder(9)] public int sequence { get; set; }                   //현재 명령의 실행 순서 이 값은 실행 전 재정렬에 따라 변경될 수 있음
        [JsonPropertyOrder(10)] public string linkedFacility { get; set; }
        [JsonPropertyOrder(11)] public bool isLocked { get; set; }                   // 취소 불가
        [JsonPropertyOrder(12)] public int sequenceChangeCount { get; set; } = 0;   // 시퀀스가 변경된 누적 횟수 예: 재정렬이 3번 발생했다면 3
        [JsonPropertyOrder(13)] public int retryCount { get; set; } = 0;            // 명령 실패 시 재시도한 횟수 (기본값은 0)
        [JsonPropertyOrder(14)] public string state { get; set; }
        [JsonPropertyOrder(15)] public string specifiedWorkerId { get; set; }            //order 지정된 Worker
        [JsonPropertyOrder(16)] public string assignedWorkerId { get; set; }             //할당된 Worker
        [JsonPropertyOrder(17)] public List<Parameta> parameters { get; set; }
        [JsonPropertyOrder(18)] public List<PreReport> preReports { get; set; }
        [JsonPropertyOrder(19)] public List<PostReport> postReports { get; set; }

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
                $",sequence = {sequence,-5}" +
                $",linkedFacility = {linkedFacility,-5}" +
                $",isLocked = {isLocked,-5}" +
                $",sequenceChangeCount = {sequenceChangeCount,-5}" +
                $",retryCount = {retryCount,-5}" +
                $",state = {state,-5}" +
                $",specifiedWorkerId = {specifiedWorkerId,-5}" +
                $",assignedWorkerId = {assignedWorkerId,-5}" +
                $",parameters = {parametersStr,-5}" +
                $",preReports = {preReportsStr,-5}" +
                $",postReports = {postReportsStr,-5}";
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

    public class MqttSubscribeDtoMission
    {
        [JsonPropertyOrder(1)] public string jobId { get; set; }
        [JsonPropertyOrder(2)] public string acsMissionId { get; set; }
        [JsonPropertyOrder(3)] public string robotId { get; set; }
        [JsonPropertyOrder(4)] public string workerId { get; set; }
        [JsonPropertyOrder(5)] public string state { get; set; }
        [JsonPropertyOrder(6)] public DateTime ts { get; set; }
        [JsonPropertyOrder(8)] public DateTime vendorTs { get; set; }
        [JsonPropertyOrder(9)] public string _id { get; set; }
        [JsonPropertyOrder(10)] public MqttSubscribeDtoMissionRowData rowData { get; set; }

        public override string ToString()
        {
            return
                $" jobId = {jobId,-5}" +
                $",acsMissionId = {acsMissionId,-5}" +
                $",robotId = {robotId,-5}" +
                $",workerId = {workerId,-5}" +
                $",state = {state,-5}" +
                $",ts = {ts,-5}" +
                $",vendorTs = {vendorTs,-5}" +
                $",_id = {_id,-5}" +
                $",rowData = {rowData,-5}";
        }
    }

    public class MqttSubscribeDtoMissionRowData
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