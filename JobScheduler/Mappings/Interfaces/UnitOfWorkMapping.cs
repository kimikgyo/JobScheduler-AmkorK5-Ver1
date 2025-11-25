using JOB.Mappings;
using JOB.Mappings.Bases;
using JOB.Mappings.Jobs;

namespace JOB.Mappings.Interfaces
{
    public class UnitOfWorkMapping : IUnitOfWorkMapping
    {
        public OrderMapping Orders { get; private set; }
        public JobMapping Jobs { get; private set; }
        public MissionMapping Missions { get; private set; }
        public WorkerMapping Workers { get; private set; }
        public PositionMapping Positions { get; private set; }
        public MapMapping Maps { get; private set; }
        public JobTemplateMapping JobTemplates { get; private set; }
        public MiddlewareMapping Middlewares { get; private set; }
        public CarrierMapping Carriers { get; private set; }
        public ElevatorMapping Elevators{ get; private set; }

        public UnitOfWorkMapping()
        {
            mapping();
        }

        private void mapping()
        {
            Orders = new OrderMapping();
            Jobs = new JobMapping();
            Missions = new MissionMapping();
            Workers = new WorkerMapping();
            Positions = new PositionMapping();
            Maps = new MapMapping();
            JobTemplates = new JobTemplateMapping();
            Middlewares = new MiddlewareMapping();
            Carriers = new CarrierMapping();
            Elevators = new ElevatorMapping();
        }

        public void SaveChanges()
        {
        }

        public void Dispose()
        {
        }
    }
}