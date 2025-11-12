using JOB.Mappings.Bases;
using JOB.Mappings.Jobs;

namespace JOB.Mappings.Interfaces
{
    public interface IUnitOfWorkMapping : IDisposable
    {
        OrderMapping Orders { get; }
        JobMapping Jobs { get; }
        MissionMapping Missions { get; }
        WorkerMapping Workers { get; }
        PositionMapping Positions { get; }
        MapMapping Maps { get; }
        JobTemplateMapping JobTemplates { get; }
        MiddlewareMapping Middlewares { get; }
        CarrierMapping Carriers { get; }
    }
}