using Common.Models.Jobs;
using Dapper;
using log4net;
using Microsoft.Data.SqlClient;

namespace Data.Repositorys.Jobs
{
    public class OrderRepository
    {
        private static readonly ILog logger = LogManager.GetLogger("Order"); //Function 실행관련 Log

        private readonly string connectionString;
        private readonly List<Order> _orders = new List<Order>(); // cached data
        private readonly object _lock = new object();

        public OrderRepository(string connectionString)
        {
            this.connectionString = connectionString;
            createTable();
            Load();
        }

        private void createTable()
        {
            // 테이블 존재 여부 확인 쿼리
            string checkTableQuery = @"
               IF OBJECT_id('dbo.[JobScheduler_Order]', 'U') IS NULL
                BEGIN
                    CREATE TABLE dbo.[JobScheduler_Order]
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

        private void Load()
        {
            _orders.Clear();
            using (var con = new SqlConnection(connectionString))
            {
                foreach (var data in con.Query<Order>("SELECT * FROM [JobScheduler_Order]"))
                {
                    _orders.Add(data);
                    logger.Info($"Load:{data}");
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
                            INSERT INTO [JobScheduler_Order]
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
                    _orders.Add(add);
                    logger.Info($"Add: {add}");
                }
            }
        }

        public void Update(Order update)
        {
            lock (_lock)
            {
                using (var con = new SqlConnection(connectionString))
                {
                    const string UPDATE_SQL = @"
                            UPDATE [JobScheduler_Order]
                            SET
                                 [subType]                 = @subType
                                ,[sourceId]                = @sourceId
                                ,[destinationId]           = @destinationId
                                ,[carrierId]               = @carrierId
                                ,[drumKeyCode]             = @drumKeyCode
                                ,[orderedBy]               = @orderedBy
                                ,[orderedAt]               = @orderedAt
                                ,[priority]                = @priority
                                ,[stateCode]               = @stateCode
                                ,[state]                  = @state
                                ,[specifiedWorkerId]       = @specifiedWorkerId
                                ,[assignedWorkerId]        = @assignedWorkerId
                                ,[createdAt]               = @createdAt
                                ,[updatedAt]               = @updatedAt
                                ,[finishedAt]              = @finishedAt

                            WHERE  [id] = @id And [type] = @type";

                    con.Execute(UPDATE_SQL, param: update);
                    logger.Info($"Update: {update}");
                }
            }
        }

        public void Delete()
        {
            lock (_lock)
            {
                string massage = null;

                using (var con = new SqlConnection(connectionString))
                {
                    con.Execute("DELETE FROM [JobScheduler_Order]");
                    _orders.Clear();
                    logger.Info($"Delete");
                }
            }
        }

        public void Remove(Order remove)
        {
            lock (_lock)
            {
                string massage = null;

                using (var con = new SqlConnection(connectionString))
                {
                    con.Execute("DELETE FROM [JobScheduler_Order] WHERE id=@id", param: new { id = remove.id });
                    _orders.Remove(remove);
                    logger.Info($"Remove: {remove}");
                }
            }
        }

        public List<Order> GetAll()
        {
            lock (_lock)
            {
                return _orders.ToList();
            }
        }

        public List<Order> GetByOrderStatus(string orderStatus)
        {
            lock (_lock)
            {
                return _orders.Where(m => m.state == orderStatus).ToList();
            }
        }

        public Order GetByid(string id)
        {
            lock (_lock)
            {
                return _orders.FirstOrDefault(m => m.id == id);
            }
        }

        public Order GetBySource_Dest(string sourceId, string destId)
        {
            lock (_lock)
            {
                return _orders.FirstOrDefault(m => m.sourceId == sourceId && m.destinationId == destId);
            }
        }
        public Order GetByDest(string destId)
        {
            lock (_lock)
            {
                return _orders.FirstOrDefault(m => m.destinationId == destId);
            }
        }

        public Order GetByIdAndTypeAndSubType(string id, string type, string subType)
        {
            lock (_lock)
            {
                return _orders.Where(m => m.id == id && m.type == type && m.subType == subType).FirstOrDefault();
            }
        }

        public List<Order> GetByWorkerId(string workerId)
        {
            lock (_lock)
            {
                return _orders.Where(m => m.assignedWorkerId == workerId || m.specifiedWorkerId == workerId).ToList();
            }
        }

        public List<Order> GetByAssignWorkerId(string workerId)
        {
            lock (_lock)
            {
                return _orders.Where(m => m.assignedWorkerId == workerId).ToList();
            }
        }

        public List<Order> GetBySpecifiedWorkerId(string workerId)
        {
            lock (_lock)
            {
                return _orders.Where(m => m.specifiedWorkerId == workerId).ToList();
            }
        }
    }
}