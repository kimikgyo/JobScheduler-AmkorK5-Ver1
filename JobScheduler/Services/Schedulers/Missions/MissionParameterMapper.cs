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
            if (assignedWorker == null) return completed;

            //현재 워커와 다른 워커를 조회한다
            var anotherWorkers = workers.Where(r => r.id != assignedWorker.id).ToList();

            switch (mission.subType)
            {
                case nameof(MissionSubType.ELEVATORWAITMOVE):

                    //엘리베이터 대기위치 점유 상황을 판단하여 점유하고있지않은 포지션으로 전달한다.
                    var waitPositionNotOccupieds = _repository.Positions.MiR_GetNotOccupied(null, nameof(PositionSubType.ELEVATORWAIT));
                    if (waitPositionNotOccupieds == null || waitPositionNotOccupieds.Count == 0) break;

                    completed = elevatorParameterMapping(waitPositionNotOccupieds, mission, assignedWorker).completed;

                    break;

                case nameof(MissionSubType.ELEVATORENTERMOVE):

                    //점유 하고있는 포지션
                    var IsOccupieds = _repository.Positions.MiR_GetIsOccupied(null, nameof(PositionSubType.ELEVATORENTER));
                    //점유하고있지않은 포지션
                    var enterPositions = _repository.Positions.MiR_GetNotOccupied(null, nameof(PositionSubType.ELEVATORENTER));
                    if (enterPositions == null || enterPositions.Count == 0) break;

                    //점유 하고 있는 에서 점유하고있지않은 포지션 뒷문자 가 같은것을 Remove한다
                    foreach (var IsOccupied in IsOccupieds)
                    {
                        //inatech 내부적으로 Robot팀에서 포지션 이름을 뒤에서 3번째는 각층에 동일하게 맞추게끔 협의함.
                        string PositionName = IsOccupied.name.Replace(" ", "");
                        string FindName = IsOccupied.name.Substring(IsOccupied.name.Length - 3);
                        var removePositions = enterPositions.Where(n => n.name.EndsWith(FindName)).ToList();

                        foreach (var removePosition in removePositions)
                        {
                            enterPositions.Remove(removePosition);
                        }
                    }

                    var elevatorparmeter = elevatorParameterMapping(enterPositions, mission, assignedWorker);

                    completed = elevatorparmeter.completed;
                    if (completed && elevatorparmeter.position != null)
                    {
                        completed = switchingMapParameterMapping(enterPositions, mission, assignedWorker, elevatorparmeter.position);
                    }
                    break;

                case nameof(MissionSubType.ELEVATOREXITMOVE):
                    var elevatorExitpositions = _repository.Positions.MiR_GetNotOccupied(null, nameof(PositionSubType.ELEVATOREXIT));
                    if (elevatorExitpositions == null || elevatorExitpositions.Count == 0) break;

                    completed = elevatorParameterMapping(elevatorExitpositions, mission, assignedWorker).completed;

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
            var Position = positions.FirstOrDefault(r => r.mapId == worker.mapId);
            if (Position != null)
            {
                var param = mission.parameters.FirstOrDefault(r => r.key == "target");
                if (IsInvalid(param.value))
                {
                    param.value = Position.id;
                    mission.parametersJson = JsonSerializer.Serialize(mission.parameters);
                    _repository.Missions.Update(mission);
                }
                completed = true;
                updateOccupied(Position, true, 0.5);
            }

            return (completed, Position);
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
                        _repository.Missions.Update(switchMapMission);
                        //직접 파라메타를 변경하는것이기때문에 포지션점유를 업데이트한다
                    }
                    completed = true;
                    updateOccupied(mapSwitchPosition, true, 0.5);
                }
            }

            return completed;
        }
    }
}