using JOB.Mappings.Bases;
using JOB.Mappings.Jobs;
using JOB.Mappings.Templates;

namespace JOB.Mappings.Interfaces
{
    public interface IUnitOfWorkMapping : IDisposable
    {
        Order_Mapping Orders { get; }
        Job_Mapping Jobs { get; }
        Mission_Mapping Missions { get; }
        Worker_Mapping Workers { get; }
        Position_Mapping Positions { get; }
        Map_Mapping Maps { get; }
        Middleware_Mapping Middlewares { get; }
        Carrier_Mapping Carriers { get; }
        Elevator_Mapping Elevators { get; }
        RoutesPlan_Mapping RoutesPlanas { get; }
        MissionTemplate_Mapping MissionTemplates { get; }
    }
}