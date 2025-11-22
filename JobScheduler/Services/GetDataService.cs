using Common.DTOs.Rests.Carriers;
using Common.DTOs.Rests.Maps;
using Common.DTOs.Rests.Positions;
using Common.DTOs.Rests.Workers;
using Common.Models;
using Common.Models.Bases;
using Common.Models.Jobs;
using Data.Interfaces;
using Data.Repositorys.Bases;
using JOB.Mappings.Interfaces;
using log4net;
using RestApi.Interfases;
using System.Data;
using System.Diagnostics;

namespace JobScheduler.Services
{
    public class GetDataService
    {
        private static readonly ILog ApiLogger = LogManager.GetLogger("ApiEvent");

        public readonly IUnitOfWorkRepository _repository;
        public readonly IUnitOfWorkMapping _mapping;
        public readonly ILog _eventlog;
        public List<MqttTopicSubscribe> mqttTopicSubscribes = new List<MqttTopicSubscribe>();

        public GetDataService(ILog eventLog, IUnitOfWorkRepository repository, IUnitOfWorkMapping mapping)
        {
            _repository = repository;
            _mapping = mapping;
            _eventlog = eventLog;
        }

        public async Task<bool> StartAsyc()
        {
            bool Complete = false;
            bool Resource = false;
            bool Template = false;

            bool GetWorkerData = false;
            bool ResourceData = false;
            while (!Complete)
            {
                try
                {
                    ApiClient();
                    foreach (var serviceApi in _repository.ServiceApis.GetAll())
                    {
                        if (serviceApi.type == "Resource")
                        {
                            _repository.Workers.Delete();
                            _repository.Maps.Delete();
                            _repository.Positions.Delete();
                            var Workers = await serviceApi.Api.GetResourceWorker();
                            var Maps = await serviceApi.Api.GetResourceMap();
                            var Positions = await serviceApi.Api.GetResourcePosition();
                            //var Carriers = await serviceApi.Api.GetResourceCarrier();

                            if (Workers == null)
                            {
                                _eventlog.Info($"{nameof(Workers)}GetDataFail");
                                break;
                            }
                            else if (Maps == null)
                            {
                                _eventlog.Info($"{nameof(Maps)}GetDataFail");
                                break;
                            }
                            else if (Positions == null)
                            {
                                _eventlog.Info($"{nameof(Positions)}GetDataFail");
                                break;
                            }
                            //else if (Carriers == null)
                            //{
                            //    _eventlog.Info($"{nameof(Carriers)}GetDataFail");
                            //    break;
                            //}
                            else
                            {
                                foreach (var getmap in Maps)
                                {
                                    var map = _mapping.Maps.ApiGetResourceResponse(getmap);
                                    _repository.Maps.Add(map);
                                }

                                foreach (var getworker in Workers)
                                {
                                    var worker = _mapping.Workers.ApiGetResourceResponse(getworker);
                                    if (getworker.Middleware != null)
                                    {
                                        var middleware = _mapping.Middlewares.ApiGetResourceResponse(worker.id, getworker.Middleware);
                                        _repository.Middlewares.Add(middleware);
                                        worker.isMiddleware = true;
                                    }
                                    _repository.Workers.Add(worker);
                                }

                                foreach (var getPosition in Positions)
                                {
                                    var position = _mapping.Positions.ApiGetResourceResponse(getPosition);
                                    _repository.Positions.Add(position);
                                }

                                //foreach (var getCarrier in Carriers)
                                //{
                                //    var carrier = _mapping.Carriers.ApiGetResourceResponse(getCarrier);
                                //    _repository.Carriers.Add(carrier);
                                //}
                                Resource = true;
                            }
                        }
                        if (serviceApi.type == "Template")
                        {
                            _repository.JobTemplates.Delete();
                            var JobTemplates = await serviceApi.Api.AmkorGetResourceJobTemplate();
                            if (JobTemplates == null)
                            {
                                _eventlog.Info($"{nameof(JobTemplates)}GetDataFail");
                                break;
                            }
                            else
                            {
                                foreach (var getJobTemplate in JobTemplates)
                                {
                                    var JobTemplate = _mapping.JobTemplates.ApiGetResourceResponse(getJobTemplate);
                                    _repository.JobTemplates.Add(JobTemplate);
                                }
                                Template = true;
                            }
                        }
                    }
                    if (Resource && Template)
                    {
                        Complete = true;
                        ConfigData.SubscribeTopics = suscreibeTopicsAdd();
                        _eventlog.Info($"GetData{nameof(Complete)}");
                    }
                    await Task.Delay(500);
                }
                catch (Exception ex)
                {
                    LogExceptionMessage(ex);
                    await Task.Delay(500);
                }
            }

            return Complete;
        }

