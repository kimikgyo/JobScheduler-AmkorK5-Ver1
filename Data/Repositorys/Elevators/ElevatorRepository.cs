using Common.Models.Bases;
using Common.Models.Jobs;
using log4net;

namespace Data.Repositorys.Elevators
{
    public class ElevatorRepository
    {
        private static readonly ILog logger = LogManager.GetLogger("Elevator"); //Function 실행관련 Log
        private readonly string connectionString;
        private readonly List<Elevator> _elevators= new List<Elevator>(); // cached data
        private readonly object _lock = new object();

        public ElevatorRepository(string connectionString)
        {
            this.connectionString = connectionString;
            createTable();
            Load();
        }

        private void createTable()
        {
            //VARCHAR 대신 NVARCHAR로 저장해야함 VARCHAR은 영문만 가능함
            // 테이블 존재 여부 확인 쿼리
            //string checkTableQuery = @"
            //   IF OBJECT_id('dbo.[Mission]', 'U') IS NULL
            //    BEGIN
            //        CREATE TABLE dbo.[Mission]
            //        (
            //             [orderId]               NVARCHAR(64)     NULL
            //            ,[jobId]                 NVARCHAR(64)     NULL
            //            ,[guid]                  NVARCHAR(64)     NULL
            //            ,[carrierId]             NVARCHAR(64)     NULL
            //            ,[service]               NVARCHAR(64)     NULL
            //            ,[type]                  NVARCHAR(64)     NULL
            //            ,[subType]               NVARCHAR(64)     NULL
            //            ,[linkedFacility]        NVARCHAR(64)     NULL
            //            ,[sequence]              int              NULL
            //            ,[isLocked]              int              NULL
            //            ,[sequenceChangeCount]   int              NULL
            //            ,[retryCount]            int              NULL
            //            ,[state]                 NVARCHAR(64)     NULL
            //            ,[specifiedWorkerId]     NVARCHAR(64)     NULL
            //            ,[assignedWorkerId]      NVARCHAR(64)     NULL
            //            ,[elevatorId]            NVARCHAR(64)     NULL
            //            ,[sourceFloor]           NVARCHAR(64)     NULL
            //            ,[destinationFloor]      NVARCHAR(64)     NULL
            //            ,[createdAt]             datetime         NULL
            //            ,[updatedAt]             datetime         NULL
            //            ,[finishedAt]            datetime         NULL
            //        );
            //    END;
            //";
            //using (SqlConnection connection = new SqlConnection(connectionString))
            //{
            //    connection.Open();
            //    using (SqlCommand command = new SqlCommand(checkTableQuery, connection))
            //    {
            //        command.ExecuteNonQuery();
            //    }
            //}
        }

        private void Load()
        {
            //_missions.Clear();
            //using (var con = new SqlConnection(connectionString))
            //{
            //    foreach (var data in con.Query<Mission>("SELECT * FROM [Mission]"))
            //    {
            //        _missions.Add(data);

            //        logger.Info($"Load:{data}");
            //    }
            //}
        }

