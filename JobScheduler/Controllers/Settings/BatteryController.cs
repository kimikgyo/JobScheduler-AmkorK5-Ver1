using Common.DTOs.Rests.Batterys;
using Common.Models.Settings;
using Data.Interfaces;
using JOB.JobQueues.Interfaces;
using JOB.Mappings.Interfaces;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace JOB.Controllers.Settings
{
    [Route("api/[controller]")]
    [ApiController]
    public class batteryController : ControllerBase
    {
        private readonly IUnitOfWorkRepository _repository;
        private readonly IUnitOfWorkMapping _mapping;
        private readonly IUnitOfWorkJobMissionQueue _queue;

        public batteryController(IUnitOfWorkRepository repository, IUnitOfWorkMapping mapping, IUnitOfWorkJobMissionQueue queue)
        {
            _repository = repository;
            _mapping = mapping;
            _queue = queue;
        }

        // GET: api/<BatteryController>
        [HttpGet]
        public ActionResult<Battery> GetAll()
        {
            return _repository.Battery.GetAll();
        }

        //// GET api/<BatteryController>/5
        //[HttpGet("{id}")]
        //public string Get(int id)
        //{
        //    return "value";
        //}

        // POST api/<BatteryController>
        //[HttpPost]
        //public ActionResult Post([FromBody] Post_BatteryDto post)
        //{
        //    var batteryGetAll = _repository.Battery.GetAll();
        //    if(batteryGetAll == null)
        //    {
        //        var battery = new Battery
        //        {
        //            minimum = post.minimum,
        //            crossCharge = post.crossCharge,
        //            chargeStart = post.chargeStart,
        //            chargeEnd = post.chargeEnd,
        //            createAt = post.createAt,
        //        };
        //        _repository.Battery.Add(battery);
        //        return Ok(battery);
        //    }
        //    else
        //    {
        //        return NotFound(batteryGetAll);

        //    }
        //}

        // PUT api/<BatteryController>/5
        [HttpPatch]
        public ActionResult Patch([FromBody] Put_BatteryDto apiPutRequstDto)
        {
            var battery = _repository.Battery.GetAll();
            if (battery != null)
            {
                if (apiPutRequstDto.minimum != null) battery.minimum = (double)apiPutRequstDto.minimum;
                if (apiPutRequstDto.crossCharge != null) battery.crossCharge = (double)apiPutRequstDto.crossCharge;
                if (apiPutRequstDto.chargeStart != null) battery.chargeStart = (double)apiPutRequstDto.chargeStart;
                if (apiPutRequstDto.chargeEnd != null) battery.chargeEnd = (double)apiPutRequstDto.chargeEnd;

                battery.updatedAt = DateTime.Now;
                _repository.Battery.Update(battery);
                return Ok(battery);
            }
            else
            {
                return NotFound();
            }
        }

        // DELETE api/<BatteryController>/5
        //[HttpDelete("{id}")]
        //public void Delete(int id)
        //{
        //}
    }
}