        private List<MqttTopicSubscribe> suscreibeTopicsAdd()
        {
            mqttTopicSubscribes.Clear();
            var workers = _repository.Workers.GetAll();
            foreach (var worker in workers)
            {
                var statetopic = new MqttTopicSubscribe
                {
                    topic = $"acs/worker/{worker.id}/state"
                };
                var missiontopic = new MqttTopicSubscribe
                {
                    topic = $"acs/worker/{worker.id}/mission"
                };
                mqttTopicSubscribes.Add(statetopic);
                mqttTopicSubscribes.Add(missiontopic);

                if (worker.isMiddleware)
                {
                    var middlewarestatetopic = new MqttTopicSubscribe
                    {
                        topic = $"acs/middleware/{worker.id}/state"
                    };
                    var middlewaremissiontopic = new MqttTopicSubscribe
                    {
                        topic = $"acs/middleware/{worker.id}/mission"
                    };
                    mqttTopicSubscribes.Add(middlewarestatetopic);
                    mqttTopicSubscribes.Add(middlewaremissiontopic);
                }
            }

            var elevatorMissionTopic = new MqttTopicSubscribe
            {
                topic = "acs/elevator/service/mission"
            };
            mqttTopicSubscribes.Add(elevatorMissionTopic);

            return mqttTopicSubscribes;
        }

        public async Task<bool> ReloadAsyc()
        {
            bool Complete = false;
            bool Resource = false;
            bool Template = false;

            bool GetWorkerData = false;
            bool ResourceData = false;
            while (!Complete)
            {
                try
                {
                    ApiClient();
                    foreach (var serviceApi in _repository.ServiceApis.GetAll())
                    {
                        if (serviceApi.type == "Resource")
                        {
                            var getReloadWorkers = await serviceApi.Api.GetResourceWorker();
                            var getReloadMaps = await serviceApi.Api.GetResourceMap();
                            var getReloadPositions = await serviceApi.Api.GetResourcePosition();
                            //var getReloadCarrier = await serviceApi.Api.GetResourceCarrier();
                            if (getReloadWorkers == null)
                            {
                                _eventlog.Info($"{nameof(getReloadWorkers)}GetDataFail");
                                break;
                            }
                            else if (getReloadMaps == null)
                            {
                                _eventlog.Info($"{nameof(getReloadMaps)}GetDataFail");
                                break;
                            }
                            else if (getReloadPositions == null)
                            {
                                _eventlog.Info($"{nameof(getReloadPositions)}GetDataFail");
                                break;
                            }
                            //else if (getReloadCarrier == null)
                            //{
                            //    _eventlog.Info($"{nameof(getReloadCarrier)}GetDataFail");
                            //    break;
                            //}
                            else
                            {
                                ReloadMap(getReloadMaps);
                                ReloadWorker(getReloadWorkers);
                                ReloadPosition(getReloadPositions);
                                //ReloadCarrier(getReloadCarrier);
                                Resource = true;
                            }
                        }
                        if (serviceApi.type == "Template")
                        {
                            var JobTemplates = await serviceApi.Api.AmkorGetResourceJobTemplate();
                            if (JobTemplates == null)
                            {
                                _eventlog.Info($"{nameof(JobTemplates)}GetDataFail");
                                break;
                            }
                            else
                            {
                                //JobTemplate경우 전부 삭제후 다시 가지고와도됨!
                                _repository.JobTemplates.Delete();
                                foreach (var getJobTemplate in JobTemplates)
                                {
                                    var JobTemplate = _mapping.JobTemplates.ApiGetResourceResponse(getJobTemplate);
                                    _repository.JobTemplates.Add(JobTemplate);
                                }
                                Template = true;
                            }
                        }
                    }
                    if (Resource && Template)
                    {
                        Complete = true;
                        ConfigData.SubscribeTopics = mqttTopicSubscribes;
                        _eventlog.Info($"GetData{nameof(Complete)}");
                    }
                    await Task.Delay(500);
                }
                catch (Exception ex)
                {
                    LogExceptionMessage(ex);
                    await Task.Delay(500);
                }
            }

            return Complete;
        }

