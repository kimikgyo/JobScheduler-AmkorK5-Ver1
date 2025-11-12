using Common.DTOs.Jobs;
using Common.Models;
using Common.Models.Jobs;
using Data.Interfaces;
using JOB.JobQueues.Interfaces;
using JOB.Mappings.Interfaces;
using JOB.MQTTs.Interfaces;
using log4net;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace JOB.Controllers.Jobs
{
    [Route("api/[controller]")]
    [ApiController]
    public class jobsController : ControllerBase
    {
        private static readonly ILog logger = LogManager.GetLogger("JobController"); //Function 실행관련 Log

        private readonly IUnitOfWorkRepository _repository;
        private readonly IUnitOfWorkMapping _mapping;
        private readonly IUnitOfWorkJobMissionQueue _queue;
        private readonly IUnitofWorkMqttQueue _mqttQueue;

        public jobsController(IUnitOfWorkRepository repository, IUnitOfWorkMapping mapping, IUnitOfWorkJobMissionQueue queue, IUnitofWorkMqttQueue mqttQueue)
        {
            _repository = repository;
            _mapping = mapping;
            _queue = queue;
            _mqttQueue = mqttQueue;
        }

        // GET: api/<JobController>
        [HttpGet]
        public ActionResult<List<ResponseDtoJob>> GetAll()
        {
            List<ResponseDtoJob> _responseDtos = new List<ResponseDtoJob>();

            foreach (var job in _repository.Jobs.GetAll())
            {
                var responceJob = _mapping.Jobs.Response(job);
                foreach (var mission in _repository.Missions.GetByJobId(responceJob.guid))
                {
                    responceJob.missions.Add(_mapping.Missions.Response(mission));
                }
                _responseDtos.Add(responceJob);
                logger.Info($"{this.ControllerLogPath()} Response = {responceJob}");
            }

            return Ok(_responseDtos);
        }

        //History
        [HttpGet("history")]
        public ActionResult<List<ResponseDtoJob>> FindHistory(DateTime startDay, DateTime endDay)
        {
            if (startDay != DateTime.MinValue && endDay != DateTime.MinValue)
            {
                List<ResponseDtoJob> _responseDtos = new List<ResponseDtoJob>();

                if (startDay == endDay) endDay = endDay.AddDays(1);
                var histories = _repository.JobHistorys.FindHistory(startDay, endDay);

                foreach (var history in histories)
                {
                    var responceJob = _mapping.Jobs.Response(history);
                    foreach (var mission in _repository.MissionHistorys.FindHistoryJobId(history.guid))
                    {
                        responceJob.missions.Add(_mapping.Missions.Response(mission));
                    }
                    _responseDtos.Add(responceJob);
                    logger.Info($"{this.ControllerLogPath()} Response = {responceJob}");
                }

                return Ok(_responseDtos);
            }
            else
            {
                return BadRequest("check startDay or endDay");
            }
        }

        [HttpGet("history/today")]
        public ActionResult<List<ResponseDtoJob>> GetTodayHistory()
        {
            List<ResponseDtoJob> _responseDtos = new List<ResponseDtoJob>();

            DateTime today = DateTime.Today;
            DateTime tomorrow = today.AddDays(1);
            var histories = _repository.JobHistorys.FindHistory(today, tomorrow);

            foreach (var history in histories)
            {
                var responceJob = _mapping.Jobs.Response(history);
                foreach (var mission in _repository.MissionHistorys.FindHistoryJobId(history.guid))
                {
                    responceJob.missions.Add(_mapping.Missions.Response(mission));
                }
                _responseDtos.Add(responceJob);
                logger.Info($"{this.ControllerLogPath()} Response = {responceJob}");
            }

            return Ok(_responseDtos);
        }

        //finisth
        [HttpGet("finish/today")]
        public ActionResult<List<ResponseDtoJob>> GetTodayFinisthHistory()
        {
            List<ResponseDtoJob> _responseDtos = new List<ResponseDtoJob>();

            DateTime today = DateTime.Today;
            DateTime tomorrow = today.AddDays(1);
            var histories = _repository.JobFinishedHistorys.FindHistory(today, tomorrow);

            foreach (var history in histories)
            {
                var responceJob = _mapping.Jobs.Response(history);
                foreach (var mission in _repository.MissionFinishedHistorys.FindHistoryJobId(history.guid))
                {
                    responceJob.missions.Add(_mapping.Missions.Response(mission));
                }
                _responseDtos.Add(responceJob);
                logger.Info($"{this.ControllerLogPath()} Response = {responceJob}");
            }
            return Ok(_responseDtos);
        }

        // GET api/<JobController>/5
        [HttpGet("{id}")]
        public ActionResult<ResponseDtoJob> GetById(string id)
        {
            ResponseDtoJob responseDto = null;

            var job = _repository.Jobs.GetByid(id);
            if (job != null)
            {
                responseDto = _mapping.Jobs.Response(job);

                foreach (var mission in _repository.Missions.GetByJobId(job.guid))
                {
                    responseDto.missions.Add(_mapping.Missions.Response(mission));
                }
            }
            logger.Info($"{this.ControllerLogPath(id)} Response = {responseDto}");
            return Ok(responseDto);
        }

        // PUT api/<JobController>/5
        [HttpPut/*("{id}")*/]
        public ActionResult Put([FromBody] UpdateRequestDtoJob update)
        {
            logger.Info($"PutRequest = {update}");

            string message = ConditionUpdateJob(update);
            if (message == null)
            {
                var job = _repository.Jobs.GetByid(update.id);
                if (job == null)
                {
                    //Order Id 가 없는경우
                    return BadRequest($"Check Job Id");
                }
                else
                {
                    job.terminationType = update.terminationType;
                    job.terminateState = nameof(TerminateState.INITED);
                    job.terminator = update.terminator;
                    job.terminatingAt = update.terminatingAt;
                    _repository.Jobs.Update(job);
                    _repository.JobHistorys.Add(job);
                    _mqttQueue.MqttPublishMessage(TopicType.job, TopicSubType.status, _mapping.Jobs.MqttPublish(job));
                    return Ok(job);
                }
            }
            else
            {
                return BadRequest(message);
            }
        }

        private string ConditionUpdateJob(UpdateRequestDtoJob updateRequestDto)
        {
            string massage = null;
            if (IsInvalid(updateRequestDto.id)) return massage = $"Check Job Id";

            //orderType 빈문자를제외후 대문자로 변환
            updateRequestDto.terminationType = updateRequestDto.terminationType.Replace(" ", "").ToUpper();
            // Enum에 값이 존재하는지 확인
            bool existTypes = Enum.IsDefined(typeof(TerminateType), updateRequestDto.terminationType);
            if (!existTypes) return massage = $"Check TerminateType";

            return massage;
        }

        private bool IsInvalid(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                || value.ToUpper() == "NULL"
                || value.ToUpper() == "STRING"
                || value.ToUpper() == "";
        }

        //// POST api/<JobController>
        //[HttpPost]
        //public void Post([FromBody] string value)
        //{
        //}

        //// DELETE api/<JobController>/5
        //[HttpDelete("{id}")]
        //public void Delete(int id)
        //{
        //}
    }
}