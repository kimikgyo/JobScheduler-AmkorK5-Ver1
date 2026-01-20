using Common.Models.Jobs;
using Common.Models.Settings;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Data.Repositorys.Settings
{
    public class BatteryRepository
    {
        private readonly string connectionString;
        private Battery _battery = null; // cached data
        private readonly object _lock = new object();

        public BatteryRepository(string connectionString)
        {
            this.connectionString = connectionString;
            createTable();
            Load();
        }

        private void createTable()
        {
            // 테이블 존재 여부 확인 쿼리
            string checkTableQuery = @"
               IF OBJECT_id('dbo.[JobScheduler_Battery]', 'U') IS NULL
                BEGIN
                    CREATE TABLE dbo.[JobScheduler_Battery]
                    (
                        [minimum]                   float         NOT NULL,
                        [crossCharge]               float         NOT NULL,                        
                        [chargeStart]               float         NOT NULL,
                        [chargeEnd]                 float         NOT NULL,
                        [createAt]               datetime        NULL,
                        [updatedAt]               datetime        NULL,

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
            using (var con = new SqlConnection(connectionString))
            {
                _battery = con.Query<Battery>("SELECT * FROM [JobScheduler_Battery]").FirstOrDefault();
                if (_battery == null)
                {
                    _battery = new Battery
                    {
                        minimum = 0,
                        crossCharge = 0,
                        chargeStart = 0,
                        chargeEnd = 0,
                        createAt = DateTime.Now,
                        updatedAt = null
                    };
                    Add(_battery);
                }
            }
        }

        public void Add(Battery add)
        {
            lock (_lock)
            {
                string massage = null;

                using (var con = new SqlConnection(connectionString))
                {
                    const string INSERT_SQL = @"
                            INSERT INTO [JobScheduler_Battery]
                                   (
                                     [minimum]
                                    ,[crossCharge]                                   
                                    ,[chargeStart]
                                    ,[chargeEnd]
                                    ,[createAt]
                                    ,[updatedAt]
                                   )
                                  values
                                  (
                                     @minimum
                                    ,@crossCharge
                                    ,@chargeStart
                                    ,@chargeEnd
                                    ,@createAt
                                    ,@updatedAt
                                  );";
                    con.Execute(INSERT_SQL, param: add);
                }
            }
        }

        public void Update(Battery model)
        {
            lock (_lock)
            {
                using (var con = new SqlConnection(connectionString))
                {
                    const string UPDATE_SQL = @"
                    UPDATE [JobScheduler_Battery]
                    SET
                         [minimum]         = @minimum
                        ,[crossCharge]     = @crossCharge
                        ,[chargeStart]     = @chargeStart
                        ,[ChargeEnd]       = @chargeEnd
                        ,[createAt]        = @createAt
                        ,[updatedAt]       = @updatedAt
                        ";

                    con.Execute(UPDATE_SQL, param: model);
                }
            }
        }

        public Battery GetAll()
        {
            lock (_lock)
            {
                return _battery;
            }
        }
    }
}