        private void ReloadCarrier(List<Response_CarrierDto> dtoResourceCarriers)
        {
            List<Carrier> Reload = new List<Carrier>();
            //update Add
            foreach (var dtoResourceCarrier in dtoResourceCarriers)
            {
                Reload.Add(_mapping.Carriers.ApiGetResourceResponse(dtoResourceCarrier));
            }

            var ReloadId = Reload.Select(x => x.carrierId).ToList();
            var carriers = _repository.Carriers.GetAll();
            var carrierIds = carriers.Select(x => x.carrierId);

            //새로운 데이터 기준 으로 기존데이터가 없는것
            var AddCarriers = Reload.Where(x => !ReloadId.Contains(x.carrierId)).ToList();

            foreach (var AddCarrier in AddCarriers)
            {
                _repository.Carriers.Add(AddCarrier);
            }

            //기존데이터 기준 새로운 데이터와 같은것 업데이트
            foreach (var carrier in carriers)
            {
                var reloadCarrier = Reload.FirstOrDefault(x => x.carrierId == carrier.carrierId);
                if (reloadCarrier != null)
                {
                    carrier.carrierId = reloadCarrier.carrierId;
                    carrier.name = reloadCarrier.name;
                    carrier.location = reloadCarrier.location;
                    carrier.installedTime = reloadCarrier.installedTime;
                    carrier.workerId = reloadCarrier.workerId;
                }
            }

            //기존데이터 기준 에서 새로운데이터가 없는것
            var removedateCarriers = carriers.Where(x => !ReloadId.Contains(x.carrierId)).ToList();
            foreach (var removedateCarrier in removedateCarriers)
            {
                _repository.Carriers.Remove(removedateCarrier);
            }
        }

        private void ReloadMap(List<Response_MapDto> dtoResourceMaps)
        {
            List<Map> Reload = new List<Map>();
            //update Add
            foreach (var dtoResourceMap in dtoResourceMaps)
            {
                Reload.Add(_mapping.Maps.ApiGetResourceResponse(dtoResourceMap));
            }

            var ReloadId = Reload.Select(x => x.id).ToList();
            var maps = _repository.Maps.GetAll();
            var mapIds = maps.Select(x => x.id);

            //새로운 데이터 기준 으로 기존데이터가 없는것
            var AddMaps = Reload.Where(x => !ReloadId.Contains(x.id)).ToList();

            foreach (var AddMap in AddMaps)
            {
                _repository.Maps.Add(AddMap);
            }

            //기존데이터 기준 새로운 데이터와 같은것 업데이트
            foreach (var map in maps)
            {
                var reloadmap = Reload.FirstOrDefault(x => x.id == map.id);
                if (reloadmap != null)
                {
                    map.mapId = reloadmap.mapId;
                    map.source = reloadmap.source;
                    map.level = reloadmap.level;
                    map.name = reloadmap.name;
                }
            }

            //기존데이터 기준 에서 새로운데이터가 없는것
            var removedateMaps = maps.Where(x => !ReloadId.Contains(x.id)).ToList();
            foreach (var removedateMap in removedateMaps)
            {
                _repository.Maps.Remove(removedateMap);
            }
        }

        private void ReloadWorker(List<Response_Worker> dtoResourceWorkers)
        {
            List<Worker> Reload = new List<Worker>();
            List<Middleware> ReloadMiddlewares = new List<Middleware>();

            foreach (var dtoResourceWorker in dtoResourceWorkers)
            {
                var ReloadWorker = _mapping.Workers.ApiGetResourceResponse(dtoResourceWorker);
                if (dtoResourceWorker.Middleware != null)
                {
                    ReloadMiddlewares.Add(_mapping.Middlewares.ApiGetResourceResponse(dtoResourceWorker._id, dtoResourceWorker.Middleware));
                    ReloadWorker.isMiddleware = true;
                }
                Reload.Add(ReloadWorker);
            }

            var ReloadId = Reload.Select(x => x.id);
            var workers = _repository.Workers.GetAll();
            var workerIds = workers.Select(x => x.id);

            var ReloadmiddlewareId = ReloadMiddlewares.Select(x => x.id);
            var middlewares = _repository.Middlewares.GetAll();
            var middlewareIds = middlewares.Select(x => x.id);

            //새로운 데이터 기준 으로 기존데이터가 없는것
            var AddWorkers = Reload.Where(x => !workerIds.Contains(x.id)).ToList();
            var AddMiddlewares = ReloadMiddlewares.Where(x => !middlewareIds.Contains(x.id)).ToList();

            foreach (var AddWorker in AddWorkers)
            {
                _repository.Workers.Add(AddWorker);
                var statetopic = new MqttTopicSubscribe
                {
                    topic = $"acs/worker/{AddWorker.id}/state"
                };
                var missiontopic = new MqttTopicSubscribe
                {
                    topic = $"acs/worker/{AddWorker.id}/mission"
                };
                mqttTopicSubscribes.Add(statetopic);
                mqttTopicSubscribes.Add(missiontopic);
            }
            foreach (var AddMiddleware in AddMiddlewares)
            {
                _repository.Middlewares.Add(AddMiddleware);
                var middlewarestatetopic = new MqttTopicSubscribe
                {
                    topic = $"acs/middleware/{AddMiddleware.id}/state"
                };
                var middlewaremissiontopic = new MqttTopicSubscribe
                {
                    topic = $"acs/middleware/{AddMiddleware.id}/mission"
                };
                mqttTopicSubscribes.Add(middlewarestatetopic);
                mqttTopicSubscribes.Add(middlewaremissiontopic);
            }
            foreach (var worker in workers)
            {
                var Update = Reload.FirstOrDefault(x => x.id == worker.id);
                if (Update != null)
                {
                    worker.id = Update.id;
                    worker.source = Update.source;
                    worker.name = Update.name;
                }
            }

            foreach (var middleware in middlewares)
            {
                var Update = ReloadMiddlewares.FirstOrDefault(x => x.id == middleware.id);
                if (Update != null)
                {
                    middleware.id = Update.id;
                    middleware.ip = Update.ip;
                    middleware.port = Update.port;
                }
            }

            //기존데이터 기준 에서 새로운데이터가 없는것
            var RemoveWorkers = workers.Where(x => !ReloadId.Contains(x.id)).ToList();
            var RemoveMiddlewares = middlewares.Where(x => !ReloadmiddlewareId.Contains(x.id)).ToList();

            foreach (var RemoveWorker in RemoveWorkers)
            {
                _repository.Workers.Remove(RemoveWorker);
                var statetopic = new MqttTopicSubscribe
                {
                    topic = $"acs/worker/{RemoveWorker.id}/state"
                };
                var missiontopic = new MqttTopicSubscribe
                {
                    topic = $"acs/worker/{RemoveWorker.id}/mission"
                };
                mqttTopicSubscribes.Remove(statetopic);
                mqttTopicSubscribes.Remove(missiontopic);
            }
            foreach (var RemoveMiddleware in RemoveMiddlewares)
            {
                _repository.Middlewares.Remove(RemoveMiddleware);
                var middlewarestatetopic = new MqttTopicSubscribe
                {
                    topic = $"acs/middleware/{RemoveMiddleware.id}/state"
                };
                var middlewaremissiontopic = new MqttTopicSubscribe
                {
                    topic = $"acs/middleware/{RemoveMiddleware.id}/mission"
                };
                mqttTopicSubscribes.Remove(middlewarestatetopic);
                mqttTopicSubscribes.Remove(middlewaremissiontopic);
            }
        }

