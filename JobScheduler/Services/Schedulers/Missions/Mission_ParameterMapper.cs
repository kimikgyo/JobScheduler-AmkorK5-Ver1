using Common.Models.Jobs;
using System.Text.Json;

namespace JOB.Services
{
    public partial class SchedulerService
    {
        private bool workerElevatorParameterMapping(Mission mission)
        {
            bool completed = false;

            var workers = _repository.Workers.MiR_GetByActive();
            //현재 워커의 정보를 조회한다
            var assignedWorker = workers.FirstOrDefault(r => r.id == mission.assignedWorkerId);
            if (assignedWorker == null)
            {
                EventLogger.Warn($"[ELEV][MAP][FAIL] assignedWorker not found. mission={mission.guid}, workerId={mission.assignedWorkerId}, subType={mission.subType}");
                return completed;
            }

            //현재 워커와 다른 워커를 조회한다
            var anotherWorkers = workers.Where(r => r.id != assignedWorker.id).ToList();

            switch (mission.subType)
            {
                case nameof(MissionSubType.ELEVATORWAITMOVE):

                    //엘리베이터 대기위치 점유 상황을 판단하여 점유하고있지않은 포지션으로 전달한다.
                    var waitPositionNotOccupieds = _repository.Positions.MiR_GetBySubType(nameof(PositionSubType.ELEVATORWAIT));
                    if (waitPositionNotOccupieds == null || waitPositionNotOccupieds.Count == 0)
                    {
                        EventLogger.Warn($"[ELEV][WAIT][NO_CANDIDATE] all occupied or none. missionguId={mission.guid}, missionName={mission.name}, workerId={assignedWorker.id},WorkerName={assignedWorker.name}" +
                                         $", mapId={assignedWorker.mapId}");
                        break;
                    }

                    var resultWait = elevatorParameterMapping(waitPositionNotOccupieds, mission, assignedWorker);
                    completed = resultWait.completed;

                    if (!completed)
                    {
                        EventLogger.Warn($"[ELEV][WAIT][FAIL] mapping failed. missionguId={mission.guid}, missionName={mission.name}, workerId={assignedWorker.id},WorkerName={assignedWorker.name}" +
                                         $", mapId={assignedWorker.mapId}, candidates={waitPositionNotOccupieds.Count}");
                    }
                    break;

                case nameof(MissionSubType.ELEVATORENTERMOVE):

                    //점유 하고있는 포지션
                    var IsOccupieds = _repository.Positions.MiR_GetIsOccupied(null, nameof(PositionSubType.ELEVATORENTER));
                    //점유하고있지않은 포지션
                    var enterNotOccupied = _repository.Positions.MiR_GetNotOccupied(null, nameof(PositionSubType.ELEVATORENTER));
                    if (enterNotOccupied == null || enterNotOccupied.Count == 0)
                    {
                        EventLogger.Warn($"[ELEV][ENTER][NO_CANDIDATE] all occupied or none. missionguId={mission.guid}, missionName={mission.name}, workerId={assignedWorker.id},WorkerName={assignedWorker.name}" +
                                         $", mapId={assignedWorker.mapId}, occupied={IsOccupieds.Count}");
                        break;
                    }

                    int beforeFilter = enterNotOccupied.Count;

                    //점유 하고 있는 에서 점유하고있지않은 포지션 뒷문자 가 같은것을 Remove한다
                    foreach (var IsOccupied in IsOccupieds)
                    {
                        //inatech 내부적으로 Robot팀에서 포지션 이름을 뒤에서 3번째는 각층에 동일하게 맞추게끔 협의함.
                        string PositionName = IsOccupied.name.Replace(" ", "");
                        string FindName = IsOccupied.name.Substring(IsOccupied.name.Length - 3);
                        var removePositions = enterNotOccupied.Where(n => n.name.EndsWith(FindName)).ToList();

                        foreach (var removePosition in removePositions)
                        {
                            enterNotOccupied.Remove(removePosition);
                        }
                    }

                    int afterFilter = enterNotOccupied.Count;

                    if (afterFilter == 0)
                    {
                        EventLogger.Warn($"[ELEV][ENTER][FILTERED_TO_ZERO] all candidates removed by suffix rule. missionguId={mission.guid}, missionName={mission.name}, workerId={assignedWorker.id}" +
                            $",WorkerName={assignedWorker.name}, mapId={assignedWorker.mapId}, before={beforeFilter}, occupied={IsOccupieds.Count}");
                        break;
                    }
                    var elevatorparmeter = elevatorParameterMapping(enterNotOccupied, mission, assignedWorker);
                    completed = elevatorparmeter.completed;

                    if (!completed || elevatorparmeter.position == null)
                    {
                        EventLogger.Warn($"[ELEV][ENTER][FAIL] mapping failed. missionguId={mission.guid}, missionName={mission.name}, workerId={assignedWorker.id},WorkerName={assignedWorker.name}" +
                                         $", mapId={assignedWorker.mapId}, candidates={afterFilter}");
                        break;
                    }

                    completed = switchingMapParameterMapping(enterNotOccupied, mission, assignedWorker, elevatorparmeter.position);
                    if (!completed)
                    {
                        EventLogger.Warn($"[ELEV][ENTER][SWITCHMAP_FAIL] missionguId={mission.guid}, missionName={mission.name}, workerId={assignedWorker.id},WorkerName={assignedWorker.name}" +
                                        $", mapId={assignedWorker.mapId}, selectedPos={elevatorparmeter.position.id}");
                    }
                    break;

                case nameof(MissionSubType.ELEVATOREXITMOVE):
                    var exitNotOccupied = _repository.Positions.MiR_GetBySubType(nameof(PositionSubType.ELEVATOREXIT));
                    if (exitNotOccupied == null || exitNotOccupied.Count == 0)
                    {
                        EventLogger.Warn($"[ELEV][EXIT][NO_CANDIDATE] all occupied or none. missionguId={mission.guid}, missionName={mission.name}, workerId={assignedWorker.id},WorkerName={assignedWorker.name}" +
                                         $", mapId={assignedWorker.mapId}");
                        break;
                    }

                    var resultExit = elevatorParameterMapping(exitNotOccupied, mission, assignedWorker);
                    completed = resultExit.completed;

                    if (!completed)
                    {
                        EventLogger.Warn($"[ELEV][EXIT][FAIL] mapping failed. missionguId={mission.guid}, missionName={mission.name}, workerId={assignedWorker.id},WorkerName={assignedWorker.name}" +
                                         $", mapId={assignedWorker.mapId}, candidates={exitNotOccupied.Count}");
                    }
                    break;

                default:
                    completed = true;
                    break;
            }
            return completed;
        }

