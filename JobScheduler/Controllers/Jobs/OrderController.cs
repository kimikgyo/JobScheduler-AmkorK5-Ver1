using Common.DTOs.Rests.Orders;
using Common.Models.Jobs;
using Data.Interfaces;
using JOB.JobQueues.Interfaces;
using JOB.Mappings.Interfaces;
using JOB.MQTTs.Interfaces;
using log4net;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Data;
using System.Diagnostics;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace JobScheduler.Controllers.Jobs
{
    [Route("api/[controller]")]
    [ApiController]
    public class ordersController : ControllerBase
    {
        //Ok()` / `Ok(object)`                 `200 OK`            요청 성공, 결과 데이터 포함
        //Created(uri, object)`                `201 Created`       리소스 생성 완료(URI 포함 가능)
        //NoContent()`                         `204 No Content`    성공했지만 반환할 내용 없음
        //BadRequest()` / `BadRequest(object)` `400 Bad Request`   잘못된 요청
        //Unauthorized()`                      `401 Unauthorized`  인증 필요(토큰 등 없음)
        //Forbid()`                            `403 Forbidden`     권한 부족으로 접근 금지
        //NotFound()`                          `404 Not Found`     리소스 없음
        //Conflict()`                          `409 Conflict`      중복 등 충돌 발생
        //StatusCode(int)`                     임의 상태               임의 HTTP 상태 코드 반환
        // 500	Internal Server Error 서버 내부 오류.명확한 원인 없이 실패한 경우
        //501	Not Implemented 요청된 기능이 서버에 구현되어 있지 않음
        //502	Bad Gateway 게이트웨이 또는 프록시 서버가 잘못된 응답 수신
        //503	Service Unavailable 서버가 일시적으로 사용 불가능 (예: 과부하, 점검 중)
        //504	Gateway Timeout 게이트웨이 또는 프록시 서버가 시간 내 응답 받지 못함
        //505	HTTP Version Not Supported  요청에서 사용한 HTTP 버전이 지원되지 않음

        private static readonly ILog logger = LogManager.GetLogger("OrderController"); //Function 실행관련 Log

        private readonly IUnitOfWorkRepository _repository;
        private readonly IUnitOfWorkMapping _mapping;
        private readonly IUnitOfWorkJobMissionQueue _queue;
        private readonly IUnitofWorkMqttQueue _mqttQueue;

        public ordersController(IUnitOfWorkRepository repository, IUnitOfWorkMapping mapping, IUnitOfWorkJobMissionQueue queue, IUnitofWorkMqttQueue mqttQueue)
        {
            _repository = repository;
            _mapping = mapping;
            _queue = queue;
            _mqttQueue = mqttQueue;
        }

        //GET: api/<OrderController>
        [HttpGet]
        public ActionResult<List<Get_OrderDto>> GetAll()
        {
            try
            {
                List<Get_OrderDto> _responseDtos = new List<Get_OrderDto>();
                foreach (var model in _repository.Orders.GetAll())
                {
                    Get_OrderDto responseDto = null;
                    responseDto = _mapping.Orders.Get(model);
                    var job = _repository.Jobs.GetByOrderId(model.id);
                    if (job != null)
                    {
                        var jobresponse = _mapping.Jobs.Get(job);

                        foreach (var mission in _repository.Missions.GetByJobId(job.guid))
                        {
                            var missionresponse = _mapping.Missions.Get(mission);
                            jobresponse.missions.Add(missionresponse);
                        }

                        responseDto.Job = jobresponse;
                    }

                    _responseDtos.Add(responseDto);
                    //logger.Info($"{this.ControllerLogPath()} Get = {responseDto}");
                }

                return Ok(_responseDtos);
            }
            catch (Exception ex)
            {
                LogExceptionMessage(ex);
                return NotFound();
            }
        }

        //History
        [HttpGet("history")]
        public ActionResult<List<Get_OrderDto>> FindHistory(DateTime startDay, DateTime endDay)
        {
            try
            {
                if (startDay != DateTime.MinValue && endDay != DateTime.MinValue)
                {
                    List<Get_OrderDto> _responseDtos = new List<Get_OrderDto>();

                    if (startDay == endDay) endDay = endDay.AddDays(1);
                    var histories = _repository.OrderHistorys.FindHistory(startDay, endDay);

                    foreach (var history in histories)
                    {
                        var responseDto = _mapping.Orders.Get(history);
                        foreach (var job in _repository.JobHistorys.FindHistoryOrderId(history.id))
                        {
                            var jobresponse = _mapping.Jobs.Get(job);

                            foreach (var mission in _repository.MissionHistorys.FindHistoryOrderId(history.id))
                            {
                                var missionresponse = _mapping.Missions.Get(mission);
                                jobresponse.missions.Add(missionresponse);
                            }
                            responseDto.Job = jobresponse;
                        }
                        _responseDtos.Add(responseDto);
                    }

                    return Ok(_responseDtos);
                }
                else
                {
                    return BadRequest("check startDay or endDay");
                }
            }
            catch (Exception ex)
            {
                LogExceptionMessage(ex);
                return NotFound();
            }
        }

        [HttpGet("history/today")]
        public ActionResult<List<Get_OrderDto>> GetTodayHistory()
        {
            try
            {
                List<Get_OrderDto> _responseDtos = new List<Get_OrderDto>();

                DateTime today = DateTime.Today;
                DateTime tomorrow = today.AddDays(1);
                var histories = _repository.OrderHistorys.FindHistory(today, tomorrow);

                foreach (var history in histories)
                {
                    var responseDto = _mapping.Orders.Get(history);
                    foreach (var job in _repository.JobHistorys.FindHistoryOrderId(history.id))
                    {
                        var jobresponse = _mapping.Jobs.Get(job);

                        foreach (var mission in _repository.MissionHistorys.FindHistoryOrderId(history.id))
                        {
                            var missionresponse = _mapping.Missions.Get(mission);
                            jobresponse.missions.Add(missionresponse);
                        }
                        responseDto.Job = jobresponse;
                    }
                    _responseDtos.Add(responseDto);
                }

                return Ok(_responseDtos);
            }
            catch (Exception ex)
            {
                LogExceptionMessage(ex);
                return NotFound();
            }
        }

        //finisth
        [HttpGet("finish/today")]
        public ActionResult<List<Get_OrderDto>> GetTodayFinisthHistory()
        {
            try
            {
                List<Get_OrderDto> _responseDtos = new List<Get_OrderDto>();

                DateTime today = DateTime.Today;
                DateTime tomorrow = today.AddDays(1);
                var histories = _repository.OrderFinishedHistorys.FindHistory(today, tomorrow);

                foreach (var history in histories)
                {
                    var mappingOrderHistory = _mapping.Orders.Get(history);
                    foreach (var job in _repository.JobFinishedHistorys.FindHistoryOrderId(history.id))
                    {
                        var mappingJobHistory = _mapping.Jobs.Get(job);

                        foreach (var mission in _repository.MissionFinishedHistorys.FindHistoryOrderId(history.id))
                        {
                            mappingJobHistory.missions.Add(_mapping.Missions.Get(mission));
                        }
                        mappingOrderHistory.Job = mappingJobHistory;
                    }
                    _responseDtos.Add(mappingOrderHistory);
                }
                return Ok(_responseDtos);
            }
            catch (Exception ex)
            {
                LogExceptionMessage(ex);
                return NotFound();
            }
        }
        [HttpGet("worker/{id}")]
        public ActionResult<List<Get_OrderDto>> GetByWorkerId(string id)
        {
            try
            {
                List<Get_OrderDto> _responseDtos = new List<Get_OrderDto>();

                var orders = _repository.Orders.GetBySpecifiedWorkerIdOrAssignWorkerId(id);

                foreach (var order in orders)
                {
                    var mappingOrder = _mapping.Orders.Get(order);
                    _responseDtos.Add(mappingOrder);
                }

                return Ok(_responseDtos);
            }
            catch (Exception ex)
            {
                LogExceptionMessage(ex);
                return NotFound();
            }
        }
        //GET api/<OrderController>/5
        [HttpGet("{id}")]
        public ActionResult<Get_OrderDto> GetById(string id)
        {
            try
            {
                Get_OrderDto responseDto = null;
                var order = _repository.Orders.GetByid(id);
                if (order != null)
                {
                    responseDto = _mapping.Orders.Get(order);
                    var job = _repository.Jobs.GetByOrderId(order.id);
                    if (job != null)
                    {
                        var jobresponse = _mapping.Jobs.Get(job);

                        foreach (var mission in _repository.Missions.GetByJobId(job.guid))
                        {
                            var missionresponse = _mapping.Missions.Get(mission);
                            jobresponse.missions.Add(missionresponse);
                        }

                        responseDto.Job = jobresponse;
                    }
                }
                //logger.Info($"{this.ControllerLogPath(id)} Get = {responseDto}");
                return Ok(responseDto);
            }
            catch (Exception ex)
            {
                LogExceptionMessage(ex);
                return NotFound();
            }
        }

        // POST api/<OrderController>
        [HttpPost]
        public ActionResult Post([FromBody] Post_OrderDto add)
        {
            logger.Info($"AddRequest = {add}");
            string message = ConditionAddOrder(add);
            if (message == null)
            {
                _queue.Create_Order(add);
                logger.Info($"{this.ControllerLogPath()} Get = " +
                                $"Code = {Ok(message).StatusCode}" +
                                $",message = {Ok(message).Value}" +
                                $",Date = {add}"
                                );
                return Created();
            }
            else
            {
                logger.Warn($"{this.ControllerLogPath()} Get = " +
                                 $"Code = {NotFound(message).StatusCode}" +
                                 $",message = {NotFound(message).Value}" +
                                 $",Date = {add}"
                                 );
                return BadRequest(message);
            }
        }

        // PUT api/<OrderController>/5
        //[HttpPut("{id}")]
        //public ActionResult Put([FromBody] UpdateRequestDtoOrder update)
        //{
        //}

        //// DELETE api/<OrderController>/5
        //[HttpDelete("{id}")]
        //public void Delete(int id)
        //{
        //}

        private string ConditionAddOrder(Post_OrderDto RequestDto)
        {
            string message = null;
            //[조건1] order Id 가 null이거나 빈문자 이고 type이 트랜스포트일경우
            if (IsInvalid(RequestDto.id)) return message = $"Order id is null or empty";
            //orderId 조회
            var order = _repository.Orders.GetByid(RequestDto.id);
            if (order != null) return message = $"Order id already exists.";

            //[조건2]도착자Id가 null이거나 빈문자일경우
            if (IsInvalid(RequestDto.destinationId)) return message = $"Order destinationId is null or empty";
            else
            {
                var destination = _repository.Positions.MiR_GetById_Name_linkedFacility(RequestDto.destinationId);
                if (destination == null) return message = $"Invalid destination. No matching position found.";

                //var orderFindDestination = _repository.Orders.GetByDest(RequestDto.destinationId);
                //    if(orderFindDestination != null) return message = $"orderDestinationSame";
            }

            ////[조건3]타입이 null이거나 빈문자일경우
            //if (IsInvalid(RequestDto.type)) return message = $"Check Order Type";
            ////orderType 빈문자를제외후 대문자로 변환
            //RequestDto.type = RequestDto.type.Replace(" ", "").ToUpper();
            // Enum에 값이 존재하는지 확인
            //bool existTypes = Enum.IsDefined(typeof(OrderType), RequestDto.type);
            //if (!existTypes) return message = $"Check Order Type";

            //[조건4]서브타입이 null이거나 빈문자일경우
            if (IsInvalid(RequestDto.subType)) return message = $"SubType is null or empty";
            //orderSubType 빈문자를제외후 대문자로 변환
            RequestDto.subType = RequestDto.subType.Replace(" ", "").ToUpper();
            // Enum에 값이 존재하는지 확인
            bool existSubTypes = Enum.IsDefined(typeof(JobSubType), RequestDto.subType);
            if (!existSubTypes) return message = $"Check Order SubType";

            switch (RequestDto.subType)
            {
                case nameof(JobSubType.SIMPLEMOVE):
                    //워커를 지정하여 보내지 않는경우
                    if (IsInvalid(RequestDto.specifiedWorkerId)) message = $"SpecifiedWorkerId is null or empty";
                    else
                    {
                        //워커를 지정 하였지만 worker가 List에 없는경우
                        var worker = _repository.Workers.MiR_GetById(RequestDto.specifiedWorkerId);
                        if (worker == null) message = $"Invalid SpecifiedWorkerId. No matching Worker found.";
                    }
                    break;

                case nameof(JobSubType.PICKDROP):
                case nameof(JobSubType.PICKONLY):
                case nameof(JobSubType.DROPONLY):
                    //같은 출발지와 목적지가 있는경우
                    var findSource_dest = _repository.Orders.GetBySource_Dest(RequestDto.sourceId, RequestDto.destinationId);
                    if (findSource_dest != null) message = $"There is a common source and destination";
                    //carrier Id 가 없는경우 [자재 이송이기때문에 carrier이 존재해야함]
                    //else if (IsInvalid(RequestDto.carrierId)) message = $"CarrierId is null or empty";
                    else if (RequestDto.subType == nameof(JobSubType.PICKONLY) || RequestDto.subType == nameof(JobSubType.DROPONLY))
                    {
                        //워커를 지정 하였지만 worker가 List에 없는경우
                        var worker = _repository.Workers.MiR_GetById(RequestDto.specifiedWorkerId);
                        if (worker == null) message = $"Check Order SpecifiedWorkerId ";
                    }
                    else if (RequestDto.subType == nameof(JobSubType.PICKDROP))
                    {
                        //출발지가 없는경우 PickDrop이기때문에 출발지와 목적지가 있어야함.
                        if (IsInvalid(RequestDto.sourceId)) message = $"Check Order sourceId ";
                        else
                        {
                            //출발지가 Position 목록에 없는경우
                            var source = _repository.Positions.MiR_GetById_Name_linkedFacility(RequestDto.sourceId);
                            if (source == null) message = $"Check Order sourceId ";
                        }
                    }
                    break;

                case nameof(JobSubType.CHARGE):
                case nameof(JobSubType.WAIT):

                    // Orders에서 서브타입이 일치하는 항목이 있는지 확인
                    if (RequestDto.type == nameof(JobType.CHARGE) && RequestDto.subType != nameof(JobSubType.CHARGE)) message = $"Check Order SubType";
                    else if (RequestDto.type == nameof(JobType.WAIT) && RequestDto.subType != nameof(JobSubType.WAIT)) message = $"Check Order SubType";
                    //워커를 지정하여 보내지 않는경우
                    else if (IsInvalid(RequestDto.specifiedWorkerId)) message = $"Check Order SpecifiedWorkerId ";
                    else
                    {
                        //워커를 지정 하였지만 worker가 List에 없는경우
                        var worker = _repository.Workers.MiR_GetById(RequestDto.specifiedWorkerId);
                        if (worker == null) message = $"Check Order SpecifiedWorkerId ";

                        var matchingOrder = _repository.Orders.GetAll().FirstOrDefault(o => o.subType == nameof(JobSubType.WAIT) && o.specifiedWorkerId == RequestDto.specifiedWorkerId);
                        // Jobs에서 서브타입이 일치하는 항목이 있는지 확인
                        var matchingJob = _repository.Jobs.GetAll().FirstOrDefault(j => j.subType == nameof(JobSubType.WAIT) && j.specifiedWorkerId == RequestDto.specifiedWorkerId);
                        if (matchingOrder != null || matchingJob != null)
                        {
                            message = "There is a Job or Order that matches the subtype.";
                        }
                    }

                break;

                case nameof(JobSubType.RESET):
                    if (RequestDto.type == nameof(JobType.RESET) && RequestDto.subType != nameof(JobType.RESET)) message = $"Check Order SubType";
                    break;
            }

            return message;
        }

        private bool IsInvalid(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                || value.ToUpper() == "NULL"
                || value.ToUpper() == "STRING"
                || value.ToUpper() == "";
        }

        private void LogExceptionMessage(Exception ex)
        {
            string message = ex.GetFullMessage() + Environment.NewLine + ex.StackTrace;
            Debug.WriteLine(message);
            logger.Error(message);
        }
    }
}