        private void ReloadPosition(List<Response_Position> dtoResourcePositions)
        {
            List<Position> Reload = new List<Position>();
            //update Add
            foreach (var dtoResourcePosition in dtoResourcePositions)
            {
                Reload.Add(_mapping.Positions.ApiGetResourceResponse(dtoResourcePosition));
            }

            var ReloadId = Reload.Select(x => x.id);
            var positions = _repository.Positions.MiR_GetAll();
            var positionIds = positions.Select(x => x.id);

            //새로운 데이터 기준 으로 기존데이터가 없는것
            var AddPositions = Reload.Where(x => !positionIds.Contains(x.id)).ToList();

            foreach (var AddPosition in AddPositions)
            {
                _repository.Positions.Add(AddPosition);
            }

            //기존데이터 기준 새로운 데이터와 같은것 업데이트
            foreach (var position in positions)
            {
                var Update = Reload.FirstOrDefault(x => x.id == position.id);
                if (Update != null)
                {
                    position.source = Update.source;
                    position.group = Update.group;
                    position.type = Update.type;
                    position.subType = Update.subType;
                    position.mapId = Update.mapId;
                    position.name = Update.name;
                    position.x = Update.x;
                    position.y = Update.y;
                    position.theth = Update.theth;
                    position.isDisplayed = Update.isDisplayed;
                    position.isEnabled = Update.isEnabled;
                    position.linkedFacility = Update.linkedFacility;
                    position.linkedRobotId = Update.linkedRobotId;
                    position.hasCharger = Update.hasCharger;
                }
            }

            //기존데이터 기준 에서 새로운데이터가 없는것
            var removes = positions.Where(x => !ReloadId.Contains(x.id)).ToList();
            foreach (var remove in removes)
            {
                _repository.Positions.Remove(remove);
            }
        }

        private void ApiClient()
        {
            //Config 파일을 불러온다
            foreach (var apiInfo in ConfigData.ServiceApis)
            {
                var serviceInfo = _repository.ServiceApis.GetByIpPort(apiInfo.ip, apiInfo.port);
                if (serviceInfo == null)
                {
                    var client = new Api(apiInfo.type, apiInfo.ip, apiInfo.port, double.Parse(apiInfo.timeOut), apiInfo.connectId, apiInfo.connectPassword);
                    apiInfo.Api = client;
                    _repository.ServiceApis.Add(apiInfo);
                }
            }
        }

        public void LogExceptionMessage(Exception ex)
        {
            //string message = ex.InnerException?.Message ?? ex.Message;
            //string message = ex.ToString();
            string message = ex.GetFullMessage() + Environment.NewLine + ex.StackTrace;
            Debug.WriteLine(message);
            _eventlog.Info(message);
        }
    }
}