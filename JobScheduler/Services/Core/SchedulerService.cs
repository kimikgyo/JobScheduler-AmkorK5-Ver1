using Common.Models;
using Common.Models.Jobs;
using Data.Interfaces;
using JOB.JobQueues.Interfaces;
using JOB.Mappings.Interfaces;
using JOB.MQTTs.Interfaces;
using JobScheduler.Services;
using log4net;

namespace JOB.Services
{
    public partial class SchedulerService
    {
        private static readonly ILog EventLogger = LogManager.GetLogger("Event");

        private static readonly ILog TestLogger = LogManager.GetLogger("Test");

        public readonly IUnitOfWorkRepository _repository;
        public readonly IUnitOfWorkJobMissionQueue _Queue;
        public readonly IUnitOfWorkMapping _mapping;
        public readonly IUnitofWorkMqttQueue _mqttQueue;
        private readonly object _positionLock = new object();

        private MainService main = null;

        //    현재 실행 중인 작업들을 추적하기 위한 리스트
        //    - Stop/재시작 시 Task가 겹치지 않게 관리하고
        //    - 종료 대기(WhenAll) 및 예외 추적에 사용
        private List<Task> _tasks = new();

        //     스케줄러의 실행 여부 플래그
        //    - 무한루프 탈출 조건으로 사용 (while(_running))
        //    - Start/Stop 간 레이스를 줄이려면 bool 대신 volatile 추천
        private bool _running;

        public SchedulerService(MainService mainService, IUnitOfWorkRepository repository, IUnitOfWorkJobMissionQueue queue
                                , IUnitOfWorkMapping mapping, IUnitofWorkMqttQueue mqttQueue)
        {
            main = mainService;
            _repository = repository;
            _Queue = queue;
            _mapping = mapping;
            _mqttQueue = mqttQueue;
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
                Task.Run(() => Monitor()),
                Task.Run(() => JobScheduler()),
            };
        }

        private async Task Monitor()
        {
            try
            {
                EventLogger.Info("[Monitor Task] Start");  // 루프 시작 로그

                while (_running)
                {
                    try
                    {
                        StatusChangeControl();
                        CarrierControl();
                        PositionControl();
                        await Task.Delay(300);
                    }
                    catch (Exception ex)
                    {
                        main.LogExceptionMessage(ex);
                    }
                }
            }
            finally
            {
                EventLogger.Info("[Monitor Task] Stop");  // 루프 종료 로그
            }
        }

        public async Task JobScheduler()
        {
            try
            {
                EventLogger.Info("[JobScheduler Task] Start");  // 루프 시작 로그

                while (_running)
                {
                    try
                    {
                        JobPlanner();
                        WorkerAssined();
                        Dispatcher();
                        cancelControl();

                        await Task.Delay(300);
                    }
                    catch (Exception ex)
                    {
                        main.LogExceptionMessage(ex);
                    }
                }
            }
            finally
            {
                EventLogger.Info("[JobScheduler Task] Stop");  // 루프 정지 로그
            }
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
                    EventLogger.Info($"[StopAsync] Scheduler Task Stop");
                }
                catch (Exception ex)
                {
                    // Task 내부 예외 로깅
                    EventLogger.Info($"[StopAsync] Scheduler Task Stop Error : {ex.Message}");
                }
            }

            _tasks.Clear();
        }

        public void updateStateOrder(Order order, OrderState state, bool historyAdd = false)
        {
            if (order.state != state.ToString())
            {
                order.state = state.ToString();
                order.stateCode = state;
                order.updatedAt = DateTime.Now;

                _repository.Orders.Update(order);
                if (historyAdd) _repository.OrderHistorys.Add(order);
                _mqttQueue.MqttPublishMessage(TopicType.order, TopicSubType.status, _mapping.Orders.Publish(order));
            }
        }

        public void updateStateJob(Job job, string state, string terminateState, string terminationType, string terminator, bool historyAdd = false)
        {
            if (job.state != state)
            {
                job.state = state;
                switch (job.state)
                {
                    case nameof(JobState.INIT):
                    case nameof(JobState.WORKERASSIGNED):
                    case nameof(JobState.INPROGRESS):
                        job.updatedAt = DateTime.Now;

                        break;

                    case nameof(JobState.CANCELCOMPLETED):
                    case nameof(JobState.ABORTCOMPLETED):
                    case nameof(JobState.COMPLETED):
                        break;
                }
                _repository.Jobs.Update(job);
                if (historyAdd) _repository.JobHistorys.Add(job);
                _mqttQueue.MqttPublishMessage(TopicType.job, TopicSubType.status, _mapping.Jobs.Publish(job));
            }
            if (job.terminateState != terminateState)
            {
                job.terminationType = terminationType;
                job.terminateState = terminateState;
                job.terminator = terminator;
                switch (terminateState)
                {
                    case nameof(TerminateState.INITED):
                        job.terminatedAt = DateTime.Now;
                        break;

                    case nameof(TerminateState.EXECUTING):
                        job.terminatingAt = DateTime.Now;
                        break;
                }

                _repository.Jobs.Update(job);
                if (historyAdd) _repository.JobHistorys.Add(job);
                _mqttQueue.MqttPublishMessage(TopicType.job, TopicSubType.status, _mapping.Jobs.Publish(job));
            }
        }

        public void updateStateMission(Mission mission, string state, bool historyAdd = false)
        {
            if (mission.state != state)
            {
                mission.state = state;

                switch (mission.state)
                {
                    case nameof(MissionState.INIT):
                    case nameof(MissionState.WORKERASSIGNED):
                    case nameof(MissionState.WAITING):
                    case nameof(MissionState.COMMANDREQUEST):
                    case nameof(MissionState.COMMANDREQUESTCOMPLETED):
                    case nameof(MissionState.PENDING):
                    case nameof(MissionState.EXECUTING):
                    case nameof(MissionState.FAILED):
                    case nameof(MissionState.ABORTINITED):
                    case nameof(MissionState.ABORTFAILED):
                    case nameof(MissionState.CANCELINITED):
                    case nameof(MissionState.CNACELFAILED):
                        mission.updatedAt = DateTime.Now;
                        break;

                    case nameof(MissionState.SKIPPED):
                    case nameof(MissionState.ABORTCOMPLETED):
                    case nameof(MissionState.CANCELINITCOMPLETED):
                    case nameof(MissionState.CANCELED):
                    case nameof(MissionState.COMPLETED):
                        mission.finishedAt = DateTime.Now;
                        break;
                }

                _repository.Missions.Update(mission);
                if (historyAdd) _repository.MissionHistorys.Add(mission);
                _mqttQueue.MqttPublishMessage(TopicType.mission, TopicSubType.status, _mapping.Missions.Publish(mission));
            }
        }

        public void updateOccupied(Position position, bool flag)
        {
            lock (_positionLock)
            {
                if (position.isOccupied != flag)
                {
                    position.isOccupied = flag;
                    _repository.Positions.update(position);
                }
            }
        }

        private bool IsInvalid(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                || value.ToUpper() == "NULL"
                || value.ToUpper() == "STRING";
        }
    }
}