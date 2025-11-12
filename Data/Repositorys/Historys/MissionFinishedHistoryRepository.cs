using Common.Models.Jobs;
using Common.Templates;
using Dapper;
using log4net;
using Microsoft.Data.SqlClient;
using System.Reflection;
using System.Text.Json;

namespace Data.Repositorys.Historys
{
    public class MissionFinishedHistoryRepository
    {
        private readonly string connectionString;
        private readonly List<Mission> histories = new List<Mission>();
        private readonly object _lock = new object();
        public MissionFinishedHistoryRepository(string connectionString)
        {
            this.connectionString = connectionString;
            createTable();
        }

        private void createTable()
        {
            // 테이블 존재 여부 확인 쿼리
            string checkTableQuery = @"
               IF OBJECT_id('dbo.[MissionFinishedHistory]', 'U') IS NULL
                BEGIN
                    CREATE TABLE dbo.[MissionFinishedHistory]
                    (
                       [orderId]                  NVARCHAR(64)     NULL,
                        [jobId]                    NVARCHAR(64)     NULL,
                        [guid]                     NVARCHAR(64)     NULL,
                        [carrierId]                NVARCHAR(64)     NULL,
                        [name]                     NVARCHAR(64)     NULL,
                        [service]                  NVARCHAR(64)     NULL,
                        [type]                     NVARCHAR(64)     NULL,
                        [subType]                  NVARCHAR(64)     NULL,
                        [linkedFacility]           NVARCHAR(64)     NULL,
                        [sequence]                 int             NULL,
                        [isLocked]                 int             NULL,
                        [sequenceChangeCount]      int             NULL,
                        [retryCount]               int             NULL,
                        [state]                   NVARCHAR(64)     NULL,
                        [specifiedWorkerId]        NVARCHAR(64)     NULL,
                        [assignedWorkerId]         NVARCHAR(64)     NULL,
                        [createdAt]                datetime        NULL,
                        [updatedAt]                datetime        NULL,
                        [finishedAt]               datetime        NULL,
                        [sequenceUpdatedAt]        datetime        NULL,
                        [parametersJson]            NVARCHAR(2000)    NULL,
                        [preReportsJson]            NVARCHAR(2000)    NULL,
                        [postReportsJson]           NVARCHAR(2000)    NULL,

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

        public void Add(Mission add)
        {
            lock (_lock)
            {
                using (var con = new SqlConnection(connectionString))
                {
                    const string INSERT_SQL = @"
                            INSERT INTO [MissionFinishedHistory]
                                   (
                                      [orderId]
                                      ,[jobId]
                                      ,[guid]
                                      ,[carrierId]
                                      ,[name]
                                      ,[service]
                                      ,[type]
                                      ,[subType]
                                      ,[linkedFacility]
                                      ,[sequence]
                                      ,[isLocked]
                                      ,[sequenceChangeCount]
                                      ,[retryCount]
                                      ,[state]
                                      ,[specifiedWorkerId]
                                      ,[assignedWorkerId]
                                      ,[createdAt]
                                      ,[updatedAt]
                                      ,[finishedAt]
                                      ,[sequenceUpdatedAt]
                                      ,[parametersJson]
                                      ,[preReportsJson]
                                      ,[postReportsJson]
                                   )
                                  values
                                  (
                                     	 @orderId
                                        ,@jobId
                                        ,@guid
                                        ,@carrierId
                                        ,@name
                                        ,@service
                                        ,@type
                                        ,@subType
                                        ,@linkedFacility
                                        ,@sequence
                                        ,@isLocked
                                        ,@sequenceChangeCount
                                        ,@retryCount
                                        ,@state
                                        ,@specifiedWorkerId
                                        ,@assignedWorkerId
                                        ,@createdAt
                                        ,@updatedAt
                                        ,@finishedAt
                                        ,@sequenceUpdatedAt
                                        ,@parametersJson
                                        ,@preReportsJson
                                        ,@postReportsJson

                                  );";
                    con.Execute(INSERT_SQL, param: add);
                }
            }
        }

        public List<Mission> FindHistoryOrderId(string orderId)
        {
            lock (_lock)
            {
                histories.Clear();
                var sql = @"SELECT * FROM [MissionFinishedHistory] WHERE orderId = @orderId";
                using (var con = new SqlConnection(connectionString))
                {
                    foreach (var data in con.Query<Mission>(sql, new { orderId = orderId }))
                    {
                        data.parameters = JsonSerializer.Deserialize<List<Parameta>>(data.parametersJson);
                        histories.Add(data);
                    }
                }
            }
            return histories;
        }

        public List<Mission> FindHistoryJobId(string jobId)
        {
            lock (_lock)
            {
                histories.Clear();
                var sql = @"SELECT * FROM [MissionFinishedHistory] WHERE jobId = @jobId";
                using (var con = new SqlConnection(connectionString))
                {
                    foreach (var data in con.Query<Mission>(sql, new { jobId = jobId }))
                    {
                        data.parameters = JsonSerializer.Deserialize<List<Parameta>>(data.parametersJson);
                        histories.Add(data);
                    }
                }
            }
            return histories;
        }

        public List<Mission> FindHistory(DateTime start, DateTime end)
        {
            lock (_lock)
            {
                histories.Clear();
                var sql = @"SELECT * FROM [MissionFinishedHistory] WHERE finishedAt >= @start AND finishedAt <= @end";
                using (var con = new SqlConnection(connectionString))
                {
                    foreach (var data in con.Query<Mission>(sql, new { start = start, end = end }))
                    {
                        data.parameters = JsonSerializer.Deserialize<List<Parameta>>(data.parametersJson);
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
                    var sql = @"DELETE [MissionFinishedHistory] WHERE finishedAt <= @endAt";
                    object queryParams = new { endAt = endAt };
                    var list = con.Execute(sql, queryParams);
                }
            }
        }
    }
}