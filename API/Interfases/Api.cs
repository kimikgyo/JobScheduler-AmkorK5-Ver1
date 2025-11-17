using Common.DTOs.Bases;
using Common.DTOs.Jobs;
using Common.Interfaces;
using Common.Models.Bases;
using Common.Models.Jobs;
using Common.Templates;
using log4net;
using Newtonsoft.Json;
using static ExceptionFilterUtility;

namespace RestApi.Interfases
{
    public class Api : IApi, IDisposable
    {
        private static readonly ILog ApiLogger = LogManager.GetLogger("ApiEvent");
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerSettings _settings;
        private readonly string _type;
        public Uri BaseAddress => _httpClient.BaseAddress;

        public Api(string type, string ip, string port, double timeout, string connectId, string connectPassword, JsonSerializerSettings settings = null)
        {
            _type = type;
            _httpClient = MakeHttpClient(ip, port, timeout, connectId, connectPassword);
            _settings = settings ?? new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, MissingMemberHandling = MissingMemberHandling.Ignore };
        }

        private HttpClient MakeHttpClient(string ip, string port, double timeout, string connectId, string connectPassword)
        {
            var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromMilliseconds(timeout);
            string uriString = $"http://{ip.Trim()}:{port.TrimEnd('/')}";
            httpClient.BaseAddress = new Uri(uriString);
            return httpClient;
        }

