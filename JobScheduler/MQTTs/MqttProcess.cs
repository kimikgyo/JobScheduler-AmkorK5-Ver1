using Common.Models;
using Common.Models.Jobs;
using Common.Models.Queues;
using Data.Interfaces;
using JOB.Mappings.Interfaces;
using JOB.MQTTs.Interfaces;
using log4net;
using System.Diagnostics;

namespace JOB.MQTTs
{
    public partial class MqttProcess
    {
        private static readonly ILog EventLogger = LogManager.GetLogger("Event");
        private readonly IMqttWorker _mqttWorker;
        private readonly IUnitOfWorkRepository _repository;
        private readonly IUnitOfWorkMapping _mapping;
        private readonly UnitofWorkMqttQueue _mqttQueue;

        public MqttProcess(UnitofWorkMqttQueue mqttQueue, IMqttWorker mqttWorker, IUnitOfWorkRepository repository, IUnitOfWorkMapping mapping)
        {
            _mqttQueue = mqttQueue;
            _mqttWorker = mqttWorker;
            _repository = repository;
            _mapping = mapping;
        }

        public void HandleReceivedMqttMessage()
        {
            while (QueueStorage.MqttTryDequeueSubscribe(out MqttSubscribeMessageDto message))
            {
                try
                {
                    //Console.WriteLine(string.Format("Process Message: [{0}] {1} at {2:yyyy-MM-dd HH:mm:ss,fff}", message.topic, message.Payload, message.Timestamp));

                    if (string.IsNullOrWhiteSpace(message.topic)) return;
                    if (string.IsNullOrWhiteSpace(message.Payload)) return;     // 페이로드 null check
                    if (!message.Payload.IsValidJson()) return;                 // 페이로드 json check
                    string[] topic = message.topic.Split('/');
                    message.type = topic[1];
                    message.id = topic[2];
                    message.subType = topic[3];

                    _mqttQueue.MqttSubscribe(message);
                }
                catch (Exception ex)
                {
                    LogExceptionMessage(ex);
                }
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

        public void worker_MissionId_MissionName_Update(Mission mission, bool flag)
        {
            var worker = _repository.Workers.GetById(mission.assignedWorkerId);
            if (worker != null)
            {
                if (flag == true)
                {
                    worker.missionId = mission.guid;
                    worker.missionName = mission.name;
                }
                else
                {
                    worker.missionId = "";
                    worker.missionName = "";
                }
                _repository.Workers.Update(worker);
            }
        }
        private readonly object _MissionStatusLock = new object();

        public void updateStateMission(Mission mission, string state, string message, bool historyAdd = false)
        {
            lock (_MissionStatusLock)
            {
                bool worekerMissionIdUpdateFlag = true;
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
                            worekerMissionIdUpdateFlag = false;
                            break;
                    }

                    _repository.Missions.Update(mission, message);
                    if (historyAdd) _repository.MissionHistorys.Add(mission);
                    _mqttQueue.MqttPublishMessage(TopicType.mission, mission.assignedWorkerId, _mapping.Missions.Publish(mission));
                    worker_MissionId_MissionName_Update(mission, worekerMissionIdUpdateFlag);
                }
            }
        }
    }
}