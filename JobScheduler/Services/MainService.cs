using Common.Models;
using Common.Models.Jobs;
using Data.Interfaces;
using JOB.JobQueues.Interfaces;
using JOB.Mappings.Interfaces;
using JOB.MQTTs.Interfaces;
using JOB.Services;
using log4net;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;

namespace JobScheduler.Services
{
    public class MainService
    {
        private static readonly ILog EventLogger = LogManager.GetLogger("Event");

        public readonly IUnitOfWorkRepository _repository;
        public readonly IUnitOfWorkJobMissionQueue _jobMissionQueue;
        public readonly IConfiguration _configuration;
        public readonly IUnitOfWorkMapping _mapping;
        public readonly IUnitofWorkMqttQueue _mqttQueue;
        public readonly IMqttWorker _mqtt;

        private MainService main = null;
        private GetDataService getData = null;
        private MQTTService mQTT = null;
        private SchedulerService schedulerService = null;

        public MainService(IUnitOfWorkRepository repository, IUnitOfWorkJobMissionQueue workJobMissionQueue, IUnitOfWorkMapping mapping, IUnitofWorkMqttQueue mqttQueue, IMqttWorker mqtt)
        {
            main = this;
            _repository = repository;
            _jobMissionQueue = workJobMissionQueue;
            _mapping = mapping;
            _mqttQueue = mqttQueue;
            _mqtt = mqtt;
            createClass();
            stratAsync();
        }

        private void createClass()
        {
            schedulerService = new SchedulerService(main, _repository, _jobMissionQueue, _mapping, _mqttQueue);
            mQTT = new MQTTService(_mqtt, _mqttQueue);
            getData = new GetDataService(EventLogger, _repository, _mapping);
        }

        private async Task stratAsync()
        {
            Start();
            bool getdataComplete = await getData.StartAsyc();
            if (getdataComplete)
            {
                mQTT.Start();
                schedulerService.Start();
            }
        }

        /// <summary>
        /// 스케줄러를 멈춘 뒤, 데이터 리로드 → 다시 시작
        /// </summary>
        public async Task ReloadAndRestartAsync()
        {
            // 1. 스케줄러 정지 (Task 종료될 때까지 대기)
            await schedulerService.StopAsync();
            // StopAsync 내부에서 while 루프 빠져나오고 Task.WhenAll() 대기하도록 구현

            // 2. 데이터 리로드
            bool getDataComplete = await getData.ReloadAsyc();
            if (getDataComplete)
            {
                //// 3. MQTT 다시 시작 (필요시)
                //_mqtt.Start();

                // 4. 스케줄러 다시 시작
                schedulerService.Start();
            }
        }

        private void Start()
        {
            Task.Run(() => log_DataDelete());
        }

        private async Task log_DataDelete()
        {
            while (true)
            {
                try
                {
                    int deleteAddDay = 180;// 30;
                    DateTime searchDateTime = DateTime.Now.AddDays(-(deleteAddDay));
                    PastLogDelete(searchDateTime);
                    PastDataDelete(searchDateTime);

                    //12시간 대기
                    await Task.Delay(43200000);
                }
                catch (Exception ex)
                {
                    LogExceptionMessage(ex);
                }
            }
        }

        /// <summary>
        /// 일정기간 Log 삭제
        /// 파일 생성 날짜가 아니라 파일 이름 날짜로 삭제 후 폴더 삭제
        /// </summary>
        /// <param name="searchDateTime"></param>
        private void PastLogDelete(DateTime searchDateTime)
        {
            //Log 삭제
            try
            {
                string Log_Directory = @"\Log\ACS\JobScheduler\";

                foreach (var subDirPath in Directory.GetDirectories(Log_Directory))
                {
                    DirectoryInfo dirInfo = new DirectoryInfo(subDirPath);
                    if (dirInfo.Exists)
                    {
                        DateTime Infodate = DateTime.ParseExact(dirInfo.Name, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                        if (Infodate < searchDateTime)
                        {
                            Directory.Delete(subDirPath, true); //하위 디렉토리와 파일까지 삭제
                            EventLogger.Info("deleteSystemLogFile_Time()");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogExceptionMessage(ex);
            }
        }

        /// <summary>
        ///DB 일정기간 경과된 data 삭제
        /// </summary>
        private void PastDataDelete(DateTime searchDateTime)
        {
            try
            {
                _repository.JobFinishedHistorys.PastDataDelete(searchDateTime);
                _repository.JobHistorys.PastDataDelete(searchDateTime);
                _repository.MissionFinishedHistorys.PastDataDelete(searchDateTime);
                _repository.MissionHistorys.PastDataDelete(searchDateTime);
                _repository.OrderFinishedHistorys.PastDataDelete(searchDateTime);
                _repository.OrderHistorys.PastDataDelete(searchDateTime);
                EventLogger.Info("deleteSystemPastData_Time()");
            }
            catch (Exception ex)
            {
                LogExceptionMessage(ex);
            }
        }

        public void LogExceptionMessage(Exception ex)
        {
            //string message = ex.InnerException?.Message ?? ex.Message;
            //string message = ex.ToString();
            string message = ex.GetFullMessage() + Environment.NewLine + ex.StackTrace;
            Debug.WriteLine(message);
            EventLogger.Info(message);
        }
    }
}