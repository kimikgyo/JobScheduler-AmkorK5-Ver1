using Common.DTOs.Rests.Missions;
using Common.Models;
using Common.Models.Jobs;
using Data.Interfaces;
using JOB.JobQueues.Interfaces;
using JOB.Mappings.Interfaces;
using JOB.MQTTs.Interfaces;
using log4net;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
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
        public ActionResult<List<Get_MissionDto>> GetAll()
        {
            try
            {
                List<Get_MissionDto> _responseDtos = new List<Get_MissionDto>();

                foreach (var model in _repository.Missions.GetAll())
                {
                    _responseDtos.Add(_mapping.Missions.Get(model));
                    //logger.Info($"{this.ControllerLogPath()} Get = {model}");
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
        public ActionResult<List<Get_MissionDto>> FindHistory(DateTime startDay, DateTime endDay)
        {
            try
            {
                if (startDay != DateTime.MinValue && endDay != DateTime.MinValue)
                {
                    List<Get_MissionDto> _responseDtos = new List<Get_MissionDto>();

                    if (startDay == endDay) endDay = endDay.AddDays(1);
                    var histories = _repository.MissionHistorys.FindHistory(startDay, endDay);
                    foreach (var history in histories)
                    {
                        _responseDtos.Add(_mapping.Missions.Get(history));
                        //logger.Info($"{this.ControllerLogPath()} Get = {history}");
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
        public ActionResult<List<Get_MissionDto>> GetTodayHistory()
        {
            try
            {
                List<Get_MissionDto> _responseDtos = new List<Get_MissionDto>();

                DateTime today = DateTime.Today;
                DateTime tomorrow = today.AddDays(1);
                var histories = _repository.MissionHistorys.FindHistory(today, tomorrow);
                foreach (var history in histories)
                {
                    _responseDtos.Add(_mapping.Missions.Get(history));
                    //logger.Info($"{this.ControllerLogPath()} Get = {history}");
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
        public ActionResult<List<Get_MissionDto>> GetTodayFinisthHistory()
        {
            try
            {
                List<Get_MissionDto> _responseDtos = new List<Get_MissionDto>();

                DateTime today = DateTime.Today;
                DateTime tomorrow = today.AddDays(1);
                var histories = _repository.MissionFinishedHistorys.FindHistory(today, tomorrow);
                foreach (var history in histories)
                {
                    _responseDtos.Add(_mapping.Missions.Get(history));
                    //logger.Info($"{this.ControllerLogPath()} Get = {history}");
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
        public ActionResult<Get_MissionDto> GetById(string id)
        {
            try
            {
                Get_MissionDto responseDto = null;

                var mission = _repository.Missions.GetById(id);

                if (mission != null)
                {
                    responseDto = _mapping.Missions.Get(mission);
                }
                //logger.Info($"{this.ControllerLogPath(id)} Get = {responseDto}");
                return responseDto;
            }
            catch (Exception ex)
            {
                LogExceptionMessage(ex);
                return NotFound();
            }
        }

        // POST api/<MissionController>
        //[HttpPost]
        //public void Request([FromBody] string value)
        //{
        //}

        // PUT api/<MissionController>/5
        [HttpPut("{acsMissionid}")]
        public ActionResult<Mission> Put(string acsMissionid, [FromBody] string value)
        {
            var mission = _repository.Missions.GetById(acsMissionid);
            if (mission != null)
            {
                string missionstate = value.Replace(" ", "").ToUpper();
                updateStateMission(mission, missionstate, "[missionsControllerPut]", true);
                return Ok(mission);
            }
            else
            {
                return NotFound();
            }
        }

        // PUT api/<MissionController>/5
        [HttpPatch]
        public ActionResult<Get_MissionDto> Patch([FromBody] Patch_MissionDto patchDto)
        {
            Get_MissionDto responseDto = null;
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
                            var positions = _repository.Positions.MiR_GetAll();
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
                                    responseDto = _mapping.Missions.Get(mission);
                                    _repository.Missions.Update(mission, "[missionsControllerPatch]");
                                    _repository.MissionHistorys.Add(mission);
                                    _mqttQueue.MqttPublishMessage(TopicType.mission, mission.assignedWorkerId, _mapping.Missions.Publish(mission));
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
                logger.Info($"{this.ControllerLogPath()} Get = " +
                                $"Code = {Ok(message).StatusCode}" +
                                $",massage = {Ok(message).Value}" +
                                $",Date = {responseDto}");
                return Ok(responseDto);
            }
            else
            {
                logger.Info($"{this.ControllerLogPath()} Get = " +
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

        private void LogExceptionMessage(Exception ex)
        {
            string message = ex.GetFullMessage() + Environment.NewLine + ex.StackTrace;
            Debug.WriteLine(message);
            logger.Error(message);
        }

        private void updateStateMission(Mission mission, string state, string message, bool historyAdd = false)
        {
            bool worekerMissionIdUpdateFlag = true;
            if (mission.state != state)
            {
                mission.state = state;
                switch (mission.state)
                {
                    case nameof(MissionState.INIT):
                    case nameof(MissionState.WORKERASSIGNED):
                    case nameof(MissionState.WAITING):
                    case nameof(MissionState.COMMANDREQUEST):
                    case nameof(MissionState.COMMANDREQUESTCOMPLETED):
                    case nameof(MissionState.PENDING):
                    case nameof(MissionState.EXECUTING):
                    case nameof(MissionState.FAILED):
                    case nameof(MissionState.ABORTINITED):
                    case nameof(MissionState.ABORTFAILED):
                    case nameof(MissionState.CANCELINITED):
                    case nameof(MissionState.CNACELFAILED):
                        mission.updatedAt = DateTime.Now;
                        break;

                    case nameof(MissionState.SKIPPED):
                    case nameof(MissionState.ABORTCOMPLETED):
                    case nameof(MissionState.CANCELINITCOMPLETED):
                    case nameof(MissionState.CANCELED):
                    case nameof(MissionState.COMPLETED):
                        mission.finishedAt = DateTime.Now;
                        worekerMissionIdUpdateFlag = false;
                        break;
                }

                _repository.Missions.Update(mission, message);
                if (historyAdd) _repository.MissionHistorys.Add(mission);
                _mqttQueue.MqttPublishMessage(TopicType.mission, mission.assignedWorkerId, _mapping.Missions.Publish(mission));
            }
        }

        //// DELETE api/<MissionController>/5
        //[HttpDelete("{id}")]
        //public void Delete(int id)
        //{
        //}
    }
}