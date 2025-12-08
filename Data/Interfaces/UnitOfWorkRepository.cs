using Common.Models.Areas;
using Data.Repositorys.Areas;
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
using System.Data;

namespace Data.Interfaces
{
    public class ConnectionStrings
    {
        public static readonly string DB1 = @"Data SOURCE=.\SQLEXPRESS;Initial Catalog=AmkorK5_JobScheduler; User ID = sa;TrustServerCertificate=true; Password=acsserver;Connect Timeout=30;";
        //public static readonly string DB1 = @"Data Source=192.168.8.215,1433; Initial Catalog=JobScheduler; User ID = sa; Password=acsserver; Connect Timeout=30; TrustServerCertificate=true"; // STI
    }

    public class UnitOfWorkRepository : IUnitOfWorkRepository
    {
        private IDbConnection _db;

        private static readonly string connectionString = ConnectionStrings.DB1;

        #region Base

        public CarrierRepository Carriers { get; private set; }
        public MapRepository Maps { get; private set; }
        public WorkerRepository Workers { get; private set; }
        public PositionRepository Positions { get; private set; }
        public MiddlewareRepository Middlewares { get; private set; }
        public ElevatorRepository Elevator { get; private set; }

        #endregion Base

        public MissionRepository Missions { get; private set; }
        public JobRepository Jobs { get; private set; }
        public OrderRepository Orders { get; private set; }

        public MissionHistoryRepository MissionHistorys { get; private set; }
        public JobHistoryRepository JobHistorys { get; private set; }
        public OrderHistoryRepository OrderHistorys { get; private set; }

        public MissionFinishedHistoryRepository MissionFinishedHistorys { get; private set; }
        public JobFinishedHistoryRepository JobFinishedHistorys { get; private set; }
        public OrderFinishedHistoryRepository OrderFinishedHistorys { get; private set; }

        public MissionTemplate_Group_Repository MissionTemplates_Group { get; private set; }
        public MissionTemplate_Single_Repository MissionTemplates_Single { get; private set; }
        public ACS_AreaRepository ACSAreas { get; private set; }
        public ServiceApiRepository ServiceApis { get; private set; }

        #region Settings

        public BatteryRepository Battery { get; private set; }

        #endregion Settings

        public UnitOfWorkRepository()
        {
            repository();
        }

        private void repository()
        {
            #region Base

            Carriers = new CarrierRepository(connectionString);
            Maps = new MapRepository(connectionString);
            Workers = new WorkerRepository(connectionString);
            Positions = new PositionRepository(connectionString);
            Middlewares = new MiddlewareRepository(connectionString);
            Elevator = new ElevatorRepository(connectionString);

            #endregion Base

            OrderHistorys = new OrderHistoryRepository(connectionString);
            JobHistorys = new JobHistoryRepository(connectionString);
            MissionHistorys = new MissionHistoryRepository(connectionString);
            MissionFinishedHistorys = new MissionFinishedHistoryRepository(connectionString);
            JobFinishedHistorys = new JobFinishedHistoryRepository(connectionString);
            OrderFinishedHistorys = new OrderFinishedHistoryRepository(connectionString);

            Orders = new OrderRepository(connectionString);
            Jobs = new JobRepository(connectionString);
            Missions = new MissionRepository(connectionString);

            MissionTemplates_Group = new MissionTemplate_Group_Repository(connectionString);
            MissionTemplates_Single = new MissionTemplate_Single_Repository(connectionString);

            ServiceApis = new ServiceApiRepository(connectionString);
            ACSAreas = new ACS_AreaRepository (connectionString);

            #region Settings

            Battery = new BatteryRepository(connectionString);

            #endregion Settings
        }

        public void SaveChanges()
        {
        }

        public void Dispose()
        {
        }
    }
}