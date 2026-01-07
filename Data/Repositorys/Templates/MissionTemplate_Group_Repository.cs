using Common.Models.Bases;
using Common.Models.Jobs;
using Common.Templates;
using Dapper;
using log4net;
using Microsoft.Data.SqlClient;
using System.Text.Json;

namespace Data.Repositorys.Templates
{
    public class MissionTemplate_Group_Repository
    {
        private static readonly ILog logger = LogManager.GetLogger("MissionTemplate_Group"); //Function 실행관련 Log
        private readonly string connectionString;
        private readonly List<MissionTemplate_Group> _missionTemplates = new List<MissionTemplate_Group>(); // cached data
        private readonly object _lock = new object();

        public MissionTemplate_Group_Repository(string connectionString)
        {
            this.connectionString = connectionString;
            createTable();
            Load();
        }

        private void createTable()
        {
            //VARCHAR 대신 NVARCHAR로 저장해야함 VARCHAR은 영문만 가능함
            // 테이블 존재 여부 확인 쿼리
            string checkTableQuery = @"
               IF OBJECT_id('dbo.[MissionTemplate_Group]', 'U') IS NULL
                BEGIN
                    CREATE TABLE dbo.[MissionTemplate_Group]
                    (

                        [guid]                    NVARCHAR(64)     NULL,
                        [group]                   NVARCHAR(64)     NULL,
                        [name]                    NVARCHAR(64)     NULL,
                        [service]                 NVARCHAR(64)     NULL,
                        [type]                    NVARCHAR(64)     NULL,
                        [subType]                 NVARCHAR(64)     NULL,
                        [seq]                  int             NULL,                        
                        [isLook]                  int             NULL,
                        [createdAt]               datetime        NULL,
                        [updatedAt]               datetime        NULL,
                        [parametersJson]              NVARCHAR(2000)    NULL,
                        [preReportsJson]              NVARCHAR(2000)    NULL,
                        [postReportsJson]             NVARCHAR(2000)    NULL,

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

        public void Load()
        {
            _missionTemplates.Clear();
            using (var con = new SqlConnection(connectionString))
            {
                foreach (var data in con.Query<MissionTemplate_Group>("SELECT * FROM [MissionTemplate_Group]"))
                {
                    //파라메타를 Json으로 되어있던것을 다시 List로 변경한다.
                    if (data.parametersJson != null) data.parameters = JsonSerializer.Deserialize<List<Parameter>>(data.parametersJson);
                    if (data.preReportsJson != null) data.preReports = JsonSerializer.Deserialize<List<PreReport>>(data.preReportsJson);
                    if (data.postReportsJson != null) data.postReports = JsonSerializer.Deserialize<List<PostReport>>(data.postReportsJson);
                    _missionTemplates.Add(data);

                    logger.Info($"Load:{data}");
                }
            }
        }

        public void Add(MissionTemplate_Group add)
        {
            lock (_lock)
            {
                using (var con = new SqlConnection(connectionString))
                {
                    const string INSERT_SQL = @"
                            INSERT INTO [MissionTemplate_Group]
                                 (
                                     [guid]
                                    ,[group]
                                    ,[name]
                                    ,[service]
                                    ,[type]
                                    ,[subType]
                                    ,[seq]                                    
                                    ,[isLook]
                                    ,[createdAt]
                                    ,[updatedAt]
                                    ,[parametersJson]
                                    ,[preReportsJson]
                                    ,[postReportsJson]
                                   )
                                  values
                                  (

                                     @guid
                                    ,@group
                                    ,@name
                                    ,@service
                                    ,@type
                                    ,@subType
                                    ,@seq                                    
                                    ,@isLook
                                    ,@createdAt
                                    ,@updatedAt
                                    ,@parametersJson
                                    ,@preReportsJson
                                    ,@postReportsJson
                                  );";
                    //TimeOut 시간을 60초로 연장 [기본30초]
                    //con.Execute(INSERT_SQL, param: add, commandTimeout: 60);
                    con.Execute(INSERT_SQL, param: add);
                    _missionTemplates.Add(add);
                    logger.Info($"Add: {add}");
                }
            }
        }

        public void Update(MissionTemplate_Group update)
        {
            lock (_lock)
            {
                using (var con = new SqlConnection(connectionString))
                {
                    const string UPDATE_SQL = @"
                            UPDATE [MissionTemplate_Group]
                            SET
                                     [group] = @group
                                    ,[name] = @name
                                    ,[service] = @service
                                    ,[type] = @type
                                    ,[subType] = @subType
                                    ,[seq] = @seq                                    
                                    ,[isLook] = @isLook
                                    ,[createdAt] = @createdAt
                                    ,[updatedAt] = @updatedAt
                                    ,[parametersJson] = @parametersJson
                                    ,[preReportsJson] = @preReportsJson
                                    ,[postReportsJson] = @postReportsJson
                            WHERE [guid] = @guid";
                    //TimeOut 시간을 60초로 연장 [기본30초]
                    //con.Execute(UPDATE_SQL, param: update, commandTimeout: 60);
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
                    con.Execute("DELETE FROM [MissionTemplate_Group]");
                    _missionTemplates.Clear();
                    logger.Info($"Delete");
                }
            }
        }

        public void Remove(MissionTemplate_Group remove)
        {
            lock (_lock)
            {
                string massage = null;

                using (var con = new SqlConnection(connectionString))
                {
                    con.Execute("DELETE FROM [MissionTemplate_Group] WHERE guid = @guid", param: new { guid = remove.guid });
                    _missionTemplates.Remove(remove);
                    logger.Info($"Remove: {remove}");
                }
            }
        }

        public MissionTemplate_Group GetById(string id)
        {
            lock (_lock)
            {
                return _missionTemplates.FirstOrDefault(m=>m.guid == id);
            }
        }
        public List<MissionTemplate_Group> GetAll()
        {
            lock (_lock)
            {
                return _missionTemplates.ToList();
            }
        }
        public List<MissionTemplate_Group> GetByGroup(string group)
        {
            lock (_lock)
            {
                return _missionTemplates.Where(m=>m.group == group).ToList();
            }
        }

        public List<Parameter> GetParametas(List<MissionTemplate_Group> missionTemplates)
        {
            //파라메타 내용을 찾을때 사용
            //1. parameters 가 null 인 Mission은 제외
            //2. List<Mission> → 모든 parameters 를 하나의 열로 평탄화
            //3. List<Parameter> 로 리턴
            lock (_lock)
            {
                return missionTemplates.Where(m => m.parameters != null).SelectMany(m => m.parameters).ToList();
            }
        }
    }
}