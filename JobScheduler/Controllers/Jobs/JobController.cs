using Common.DTOs.Rests.Jobs;
using Common.Models;
using Common.Models.Jobs;
using Data.Interfaces;
using JOB.JobQueues.Interfaces;
using JOB.Mappings.Interfaces;
using JOB.MQTTs.Interfaces;
using log4net;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

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
        public ActionResult<List<Get_JobDto>> GetAll()
        {
            try
            {
                List<Get_JobDto> _responseDtos = new List<Get_JobDto>();

                foreach (var job in _repository.Jobs.GetAll())
                {
                    var responceJob = _mapping.Jobs.Get(job);
                    foreach (var mission in _repository.Missions.GetByJobId(responceJob.guid))
                    {
                        responceJob.missions.Add(_mapping.Missions.Get(mission));
                    }
                    _responseDtos.Add(responceJob);
                    //logger.Info($"{this.ControllerLogPath()} GetAll = {responceJob}");
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
        public ActionResult<List<Get_JobDto>> FindHistory(DateTime startDay, DateTime endDay)
        {
            try
            {
                if (startDay != DateTime.MinValue && endDay != DateTime.MinValue)
                {
                    List<Get_JobDto> _responseDtos = new List<Get_JobDto>();

                    if (startDay == endDay) endDay = endDay.AddDays(1);
                    var histories = _repository.JobHistorys.FindHistory(startDay, endDay);

                    foreach (var history in histories)
                    {
                        var responceJob = _mapping.Jobs.Get(history);
                        foreach (var mission in _repository.MissionHistorys.FindHistoryJobId(history.guid))
                        {
                            responceJob.missions.Add(_mapping.Missions.Get(mission));
                        }
                        _responseDtos.Add(responceJob);
                        //logger.Info($"{this.ControllerLogPath()} FindHistory = {responceJob}");
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
        public ActionResult<List<Get_JobDto>> GetTodayHistory()
        {
            try
            {
                List<Get_JobDto> _responseDtos = new List<Get_JobDto>();

                DateTime today = DateTime.Today;
                DateTime tomorrow = today.AddDays(1);
                var histories = _repository.JobHistorys.FindHistory(today, tomorrow);

                foreach (var history in histories)
                {
                    var responceJob = _mapping.Jobs.Get(history);
                    foreach (var mission in _repository.MissionHistorys.FindHistoryJobId(history.guid))
                    {
                        responceJob.missions.Add(_mapping.Missions.Get(mission));
                    }
                    _responseDtos.Add(responceJob);
                    //logger.Info($"{this.ControllerLogPath()} GetTodayHistory = {responceJob}");
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
        public ActionResult<List<Get_JobDto>> GetTodayFinisthHistory()
        {
            try
            {
                List<Get_JobDto> _responseDtos = new List<Get_JobDto>();

                DateTime today = DateTime.Today;
                DateTime tomorrow = today.AddDays(1);
                var histories = _repository.JobFinishedHistorys.FindHistory(today, tomorrow);

                foreach (var history in histories)
                {
                    var responceJob = _mapping.Jobs.Get(history);
                    foreach (var mission in _repository.MissionFinishedHistorys.FindHistoryJobId(history.guid))
                    {
                        responceJob.missions.Add(_mapping.Missions.Get(mission));
                    }
                    _responseDtos.Add(responceJob);
                    //logger.Info($"{this.ControllerLogPath()} GetTodayFinisthHistory = {responceJob}");
                }
                return Ok(_responseDtos);
            }
            catch (Exception ex)
            {
                LogExceptionMessage(ex);
                return NotFound();
            }
        }

        // GET api/<JobController>/5
        [HttpGet("{id}")]
        public ActionResult<Get_JobDto> GetById(string id)
        {
            try
            {
                Get_JobDto responseDto = null;

                var job = _repository.Jobs.GetByid(id);
                if (job != null)
                {
                    responseDto = _mapping.Jobs.Get(job);

                    foreach (var mission in _repository.Missions.GetByJobId(job.guid))
                    {
                        responseDto.missions.Add(_mapping.Missions.Get(mission));
                    }
                }
                //logger.Info($"{this.ControllerLogPath(id)} GetById = {responseDto}");
                return Ok(responseDto);
            }
            catch (Exception ex)
            {
                LogExceptionMessage(ex);
                return NotFound();
            }
        }

        // PUT api/<JobController>/5
        [HttpPut/*("{id}")*/]
        public ActionResult Put([FromBody] Put_JobDto update)
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
                    _mqttQueue.MqttPublishMessage(TopicType.job, nameof(TopicSubType.status), _mapping.Jobs.Publish(job));
                    return Ok(job);
                }
            }
            else
            {
                return BadRequest(message);
            }
        }

        private string ConditionUpdateJob(Put_JobDto updateRequestDto)
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


        private void LogExceptionMessage(Exception ex)
        {
            string message = ex.GetFullMessage() + Environment.NewLine + ex.StackTrace;
            Debug.WriteLine(message);
            logger.Error(message);
        }
        //// POST api/<JobController>
        //[HttpPost]
        //public void Request([FromBody] string value)
        //{
        //}

        //// DELETE api/<JobController>/5
        //[HttpDelete("{id}")]
        //public void Delete(int id)
        //{
        //}
    }
}