        public async Task<List<ApiGetResponseDtoResourceWorker>> GetResourceWorker()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<ApiGetResponseDtoResourceWorker>>("api/workers");
            }
            //catch (Exception ex) when (True(() => _logger.Error(ex)))
            catch (Exception ex) when (True(() => ApiLogger.Error($"IPAddress = {_httpClient.BaseAddress}" + "\r\n" + ex)))
            {
                return null;
            }
        }

        public async Task<List<ApiGetResponseDtoResourceMap>> GetResourceMap()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<ApiGetResponseDtoResourceMap>>("api/maps");
            }
            //catch (Exception ex) when (True(() => _logger.Error(ex)))
            catch (Exception ex) when (True(() => ApiLogger.Error($"IPAddress = {_httpClient.BaseAddress}" + "\r\n" + ex)))
            {
                return null;
            }
        }

        public async Task<List<ApiGetResponseDtoResourcePosition>> GetResourcePosition()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<ApiGetResponseDtoResourcePosition>>("api/positions");
            }
            //catch (Exception ex) when (True(() => _logger.Error(ex)))
            catch (Exception ex) when (True(() => ApiLogger.Error($"IPAddress = {_httpClient.BaseAddress}" + "\r\n" + ex)))
            {
                return null;
            }
        }

        public async Task<List<ApiGetResponseDtoResourceCarrier>> GetResourceCarrier()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<ApiGetResponseDtoResourceCarrier>>("api/carriers");
            }
            //catch (Exception ex) when (True(() => _logger.Error(ex)))
            catch (Exception ex) when (True(() => ApiLogger.Error($"IPAddress = {_httpClient.BaseAddress}" + "\r\n" + ex)))
            {
                return null;
            }
        }
        public async Task<List<ApiGetResponseDtoResourceJobTemplate>> STIGetResourceJobTemplate()
        {
            try
            {
                string jsonString = await _httpClient.GetStringAsync("api/JobTemplates/STI");
                return await _httpClient.GetFromJsonAsync<List<ApiGetResponseDtoResourceJobTemplate>>("api/JobTemplates/STI");
            }
            //catch (Exception ex) when (True(() => _logger.Error(ex)))
            catch (Exception ex) when (True(() => ApiLogger.Error($"IPAddress = {_httpClient.BaseAddress}" + "\r\n" + ex)))
            {
                return null;
            }
        }

        public async Task<List<ApiGetResponseDtoResourceJobTemplate>> AmkorGetResourceJobTemplate()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<ApiGetResponseDtoResourceJobTemplate>>("api/JobTemplates/Amkor");
            }
            //catch (Exception ex) when (True(() => _logger.Error(ex)))
            catch (Exception ex) when (True(() => ApiLogger.Error($"IPAddress = {_httpClient.BaseAddress}" + "\r\n" + ex)))
            {
                return null;
            }
        }

        public async Task<ApResponseDto> ElevatorPostMissionQueueAsync(object value)
        {
            if (!AcceptFilterUtility.WriteAccepted) { ApiLogger.Error($"IPAddress = {_httpClient.BaseAddress}" + "\r\n" + $"-- API NOT ALLOWED. [{nameof(ElevatorPostMissionQueueAsync)}] --"); return null; }

            try
            {
                //수정본
                var response = await _httpClient.PostAsJsonAsync("api/missions", value);
                var jsonResponse = await response.Content.ReadAsStringAsync();

                var missionQueueResponse = new ApResponseDto
                {
                    statusCode = Convert.ToInt32(response.StatusCode),
                    statusText = response.StatusCode.ToString(),
                    message = jsonResponse
                };
                return missionQueueResponse;

                //기존
                //var response = await _httpClient.PostAsJsonAsync("api/Workers/mission_queue", value);
                //var jsonResponse = await response.Content.ReadAsStringAsync();
                //return JsonConvert.DeserializeObject<ApiPostResponseDtoMissionQueue>(jsonResponse);
            }
            //catch (Exception ex) when (True(() => _logger.Error(ex)))
            catch (Exception ex) when (True(() => ApiLogger.Error($"IPAddress = {_httpClient.BaseAddress}" + "\r\n" + ex)))
            {
                return null;
            }
        }

        public async Task<ApResponseDto> WorkerPostMissionQueueAsync(object value)
        {
            if (!AcceptFilterUtility.WriteAccepted) { ApiLogger.Error($"IPAddress = {_httpClient.BaseAddress}" + "\r\n" + $"-- API NOT ALLOWED. [{nameof(WorkerPostMissionQueueAsync)}] --"); return null; }

            try
            {
                //수정본
                var response = await _httpClient.PostAsJsonAsync("missions/worker", value);
                var jsonResponse = await response.Content.ReadAsStringAsync();

                var missionQueueResponse = new ApResponseDto
                {
                    statusCode = Convert.ToInt32(response.StatusCode),
                    statusText = response.StatusCode.ToString(),
                    message = jsonResponse
                };
                return missionQueueResponse;

                //기존
                //var response = await _httpClient.PostAsJsonAsync("api/Workers/mission_queue", value);
                //var jsonResponse = await response.Content.ReadAsStringAsync();
                //return JsonConvert.DeserializeObject<ApiPostResponseDtoMissionQueue>(jsonResponse);
            }
            //catch (Exception ex) when (True(() => _logger.Error(ex)))
            catch (Exception ex) when (True(() => ApiLogger.Error($"IPAddress = {_httpClient.BaseAddress}" + "\r\n" + ex)))
            {
                return null;
            }
        }

        public async Task<ApResponseDto> MiddlewarePostMissionQueueAsync(object value)
        {
            if (!AcceptFilterUtility.WriteAccepted) { ApiLogger.Error($"IPAddress = {_httpClient.BaseAddress}" + "\r\n" + $"-- API NOT ALLOWED. [{nameof(MiddlewarePostMissionQueueAsync)}] --"); return null; }

            try
            {
                //수정본
                var response = await _httpClient.PostAsJsonAsync("missions/middleware", value);
                var jsonResponse = await response.Content.ReadAsStringAsync();

                var missionQueueResponse = new ApResponseDto
                {
                    statusCode = Convert.ToInt32(response.StatusCode),
                    statusText = response.StatusCode.ToString(),
                    message = jsonResponse
                };
                return missionQueueResponse;

                //기존
                //var response = await _httpClient.PostAsJsonAsync("api/Workers/mission_queue", value);
                //var jsonResponse = await response.Content.ReadAsStringAsync();
                //return JsonConvert.DeserializeObject<ApiPostResponseDtoMissionQueue>(jsonResponse);
            }
            //catch (Exception ex) when (True(() => _logger.Error(ex)))
            catch (Exception ex) when (True(() => ApiLogger.Error($"IPAddress = {_httpClient.BaseAddress}" + "\r\n" + ex)))
            {
                return null;
            }
        }
        public async Task<ApResponseDto> WorkerDeleteMissionQueueAsync(string id)
        {
            if (!AcceptFilterUtility.WriteAccepted) { ApiLogger.Error($"IPAddress = {_httpClient.BaseAddress}" + "\r\n" + $"-- API NOT ALLOWED. [{nameof(WorkerDeleteMissionQueueAsync)}] --"); return null; }

            try
            {
                //수정본
                var response = await _httpClient.DeleteAsync($"api/Workers/mission_queue/{id}");
                var jsonResponse = await response.Content.ReadAsStringAsync();

                var missionQueueResponse = new ApResponseDto
                {
                    statusCode = Convert.ToInt32(response.StatusCode),
                    statusText = response.StatusCode.ToString(),
                    message = jsonResponse
                };
                return missionQueueResponse;

                //기존
                //var response = await _httpClient.PostAsJsonAsync("api/Workers/mission_queue", value);
                //var jsonResponse = await response.Content.ReadAsStringAsync();
                //return JsonConvert.DeserializeObject<ApiPostResponseDtoMissionQueue>(jsonResponse);
            }
            //catch (Exception ex) when (True(() => _logger.Error(ex)))
            catch (Exception ex) when (True(() => ApiLogger.Error($"IPAddress = {_httpClient.BaseAddress}" + "\r\n" + ex)))
            {
                return null;
            }
        }
        public async Task<ApResponseDto> MiddlewareDeleteMissionQueueAsync(string id)
        {
            if (!AcceptFilterUtility.WriteAccepted) { ApiLogger.Error($"IPAddress = {_httpClient.BaseAddress}" + "\r\n" + $"-- API NOT ALLOWED. [{nameof(MiddlewareDeleteMissionQueueAsync)}] --"); return null; }

            try
            {
                //수정본
                var response = await _httpClient.DeleteAsync($"api/missions/middleware/{id}");
                var jsonResponse = await response.Content.ReadAsStringAsync();

                var missionQueueResponse = new ApResponseDto
                {
                    statusCode = Convert.ToInt32(response.StatusCode),
                    statusText = response.StatusCode.ToString(),
                    message = jsonResponse
                };
                return missionQueueResponse;

                //기존
                //var response = await _httpClient.PostAsJsonAsync("api/Workers/mission_queue", value);
                //var jsonResponse = await response.Content.ReadAsStringAsync();
                //return JsonConvert.DeserializeObject<ApiPostResponseDtoMissionQueue>(jsonResponse);
            }
            //catch (Exception ex) when (True(() => _logger.Error(ex)))
            catch (Exception ex) when (True(() => ApiLogger.Error($"IPAddress = {_httpClient.BaseAddress}" + "\r\n" + ex)))
            {
                return null;
            }
        }
        public async Task<ApResponseDto> PositionPatchAsync(string Id,object value)
        {
            if (!AcceptFilterUtility.WriteAccepted) { ApiLogger.Error($"IPAddress = {_httpClient.BaseAddress}" + "\r\n" + $"-- API NOT ALLOWED. [{nameof(MiddlewarePostMissionQueueAsync)}] --"); return null; }

            try
            {
                //수정본
                var response = await _httpClient.PatchAsJsonAsync($"api/positions/{Id}", value);
                var jsonResponse = await response.Content.ReadAsStringAsync();

                var missionQueueResponse = new ApResponseDto
                {
                    statusCode = Convert.ToInt32(response.StatusCode),
                    statusText = response.StatusCode.ToString(),
                    message = jsonResponse
                };
                return missionQueueResponse;

                //기존
                //var response = await _httpClient.PostAsJsonAsync("api/Workers/mission_queue", value);
                //var jsonResponse = await response.Content.ReadAsStringAsync();
                //return JsonConvert.DeserializeObject<ApiPostResponseDtoMissionQueue>(jsonResponse);
            }
            //catch (Exception ex) when (True(() => _logger.Error(ex)))
            catch (Exception ex) when (True(() => ApiLogger.Error($"IPAddress = {_httpClient.BaseAddress}" + "\r\n" + ex)))
            {
                return null;
            }
        }
        public override string ToString()
        {
            return $"BaseAddress={_httpClient.BaseAddress.AbsoluteUri}";
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }
    }
}