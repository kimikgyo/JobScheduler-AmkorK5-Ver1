using Common.Models.Jobs;
using Dapper;
using Data.Repositorys.Historys;
using log4net;
using Microsoft.Data.SqlClient;

namespace Data.Repositorys.Jobs
{
    public class JobRepository
    {
        private static readonly ILog logger = LogManager.GetLogger("Job"); //Function 실행관련 Log

        private readonly string connectionString;
        private readonly object _lock = new object();
        private readonly List<Job> _jobs = new List<Job>(); // cached data

        public JobRepository(string connectionString)
        {
            this.connectionString = connectionString;
            createTable();
            Load();
        }

        private void createTable()
        {
            // 테이블 존재 여부 확인 쿼리
            string checkTableQuery = @"
               IF OBJECT_id('dbo.[Job]', 'U') IS NULL
                BEGIN
                    CREATE TABLE dbo.[Job]
                    (

                            [orderId]             NVARCHAR(64)     NULL,
                            [guid]                NVARCHAR(64)     NULL,
                            [group]               NVARCHAR(64)     NULL,
                            [name]                NVARCHAR(64)     NULL,
                            [type]                NVARCHAR(64)     NULL,
                            [subType]             NVARCHAR(64)     NULL,
                            [sequence]            int             NULL,
                            [carrierId]           NVARCHAR(64)     NULL,
                            [drumKeyCode]           NVARCHAR(64)     NULL,
                            [sourceId]            NVARCHAR(64)     NULL,
                            [sourceName]          NVARCHAR(64)     NULL,
                            [sourcelinkedFacility]          NVARCHAR(64)     NULL,
                            [destinationId]       NVARCHAR(64)     NULL,
                            [destinationName]     NVARCHAR(64)     NULL,
                            [destinationlinkedFacility]     NVARCHAR(64)     NULL,
                            [isLocked]            int             NULL,
                            [state]              NVARCHAR(64)     NULL,
                            [specifiedWorkerId]   NVARCHAR(64)     NULL,
                            [assignedWorkerId]   NVARCHAR(64)     NULL,
                            [createdAt]           datetime        NULL,
                            [updatedAt]           datetime        NULL,
                            [finishedAt]          datetime        NULL,
                            [terminationType]    NVARCHAR(64)     NULL,
                            [terminateState]     NVARCHAR(64)     NULL,
                            [terminator]         NVARCHAR(64)     NULL,
                            [terminatingAt]      datetime         NULL,
                            [terminatedAt]       datetime         NULL,
                    );
                END;
            ";
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand(checkTableQuery, connection))
                {
                    command.ExecuteNonQuery();
                }
            }
        }

        private void Load()
        {
            _jobs.Clear();
            using (var con = new SqlConnection(connectionString))
            {
                foreach (var data in con.Query<Job>("SELECT * FROM [Job]"))
                {
                    _jobs.Add(data);
                    logger.Info($"Load:{data}");
                }
            }
        }

        public void Add(Job add)
        {
            lock (_lock)
            {
                using (var con = new SqlConnection(connectionString))
                {
                    const string INSERT_SQL = @"
                            INSERT INTO Job
                                  (
                                     [orderId]
                                    ,[guid]
                                    ,[group]
                                    ,[name]
                                    ,[type]
                                    ,[subType]
                                    ,[sequence]
                                    ,[carrierId]
                                    ,[drumKeyCode]
                                    ,[sourceId]
                                    ,[sourceName]
                                    ,[sourcelinkedFacility]
                                    ,[destinationId]
                                    ,[destinationName]
                                    ,[destinationlinkedFacility]
                                    ,[isLocked]
                                    ,[state]
                                    ,[specifiedWorkerId]
                                    ,[assignedWorkerId]
                                    ,[createdAt]
                                    ,[updatedAt]
                                    ,[finishedAt]
                                    ,[terminationType]
                                    ,[terminateState]
                                    ,[terminator]
                                    ,[terminatingAt]
                                    ,[terminatedAt]
                                   )
                                  values
                                  (
                                     @orderId
                                    ,@guid
                                    ,@group
                                    ,@name
                                    ,@type
                                    ,@subType
                                    ,@sequence
                                    ,@carrierId
                                    ,@drumKeyCode
                                    ,@sourceId
                                    ,@sourceName
                                    ,@sourcelinkedFacility
                                    ,@destinationId
                                    ,@destinationName
                                    ,@destinationlinkedFacility
                                    ,@isLocked
                                    ,@state
                                    ,@specifiedWorkerId
                                    ,@assignedWorkerId
                                    ,@createdAt
                                    ,@updatedAt
                                    ,@finishedAt
                                    ,@terminationType
                                    ,@terminateState
                                    ,@terminator
                                    ,@terminatingAt
                                    ,@terminatedAt
                                  );";
                    con.Execute(INSERT_SQL, param: add);
                    _jobs.Add(add);
                    logger.Info($"Add: {add}");
                }
            }
        }

        public void Update(Job update)
        {
            lock (_lock)
            {
                string massage = null;

                using (var con = new SqlConnection(connectionString))
                {
                    const string UPDATE_SQL = @"
                            UPDATE [Job]
                            SET
                                     [orderId]                  = @orderId
                                    ,[group]                    = @group
                                    ,[name]                     = @name
                                    ,[type]                     = @type
                                    ,[subType]                  = @subType
                                    ,[sequence]                 = @sequence
                                    ,[carrierId]                = @carrierId
                                    ,[drumKeyCode]              = @drumKeyCode
                                    ,[sourceId]                 = @sourceId
                                    ,[sourceName]               = @sourceName
                                    ,[sourcelinkedFacility]     = @sourcelinkedFacility
                                    ,[destinationId]            = @destinationId
                                    ,[destinationName]          = @destinationName
                                    ,[destinationlinkedFacility] = @destinationlinkedFacility
                                    ,[isLocked]                 = @isLocked
                                    ,[state]                    = @state
                                    ,[specifiedWorkerId]        = @specifiedWorkerId
                                    ,[assignedWorkerId]         = @assignedWorkerId
                                    ,[createdAt]                = @createdAt
                                    ,[updatedAt]                = @updatedAt
                                    ,[finishedAt]               = @finishedAt
                                    ,[terminationType]          = @terminationType
                                    ,[terminateState]           = @terminateState
                                    ,[terminator]               = @terminator
                                    ,[terminatingAt]            = @terminatingAt
                                    ,[terminatedAt]             = @terminatedAt

                            WHERE [guid] = @guid";
                    con.Execute(UPDATE_SQL, param: update);
                    logger.Info($"Update: {update}");
                }
            }
        }

        public void Delete()
        {
            lock (_lock)
            {
                using (var con = new SqlConnection(connectionString))
                {
                    con.Execute("DELETE FROM [Job]");
                    _jobs.Clear();
                    logger.Info($"Delete");
                }
            }
        }

        public void Remove(Job remove)
        {
            lock (_lock)
            {
                string massage = null;

                using (var con = new SqlConnection(connectionString))
                {
                    con.Execute("DELETE FROM [Job] WHERE guid = @guid", param: new { guid = remove.guid });
                    _jobs.Remove(remove);
                    logger.Info($"Remove: {remove}");
                }
            }
        }

        public List<Job> GetAll()
        {
            lock (_lock)
            {
                return _jobs.ToList();
            }
        }

        public Job GetByOrderId(string orderId, string type, string subType)
        {
            lock (_lock)
            {
                return _jobs.FirstOrDefault(m => m.orderId == orderId && m.type == type && (m.subType == subType || m.subType == $"{subType}WITHEV"));
            }
        }

        public List<Job> GetByInit()
        {
            lock (_lock)
            {
                return _jobs.Where(m => m.state == nameof(JobState.INIT)).ToList();
            }
        }

        public Job GetByid(string id)
        {
            lock (_lock)
            {
                return _jobs.FirstOrDefault(m => m.guid == id);
            }
        }

        public List<Job> GetByWorkerId(string workerId)
        {
            lock (_lock)
            {
                return _jobs.Where(m => m.assignedWorkerId == workerId || m.specifiedWorkerId == workerId).ToList();
            }
        }

        public List<Job> GetByAssignWorkerId(string workerId)
        {
            lock (_lock)
            {
                return _jobs.Where(m => m.assignedWorkerId == workerId).ToList();
            }
        }

        public List<Job> GetBySpecifiedWorkerId(string workerId)
        {
            lock (_lock)
            {
                return _jobs.Where(m => m.specifiedWorkerId == workerId).ToList();
            }
        }
    }
}