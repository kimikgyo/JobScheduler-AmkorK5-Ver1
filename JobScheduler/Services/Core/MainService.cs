using Data.Interfaces;
using JOB.JobQueues.Interfaces;
using JOB.JobQueues.Process;
using JOB.Mappings.Interfaces;
using JOB.MQTTs.Interfaces;
using JOB.Services;
using log4net;
using System.Diagnostics;
using System.Globalization;

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
        private Response_Data_Service Response_Data = null;
        private MQTTService mQTT = null;
        private SchedulerService schedulerService = null;
        private QueueProcess queueProcess = null;

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
            Response_Data = new Response_Data_Service(EventLogger, _repository, _mapping);
            queueProcess = new QueueProcess(main, _repository, _mqttQueue, _mapping);
        }

        private async Task stratAsync()
        {
            Start();
            bool Response_DataComplete = await Response_Data.StartAsyc();
            if (Response_DataComplete)
            {
                mQTT.Start();
                schedulerService.Start();
                queueProcess.Start();
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
            bool getDataComplete = await Response_Data.ReloadAsyc();
            if (getDataComplete)
            {
                //// 3. MQTT 다시 시작 (필요시)
                //_mqtt.Start();

                // 4. 스케줄러 다시 시작
                queueProcess.Start();
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
        /// 생성된 로그 폴더 구조(날짜 폴더 → JobScheduler → 파일)에 맞추어
        /// 오래된 로그 디렉토리를 삭제하는 메소드.
        ///
        /// 로그 생성 구조 예:
        /// \Log\ACS\2025-11-27\JobScheduler\_ApiEvent.log
        /// </summary>
        private void PastLogDelete(DateTime searchDateTime)
        {
            try
            {
                // 1) 로그 루트 경로: \Log\ACS
                // log4net 설정의 <file value="\Log\ACS\" /> 와 동일한 기준 경로
                string logRoot = @"C:\Log\ACS";

                // 루트 폴더가 없다면 삭제할 것도 없으므로 종료
                if (!Directory.Exists(logRoot)) return;

                // 2) 날짜 폴더 목록 가져오기
                // 예: \Log\ACS\2025-11-27, \Log\ACS\2025-11-25 등
                foreach (var dateDir in Directory.GetDirectories(logRoot))
                {
                    DirectoryInfo dirInfo = new DirectoryInfo(dateDir);

                    // 3) 폴더명이 yyyy-MM-dd 형식인지 확인
                    // 올바른 날짜 폴더만 삭제 검사 대상으로 삼는다.
                    // 날짜 형식이 아니면 로그 폴더가 아니므로 스킵
                    if (!DateTime.TryParseExact(dirInfo.Name, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime folderDate)) continue;

                    // 4) 날짜 비교: searchDateTime 이전 날짜면 삭제 대상
                    if (folderDate < searchDateTime)
                    {
                        // 날짜 폴더 안의 JobScheduler 폴더 경로
                        // 예: \Log\ACS\2025-11-27\JobScheduler
                        string jobSchedulerPath = Path.Combine(dateDir, "JobScheduler");

                        // 5) JobScheduler 폴더가 있으면 그 하위 모든 파일/폴더 삭제
                        if (Directory.Exists(jobSchedulerPath))
                        {
                            // true = 하위 파일과 디렉토리 포함 전체 삭제
                            Directory.Delete(jobSchedulerPath, true);
                        }

                        // 6) 날짜 폴더가 비었으면 날짜 폴더도 삭제
                        // 로그 파일만 삭제하면 날짜 폴더가 빈 폴더로 남을 수 있으므로 정리 필요
                        bool isEmpty = Directory.GetFiles(dateDir).Length == 0 && Directory.GetDirectories(dateDir).Length == 0;

                        if (isEmpty)
                        {
                            Directory.Delete(dateDir, true);
                        }

                        // 7) 로그 출력 (삭제되었다는 기록)
                        EventLogger.Info($"deleteSystemLogFile_Time() : deleted {dirInfo.Name}");
                    }
                }
            }
            catch (Exception ex)
            {
                // 8) 예상치 못한 오류 기록
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
            EventLogger.Error(message);
        }
    }
}