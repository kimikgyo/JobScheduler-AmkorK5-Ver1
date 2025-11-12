using Common.DTOs.Jobs;
using Common.Models;
using Common.Models.Jobs;
using Data.Interfaces;
using JOB.JobQueues.Interfaces;
using JOB.Mappings.Interfaces;
using JOB.MQTTs.Interfaces;
using log4net;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace JOB.Controllers.Jobs
{
    [Route("api/[controller]")]
    [ApiController]
    public class missionsController : ControllerBase
    {
        private static readonly ILog logger = LogManager.GetLogger("MissionController"); //Function 실행관련 Log

        private readonly IUnitOfWorkRepository _repository;
        private readonly IUnitOfWorkMapping _mapping;
        private readonly IUnitOfWorkJobMissionQueue _queue;
        private readonly IUnitofWorkMqttQueue _mqttQueue;

        public missionsController(IUnitOfWorkRepository repository, IUnitOfWorkMapping mapping, IUnitOfWorkJobMissionQueue queue, IUnitofWorkMqttQueue mqttQueue)
        {
            _repository = repository;
            _mapping = mapping;
            _queue = queue;
            _mqttQueue = mqttQueue;
        }

        // GET: api/<MissionController>
        [HttpGet]
        public ActionResult<List<ResponseDtoMission>> GetAll()
        {
            List<ResponseDtoMission> _responseDtos = new List<ResponseDtoMission>();

            foreach (var model in _repository.Missions.GetAll())
            {
                _responseDtos.Add(_mapping.Missions.Response(model));
                logger.Info($"{this.ControllerLogPath()} Response = {model}");
            }
            return Ok(_responseDtos);
        }

        //History
        [HttpGet("history")]
        public ActionResult<List<ResponseDtoMission>> FindHistory(DateTime startDay, DateTime endDay)
        {
            if (startDay != DateTime.MinValue && endDay != DateTime.MinValue)
            {
                List<ResponseDtoMission> _responseDtos = new List<ResponseDtoMission>();

                if (startDay == endDay) endDay = endDay.AddDays(1);
                var histories = _repository.MissionHistorys.FindHistory(startDay, endDay);
                foreach (var history in histories)
                {
                    _responseDtos.Add(_mapping.Missions.Response(history));
                    logger.Info($"{this.ControllerLogPath()} Response = {history}");
                }

                return Ok(_responseDtos);
            }
            else
            {
                return BadRequest("check startDay or endDay");
            }
        }

        [HttpGet("history/today")]
        public ActionResult<List<ResponseDtoMission>> GetTodayHistory()
        {
            List<ResponseDtoMission> _responseDtos = new List<ResponseDtoMission>();

            DateTime today = DateTime.Today;
            DateTime tomorrow = today.AddDays(1);
            var histories = _repository.MissionHistorys.FindHistory(today, tomorrow);
            foreach (var history in histories)
            {
                _responseDtos.Add(_mapping.Missions.Response(history));
                logger.Info($"{this.ControllerLogPath()} Response = {history}");
            }
            return Ok(_responseDtos);
        }

        //finisth
        [HttpGet("finish/today")]
        public ActionResult<List<ResponseDtoMission>> GetTodayFinisthHistory()
        {
            List<ResponseDtoMission> _responseDtos = new List<ResponseDtoMission>();

            DateTime today = DateTime.Today;
            DateTime tomorrow = today.AddDays(1);
            var histories = _repository.MissionFinishedHistorys.FindHistory(today, tomorrow);
            foreach (var history in histories)
            {
                _responseDtos.Add(_mapping.Missions.Response(history));
                logger.Info($"{this.ControllerLogPath()} Response = {history}");
            }
            return Ok(_responseDtos);
        }

        // GET api/<JobController>/5
        [HttpGet("{id}")]
        public ActionResult<ResponseDtoMission> GetById(string id)
        {
            ResponseDtoMission responseDto = null;

            var mission = _repository.Missions.GetById(id);

            if (mission != null)
            {
                responseDto = _mapping.Missions.Response(mission);
            }
            logger.Info($"{this.ControllerLogPath(id)} Response = {responseDto}");
            return responseDto;
        }

        // POST api/<MissionController>
        //[HttpPost]
        //public void Post([FromBody] string value)
        //{
        //}

        //// PUT api/<MissionController>/5
        //[HttpPut("{id}")]
        //public void Put(int id, [FromBody] string value)
        //{
        //}

        // PUT api/<MissionController>/5
        [HttpPatch]
        public ActionResult<ResponseDtoMission> Put([FromBody] PatchDtoMission patchDto)
        {
            ResponseDtoMission responseDto = null;
            string message = null;
            if (!IsInvalid(patchDto.orderId) && !IsInvalid(patchDto.destinationId))
            {
                var mission = _repository.Missions.GetByOrderId(patchDto.orderId).FirstOrDefault(m => m.subType == nameof(MissionSubType.DESTINATIONMOVE));
                if (mission != null)
                {
                    if ((mission.state == nameof(MissionState.INIT)) || (mission.state == nameof(MissionState.WORKERASSIGNED)) || (mission.state == nameof(MissionState.WAITING)))
                    {
                        var parameter = mission.parameters.FirstOrDefault(e => e.key == "target");
                        if (parameter != null)
                        {
                            var positions = _repository.Positions.GetAll();
                            var movePosition = positions.FirstOrDefault(p => p.id == parameter.value);
                            var updatePosition = positions.FirstOrDefault(p => p.id == patchDto.destinationId);

                            if (movePosition != null && updatePosition != null)
                            {
                                if (movePosition.name.ToUpper().Contains("BUFFER") && updatePosition.name.ToUpper().Contains("BUFFER"))
                                {
                                    mission.name = updatePosition.name;
                                    parameter.value = updatePosition.id;
                                    mission.updatedAt = DateTime.Now;
                                    mission.parametersJson = JsonSerializer.Serialize(mission.parameters);
                                    responseDto = _mapping.Missions.Response(mission);
                                    _repository.Missions.Update(mission);
                                    _repository.MissionHistorys.Add(mission);
                                    _mqttQueue.MqttPublishMessage(TopicType.mission, TopicSubType.status, _mapping.Missions.MqttPublish(mission));
                                }
                                else
                                {
                                    message = $"Only buffer name is allowed";
                                }
                            }
                            else
                            {
                                message = $"NotFind Position";
                            }
                        }
                        else
                        {
                            message = $"Not Find Target Parameter MissionId = {mission.guid}";
                        }
                    }
                    else
                    {
                        message = $"Unable to update MissionState = {mission.state}";
                    }
                }
                else
                {
                    message = "Not Find Mission";
                }
            }
            else
            {
                message = "check orderId or destinationId";
            }
            if (responseDto != null)
            {
                logger.Info($"{this.ControllerLogPath()} Response = " +
                                $"Code = {Ok(message).StatusCode}" +
                                $",massage = {Ok(message).Value}" +
                                $",Date = {responseDto}");
                return Ok(responseDto);
            }
            else
            {
                logger.Info($"{this.ControllerLogPath()} Response = " +
                            $"Code = {NotFound(message).StatusCode}" +
                            $",massage = {NotFound(message).Value}" +
                            $",Date = {message}"
                            );
                return BadRequest(message);
            }
        }

        private bool IsInvalid(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                || value.ToUpper() == "NULL"
                || value.ToUpper() == "STRING"
                || value.ToUpper() == "";
        }

        //// DELETE api/<MissionController>/5
        //[HttpDelete("{id}")]
        //public void Delete(int id)
        //{
        //}
    }
}