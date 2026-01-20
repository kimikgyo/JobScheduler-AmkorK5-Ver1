using Common.Models.Jobs;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Data.Repositorys.Historys
{
    public class OrderHistoryRepository
    {
        private readonly string connectionString;
        private readonly List<Order> histories = new List<Order>();
        private readonly object _lock = new object();

        public OrderHistoryRepository(string connectionString)
        {
            this.connectionString = connectionString;
            createTable();
        }

        private void createTable()
        {
            // 테이블 존재 여부 확인 쿼리
            string checkTableQuery = @"
               IF OBJECT_id('dbo.[JobScheduler_OrderHistory]', 'U') IS NULL
                BEGIN
                    CREATE TABLE dbo.[JobScheduler_OrderHistory]
                     (
                       [id]                      NVARCHAR(64)     NULL,
                        [type]                    NVARCHAR(64)     NULL,
                        [subType]                 NVARCHAR(64)     NULL,
                        [sourceId]                NVARCHAR(64)     NULL,
                        [destinationId]           NVARCHAR(64)     NULL,
                        [carrierId]               NVARCHAR(64)     NULL,
                        [drumKeyCode]             NVARCHAR(64)     NULL,
                        [orderedBy]               NVARCHAR(64)     NULL,
                        [orderedAt]               datetime        NULL,
                        [priority]                int             NULL,
                        [stateCode]               int               NULL,
                        [state]                   NVARCHAR(64)     NULL,
                        [specifiedWorkerId]       NVARCHAR(64)     NULL,
                        [assignedWorkerId]        NVARCHAR(64)     NULL,
                        [createdAt]               datetime        NULL,
                        [updatedAt]               datetime        NULL,
                        [finishedAt]              datetime        NULL,
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

        public void Add(Order add)
        {
            lock (_lock)
            {
                string massage = null;

                using (var con = new SqlConnection(connectionString))
                {
                    const string INSERT_SQL = @"
                            INSERT INTO [JobScheduler_OrderHistory]
                                  (
                                     [id]
                                    ,[type]
                                    ,[subType]
                                    ,[sourceId]
                                    ,[destinationId]
                                    ,[carrierId]
                                    ,[drumKeyCode]
                                    ,[orderedBy]
                                    ,[orderedAt]
                                    ,[priority]
                                    ,[stateCode]
                                    ,[state]
                                    ,[specifiedWorkerId]
                                    ,[assignedWorkerId]
                                    ,[createdAt]
                                    ,[updatedAt]
                                    ,[finishedAt]
                                   )
                                  values
                                  (
                                     @id
                                    ,@type
                                    ,@subType
                                    ,@sourceId
                                    ,@destinationId
                                    ,@carrierId
                                    ,@drumKeyCode
                                    ,@orderedBy
                                    ,@orderedAt
                                    ,@priority
                                    ,@stateCode
                                    ,@state
                                    ,@specifiedWorkerId
                                    ,@assignedWorkerId
                                    ,@createdAt
                                    ,@updatedAt
                                    ,@finishedAt
                                  );";
                    con.Execute(INSERT_SQL, param: add);
                }
            }
        }

        public List<Order> FindHistory(DateTime start, DateTime end)
        {
            lock (_lock)
            {
                histories.Clear();
                var sql = @"SELECT * FROM [JobScheduler_OrderHistory] WHERE finishedAt >= @start AND finishedAt <= @end";
                using (var con = new SqlConnection(connectionString))
                {
                    foreach (var data in con.Query<Order>(sql, new { start = start, end = end }))
                    {
                        histories.Add(data);
                    }
                }
            }
            return histories;
        }

        public void PastDataDelete(DateTime endAt)
        {
            lock (_lock)
            {
                using (var con = new SqlConnection(connectionString))
                {
                    var sql = @"DELETE [JobScheduler_OrderHistory] WHERE finishedAt <= @endAt";
                    object queryParams = new { endAt = endAt };
                    var list = con.Execute(sql, queryParams);
                }
            }
        }
    }
}