        private (bool completed, Position position) elevatorParameterMapping(List<Position> positions, Mission mission, Worker worker)
        {
            bool completed = false;

            // 후보군 내에서 worker.mapId에 맞는 포지션 선택
            var selected = positions.FirstOrDefault(r => r.mapId == worker.mapId);

            if (selected == null)
            {
                // 핵심: 후보는 있었는데 mapId가 안 맞아서 못 찾는 경우
                EventLogger.Warn($"[ELEV][MAP][NO_MATCH] no position for worker.mapId.  missionguId={mission.guid} subType={mission.subType}, missionName={mission.name}" +
                                 $", workerId={worker.id},WorkerName={worker.name}, mapId={worker.mapId}, candidates={positions.Count}");
                return (false, null);
            }

            // target 파라미터 세팅
            var param = mission.parameters.FirstOrDefault(r => r.key == "target");
            if (param == null)
            {
                EventLogger.Error($"[ELEV][PARAM][MISSING] target param missing.  missionguId={mission.guid} subType={mission.subType}, missionName={mission.name}" +
                                  $", workerId={worker.id},WorkerName={worker.name}, mapId={worker.mapId}");
                return (false, selected);
            }

            // target 값 갱신
            if (IsInvalid(param.value))
            {
                var old = param.value;
                param.value = selected.id;
                mission.parametersJson = JsonSerializer.Serialize(mission.parameters);
                _repository.Missions.Update(mission, "[elevatorParameterMapping]");

                //EventLogger.Info($"[ELEV][PARAM][SET] target updated. missionguId={mission.guid} subType={mission.subType}, missionName={mission.name}" +
                //                 $", workerId={worker.id},WorkerName={worker.name}, mapId={worker.mapId}, oldTarget={old}, newTarget={selected.id}");
            }

            completed = true;

            // 점유 업데이트(hold 포함)
            updateOccupied(selected, true, 0.5, "elevatorParameterMapping");

            //EventLogger.Info($"[ELEV][OCC][HOLD] occupied set.  missionguId={mission.guid} subType={mission.subType}, missionName={mission.name}" +
            //                 $", workerId={worker.id},WorkerName={worker.name}, mapId={worker.mapId}, posId={selected.id}, posName={selected.name}, holdSec=0.5");

            return (completed, selected);
        }

