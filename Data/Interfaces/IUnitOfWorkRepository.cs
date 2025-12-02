using Data.Repositorys.Carriers;
using Data.Repositorys.Elevators;
using Data.Repositorys.Historys;
using Data.Repositorys.Jobs;
using Data.Repositorys.Maps;
using Data.Repositorys.Middlewares;
using Data.Repositorys.Positions;
using Data.Repositorys.Services;
using Data.Repositorys.Settings;
using Data.Repositorys.Templates;
using Data.Repositorys.Workers;

namespace Data.Interfaces
{
    public interface IUnitOfWorkRepository : IDisposable
    {
        #region Base

        CarrierRepository Carriers { get; }
        MapRepository Maps { get; }
        PositionRepository Positions { get; }
        WorkerRepository Workers { get; }
        MiddlewareRepository Middlewares { get; }
        ElevatorRepository Elevator { get; }

        #endregion Base

        MissionRepository Missions { get; }
        JobRepository Jobs { get; }
        OrderRepository Orders { get; }

        MissionHistoryRepository MissionHistorys { get; }
        JobHistoryRepository JobHistorys { get; }
        OrderHistoryRepository OrderHistorys { get; }

        MissionFinishedHistoryRepository MissionFinishedHistorys { get; }
        JobFinishedHistoryRepository JobFinishedHistorys { get; }
        OrderFinishedHistoryRepository OrderFinishedHistorys { get; }

        JobTemplateRepository JobTemplates { get; }
        MissionTemplate_Group_Repository MissionTemplates_Group { get; }
        MissionTemplate_Single_Repository MissionTemplates_Single { get; }

        ServiceApiRepository ServiceApis { get; }

        #region Settings

        BatteryRepository Battery { get; }

        #endregion Settings

        void SaveChanges();
    }
}