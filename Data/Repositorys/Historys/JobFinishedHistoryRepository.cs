using Common.Models.Jobs;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Data.Repositorys.Historys
{
    public class JobFinishedHistoryRepository
    {
        private readonly string connectionString;
        private readonly List<Job> histories = new List<Job>();
        private readonly object _lock = new object();

        public JobFinishedHistoryRepository(string connectionString)
        {
            this.connectionString = connectionString;
            createTable();
        }

        private void createTable()
        {
            // 테이블 존재 여부 확인 쿼리
            string checkTableQuery = @"
               IF OBJECT_id('dbo.[JobFinishedHistory]', 'U') IS NULL
                BEGIN
                    CREATE TABLE dbo.[JobFinishedHistory]
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

        public void Add(Job add)
        {
            lock (_lock)
            {
                using (var con = new SqlConnection(connectionString))
                {
                    const string INSERT_SQL = @"
                            INSERT INTO [JobFinishedHistory]
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
                }
            }
        }

        public List<Job> FindHistoryOrderId(string orderId)
        {
            lock (_lock)
            {
                histories.Clear();
                var sql = @"SELECT * FROM [JobFinishedHistory] WHERE orderId = @orderId";
                using (var con = new SqlConnection(connectionString))
                {
                    foreach (var data in con.Query<Job>(sql, new { orderId = orderId }))
                    {
                        histories.Add(data);
                    }
                }
            }
            return histories;
        }

        public List<Job> FindHistory(DateTime start, DateTime end)
        {
            lock (_lock)
            {
                histories.Clear();
                var sql = @"SELECT * FROM [JobFinishedHistory] WHERE finishedAt >= @start AND finishedAt <= @end";
                using (var con = new SqlConnection(connectionString))
                {
                    foreach (var data in con.Query<Job>(sql, new { start = start, end = end }))
                    {
                        histories.Add(data);
                    }
                }
                return histories;
            }
        }

        public void PastDataDelete(DateTime endAt)
        {
            lock (_lock)
            {
                using (var con = new SqlConnection(connectionString))
                {
                    var sql = @"DELETE [JobFinishedHistory] WHERE finishedAt <= @endAt";
                    object queryParams = new { endAt = endAt };
                    var list = con.Execute(sql, queryParams);
                }
            }
        }
    }
}