        public void Add(Elevator add)
        {
            _elevators.Add(add);
            logger.Info($"Add: {add}");

            //lock (_lock)
            //{
            //    using (var con = new SqlConnection(connectionString))
            //    {
            //        const string INSERT_SQL = @"
            //                INSERT INTO [Mission]
            //                     (
            //                           [orderId]
            //                          ,[jobId]
            //                          ,[guid]
            //                          ,[carrierId]
            //                          ,[service]
            //                          ,[type]
            //                          ,[subType]
            //                          ,[linkedFacility]
            //                          ,[sequence]
            //                          ,[isLocked]
            //                          ,[sequenceChangeCount]
            //                          ,[retryCount]
            //                          ,[state]
            //                          ,[specifiedWorkerId]
            //                          ,[assignedWorkerId]
            //                          ,[elevatorId]
            //                          ,[sourceFloor]
            //                          ,[destinationFloor]
            //                          ,[createdAt]
            //                          ,[updatedAt]
            //                          ,[finishedAt]
            //                       )
            //                      values
            //                      (
            //                             @orderId
            //                            ,@jobId
            //                            ,@guid
            //                            ,@carrierId
            //                            ,@service
            //                            ,@type
            //                            ,@subType
            //                            ,@linkedFacility
            //                            ,@sequence
            //                            ,@isLocked
            //                            ,@sequenceChangeCount
            //                            ,@retryCount
            //                            ,@state
            //                            ,@specifiedWorkerId
            //                            ,@assignedWorkerId
            //                            ,@elevatorId
            //                            ,@sourceFloor
            //                            ,@destinationFloor
            //                            ,@createdAt
            //                            ,@updatedAt
            //                            ,@finishedAt
            //                      );";
            //        //TimeOut 시간을 60초로 연장 [기본30초]
            //        //con.Execute(INSERT_SQL, param: add, commandTimeout: 60);
            //        con.Execute(INSERT_SQL, param: add);
            //        _missions.Add(add);
            //        logger.Info($"Add: {add}");
            //    }
            //}
        }

        public void Update(Elevator update)
        {
            logger.Info($"Update: {update}");

            //lock (_lock)
            //{
            //    using (var con = new SqlConnection(connectionString))
            //    {
            //        const string UPDATE_SQL = @"
            //                UPDATE [Mission]
            //                SET
            //                     [orderId]                = @orderId
            //                    ,[jobId]                  = @jobId
            //                    ,[carrierId]              = @carrierId
            //                    ,[service]                = @service
            //                    ,[type]                   = @type
            //                    ,[subType]                = @subType
            //                    ,[linkedFacility]         = @linkedFacility
            //                    ,[sequence]               = @sequence
            //                    ,[isLocked]               = @isLocked
            //                    ,[sequenceChangeCount]    = @sequenceChangeCount
            //                    ,[retryCount]             = @retryCount
            //                    ,[state]                  = @state
            //                    ,[specifiedWorkerId]      = @specifiedWorkerId
            //                    ,[assignedWorkerId]       = @assignedWorkerId
            //                    ,[elevatorId]             = @elevatorId
            //                    ,[sourceFloor]            = @sourceFloor
            //                    ,[destinationFloor]       = @destinationFloor
            //                    ,[createdAt]              = @createdAt
            //                    ,[updatedAt]              = @updatedAt
            //                    ,[finishedAt]             = @finishedAt
            //                WHERE [guid] = @guid";
            //        //TimeOut 시간을 60초로 연장 [기본30초]
            //        //con.Execute(UPDATE_SQL, param: update, commandTimeout: 60);
            //        con.Execute(UPDATE_SQL, param: update);
            //        logger.Info($"Update: {update}");
            //    }
            //}
        }

        public void Delete()
        {
            logger.Info($"Delete");

            //lock (_lock)
            //{
            //    string massage = null;

            //    using (var con = new SqlConnection(connectionString))
            //    {
            //        con.Execute("DELETE FROM [Mission]");
            //        _missions.Clear();
            //        logger.Info($"Delete");
            //    }
            //}
        }

        public void Remove(Elevator remove)
        {
            _elevators.Remove(remove);
            logger.Info($"Remove: {remove}");

            //lock (_lock)
            //{
            //    string massage = null;

            //    using (var con = new SqlConnection(connectionString))
            //    {
            //        con.Execute("DELETE FROM [Mission] WHERE guid = @guid", param: new { guid = remove.guid });
            //        _missions.Remove(remove);
            //        logger.Info($"Remove: {remove}");
            //    }
            //}
        }

        public List<Elevator> GetAll()
        {
            lock (_lock)
            {
                return _elevators.ToList();
            }
        }

        public Elevator GetById(string id)
        {
            lock (_lock)
            {
                return _elevators.FirstOrDefault(m => m.id == id);
            }
        }
    }
}
