using Data.Interfaces;
using JOB.Mappings.Interfaces;
using JOB.MQTTs.Interfaces;
using JobScheduler.Services;
using log4net;
using System.Threading.Tasks;

namespace JOB.JobQueues.Process
{
    public partial class QueueProcess
    {
        private static readonly ILog EventLogger = LogManager.GetLogger("Event");

        private readonly IUnitOfWorkRepository _repository;
        private readonly IUnitofWorkMqttQueue _mqttQueue;
        private readonly IUnitOfWorkMapping _mapping;
        private MainService main = null;

        //    현재 실행 중인 작업들을 추적하기 위한 리스트
        //    - Stop/재시작 시 Task가 겹치지 않게 관리하고
        //    - 종료 대기(WhenAll) 및 예외 추적에 사용
        private List<Task> _tasks = new();

        //     스케줄러의 실행 여부 플래그
        //    - 무한루프 탈출 조건으로 사용 (while(_running))
        //    - Start/Stop 간 레이스를 줄이려면 bool 대신 volatile 추천
        private bool _running;

        public QueueProcess(MainService mainService, IUnitOfWorkRepository repository, IUnitofWorkMqttQueue mqttQueue, IUnitOfWorkMapping mapping)
        {
            _repository = repository;
            _mqttQueue = mqttQueue;
            _mapping = mapping;
            main = mainService;
        }

        /// <summary>
        /// 스케줄러의 모든 무한루프 작업을 시작합니다.
        /// </summary>
        public void Start()
        {
            // [중복 실행 방지]
            // 이미 실행 중이면 다시 시작하지 않도록 가드.
            // - 중복 Start는 같은 루프가 2개 이상 떠서 상태가 꼬일 수 있음.
            if (_running) return;

            // [실행 플래그 on]
            // - 아래 Task들이 while(_running) 조건을 보고 동작하므로
            //   Start 전에 반드시 true 로 세팅해야 함.
            _running = true;

            // [Task 컨테이너 초기화]
            // - 이전 실행 기록이 남아있지 않도록 매번 새 리스트로 준비.
            _tasks = new List<Task>
             {
                Task.Run(() => Queue_JobProcess()),
                Task.Run(() => Queue_OrderProcess()),
                Task.Run(() => Queue_MissionProcess()),
            };
        }

        /// <summary>
        /// Stop 요청 후 모든 Task가 종료될 때까지 대기
        /// </summary>
        public async Task StopAsync()
        {
            if (!_running) return;

            _running = false;  // 루프 종료 신호

            // [실제 종료 대기]
            if (_tasks.Count > 0)
            {
                try
                {
                    await Task.WhenAll(_tasks);  // 모든 Task 종료 대기
                    EventLogger.Info($"[StopAsync] Queue_Job Or Order Or Mission Task Stop");
                }
                catch (Exception ex)
                {
                    // Task 내부 예외 로깅
                    EventLogger.Info($"[StopAsync] Queue_Job Or Order Or Mission Task Stop Error : {ex.Message}");
                }
            }

            _tasks.Clear();
        }

        private async Task Queue_JobProcess()
        {
            try
            {
                EventLogger.Info("[Queue_JobProcess Task] Start");  // 루프 시작 로그

                while (_running)
                {
                    try
                    {
                        Job();
                        await Task.Delay(100);
                    }
                    catch (Exception ex)
                    {
                        main.LogExceptionMessage(ex);
                    }
                }
            }
            finally
            {
                EventLogger.Info("[Queue_JobProcess Task] Stop");  // 루프 종료 로그
            }
        }

        private async Task Queue_OrderProcess()
        {
            try
            {
                EventLogger.Info("[Queue_OrderProcess Task] Start");  // 루프 시작 로그

                while (_running)
                {
                    try
                    {
                        Order();
                        await Task.Delay(100);
                    }
                    catch (Exception ex)
                    {
                        main.LogExceptionMessage(ex);
                    }
                }
            }
            finally
            {
                EventLogger.Info("[Queue_OrderProcess Task] Stop");  // 루프 종료 로그
            }
        }

        private async Task Queue_MissionProcess()
        {
            try
            {
                EventLogger.Info("[Queue_MissionProcess Task] Start");  // 루프 시작 로그

                while (_running)
                {
                    try
                    {
                        Mission();
                        await Task.Delay(100);
                    }
                    catch (Exception ex)
                    {
                        main.LogExceptionMessage(ex);
                    }
                }
            }
            finally
            {
                EventLogger.Info("[Queue_MissionProcess Task] Stop");  // 루프 종료 로그
            }
        }

        private void Order()
        {
            Crate_Order();
            Remove_Order_Job_Mission();
        }

        private void Job()
        {
            Crate_Job();
            Remove_Job_Mission();
        }

        private void Mission()
        {
            Create_Mission();
        }
    }
}