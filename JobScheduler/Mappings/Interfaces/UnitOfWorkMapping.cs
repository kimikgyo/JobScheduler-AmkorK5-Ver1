using Common.Templates;
using JOB.Mappings;
using JOB.Mappings.Areas;
using JOB.Mappings.Bases;
using JOB.Mappings.Jobs;
using JOB.Mappings.Templates;

namespace JOB.Mappings.Interfaces
{
    public class UnitOfWorkMapping : IUnitOfWorkMapping
    {
        public Order_Mapping Orders { get; private set; }
        public Job_Mapping Jobs { get; private set; }
        public Mission_Mapping Missions { get; private set; }
        public Worker_Mapping Workers { get; private set; }
        public Position_Mapping Positions { get; private set; }
        public Map_Mapping Maps { get; private set; }
        public Middleware_Mapping Middlewares { get; private set; }
        public Carrier_Mapping Carriers { get; private set; }
        public Elevator_Mapping Elevators{ get; private set; }
        public RoutesPlan_Mapping RoutesPlanas { get; private set; }
        public MissionTemplate_Mapping MissionTemplates { get; private set; }
        public ACSAreaMapping ACSAreas { get; private set; }

        public UnitOfWorkMapping()
        {
            mapping();
        }

        private void mapping()
        {
            Orders = new Order_Mapping();
            Jobs = new Job_Mapping();
            Missions = new Mission_Mapping();
            Workers = new Worker_Mapping();
            Positions = new Position_Mapping();
            Maps = new Map_Mapping();
            Middlewares = new Middleware_Mapping();
            Carriers = new Carrier_Mapping();
            Elevators = new Elevator_Mapping();
            RoutesPlanas = new RoutesPlan_Mapping();
            MissionTemplates = new MissionTemplate_Mapping();
            ACSAreas = new ACSAreaMapping();
        }

        public void SaveChanges()
        {
        }

        public void Dispose()
        {
        }
    }
}