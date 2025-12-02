using Common.DTOs.Rests.Templates;
using Common.Templates;
using Data.Interfaces;
using JOB.JobQueues.Interfaces;
using JOB.Mappings.Interfaces;
using JOB.MQTTs.Interfaces;
using log4net;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace JOB.Controllers.Templetes
{

    [Route("api/[controller]")]
    [ApiController]
    public class missiontemplete_singleController : ControllerBase
    {
        private static readonly ILog logger = LogManager.GetLogger("MissionTemplete_SingleController"); //Function 실행관련 Log

        private readonly IUnitOfWorkRepository _repository;
        private readonly IUnitOfWorkMapping _mapping;
        private readonly IUnitOfWorkJobMissionQueue _queue;
        private readonly IUnitofWorkMqttQueue _mqttQueue;

        public missiontemplete_singleController(IUnitOfWorkRepository repository, IUnitOfWorkMapping mapping, IUnitOfWorkJobMissionQueue queue, IUnitofWorkMqttQueue mqttQueue)
        {
            _repository = repository;
            _mapping = mapping;
            _queue = queue;
            _mqttQueue = mqttQueue;
        }

        // GET: api/<MissionTemplete_GroupControllerController>
        [HttpGet]
        public ActionResult<List<MissionTemplate_Single>> Get()
        {
            return _repository.MissionTemplates_Single.GetAll();
        }

        // GET api/<MissionTemplete_GroupControllerController>/5
        [HttpGet("{id}")]
        public ActionResult<MissionTemplate_Single> Get(string id)
        {
            return _repository.MissionTemplates_Single.GetById(id);
        }

        // POST api/<MissionTemplete_GroupControllerController>
        [HttpPost]
        public void Post([FromBody] Post_MissionTemplate_Single value)
        {
            var create = new MissionTemplate_Single
            {
                guid = Guid.NewGuid().ToString(),
                name = value.name,
                service = value.service.ToUpper(),
                type = value.type.ToUpper(),
                subType = value.subType.ToUpper(),
                isLook = value.isLook,
                parameters = value.parameters,
                preReports = value.preReports,
                postReports = value.postReports,
                parametersJson = JsonSerializer.Serialize(value.parameters),
                preReportsJson = JsonSerializer.Serialize(value.preReports),
                postReportsJson = JsonSerializer.Serialize(value.postReports),
                createdAt = DateTime.Now,
            };
            _repository.MissionTemplates_Single.Add(create);
        }

        // PUT api/<MissionTemplete_GroupControllerController>/5
        //[HttpPut("{id}")]
        //public void Put(int id, [FromBody] string value)
        //{
        //}

        // DELETE api/<MissionTemplete_GroupControllerController>/5
        [HttpDelete("{id}")]
        public void Delete(string id)
        {
            var missionTemplate = _repository.MissionTemplates_Single.GetById(id);
            if (missionTemplate != null)
            {
                _repository.MissionTemplates_Single.Remove(missionTemplate);
            }
        }
    }
}