        private bool switchingMapParameterMapping(List<Position> positions, Mission mission, Worker worker, Position enterSelectPosition)
        {
            bool completed = false;

            Position sourcePosition = null;
            Position destPosition = null;
            Position mapSwitchPosition = null;

            //inatech 내부적으로 Robot팀에서 포지션 이름을 뒤에서 3번째는 각층에 동일하게 맞추게끔 협의함.
            string SwitchPositionName = enterSelectPosition.name.Replace(" ", "");
            string switchFindName = SwitchPositionName.Substring(SwitchPositionName.Length - 3);

            positions = positions.Where(r => r != null && r.mapId != worker.mapId).ToList();
            if (positions == null || positions.Count() == 0) return completed;

            //Map스위칭 포지션 적용
            var job = _repository.Jobs.GetByid(mission.jobId);
            if (job != null)
            {
                var missions = _repository.Missions.GetByJobId(job.guid);
                if (job.sourceId != null) sourcePosition = _repository.Positions.MiR_GetById(job.sourceId);

                //출발지 가 현재 Worker층과 다를경우
                var sourceMission = missions.Where(r => r.subType == nameof(MissionSubType.SOURCEMOVE) && r.state == nameof(MissionState.WAITING)).FirstOrDefault();
                if (sourceMission != null && sourcePosition != null)
                {
                    //다른층 같은 포지션으로 찾기 위해서는 이름 일치로 찾아야함
                    mapSwitchPosition = positions.FirstOrDefault(r => r.mapId == sourcePosition.mapId && r.name.EndsWith(switchFindName));
                }
                else
                {
                    //도착지층 과 다를경우
                    destPosition = _repository.Positions.MiR_GetById(job.destinationId);
                    var destMission = missions.Where(r => r.state == nameof(MissionState.WAITING)
                                                 && (r.subType == nameof(MissionSubType.DESTINATIONMOVE) || r.subType == nameof(MissionSubType.CHARGERMOVE))).FirstOrDefault();

                    if (destMission != null && destPosition != null)
                    {
                        //다른층 같은 포지션으로 찾기 위해서는 이름 일치로 찾아야함
                        mapSwitchPosition = positions.FirstOrDefault(r => r.mapId == destPosition.mapId && r.name.EndsWith(switchFindName));
                    }
                }

                if (mapSwitchPosition != null)
                {
                    var switchMapMission = missions.FirstOrDefault(r => r.subType == nameof(MissionSubType.SWITCHINGMAP) && r.state == nameof(MissionState.WAITING));
                    var mapSwitchParam = switchMapMission.parameters.FirstOrDefault(p => p.key == "target");
                    if (IsInvalid(mapSwitchParam.value))
                    {
                        mapSwitchParam.value = mapSwitchPosition.id;
                        switchMapMission.parametersJson = JsonSerializer.Serialize(switchMapMission.parameters);
                        _repository.Missions.Update(switchMapMission, "[switchingMapParameterMapping]");
                        //직접 파라메타를 변경하는것이기때문에 포지션점유를 업데이트한다
                    }
                    completed = true;
                    updateOccupied(mapSwitchPosition, true, 0.5, "MapSwitch");
                }
            }

            return completed;
        }
    }
}