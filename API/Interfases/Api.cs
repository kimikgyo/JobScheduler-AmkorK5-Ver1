using Common.DTOs.Rests.Carriers;
using Common.DTOs.Rests.JobTemplates;
using Common.DTOs.Rests.Maps;
using Common.DTOs.Rests.Nodes_Edges;
using Common.DTOs.Rests.Positions;
using Common.DTOs.Rests.Workers;
using Common.Interfaces;
using Common.Models.Bases;
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

        public async Task<List<Response_WorkerDto>> GetResourceWorker()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<Response_WorkerDto>>("api/workers");
            }
            //catch (Exception ex) when (True(() => _logger.Error(ex)))
            catch (Exception ex) when (True(() => ApiLogger.Error($"IPAddress = {_httpClient.BaseAddress}" + "\r\n" + ex)))
            {
                return null;
            }
        }

        public async Task<List<Response_MapDto>> GetResourceMap()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<Response_MapDto>>("api/maps");
            }
            //catch (Exception ex) when (True(() => _logger.Error(ex)))
            catch (Exception ex) when (True(() => ApiLogger.Error($"IPAddress = {_httpClient.BaseAddress}" + "\r\n" + ex)))
            {
                return null;
            }
        }

        public async Task<List<Response_PositionDto>> GetResourcePosition()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<Response_PositionDto>>("api/positions");
            }
            //catch (Exception ex) when (True(() => _logger.Error(ex)))
            catch (Exception ex) when (True(() => ApiLogger.Error($"IPAddress = {_httpClient.BaseAddress}" + "\r\n" + ex)))
            {
                return null;
            }
        }

        public async Task<List<Response_CarrierDto>> GetResourceCarrier()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<Response_CarrierDto>>("api/carriers");
            }
            //catch (Exception ex) when (True(() => _logger.Error(ex)))
            catch (Exception ex) when (True(() => ApiLogger.Error($"IPAddress = {_httpClient.BaseAddress}" + "\r\n" + ex)))
            {
                return null;
            }
        }

        public async Task<Response_Node_EdgeDto> Post_Routes_Plan_Async(object value)
        {
            if (!AcceptFilterUtility.WriteAccepted) { ApiLogger.Error($"IPAddress = {_httpClient.BaseAddress}" + "\r\n" + $"-- API NOT ALLOWED. [{nameof(ElevatorPostMissionQueueAsync)}] --"); return null; }

            try
            {
                //기존
                var response = await _httpClient.PostAsJsonAsync("api/routes/plan", value);
                var jsonResponse = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<Response_Node_EdgeDto>(jsonResponse);
            }
            //catch (Exception ex) when (True(() => _logger.Error(ex)))
            catch (Exception ex) when (True(() => ApiLogger.Error($"IPAddress = {_httpClient.BaseAddress}" + "\r\n" + ex)))
            {
                return null;
            }
        }

        public async Task<ApiResponseDto> ElevatorPostMissionQueueAsync(object value)
        {
            if (!AcceptFilterUtility.WriteAccepted) { ApiLogger.Error($"IPAddress = {_httpClient.BaseAddress}" + "\r\n" + $"-- API NOT ALLOWED. [{nameof(ElevatorPostMissionQueueAsync)}] --"); return null; }

            try
            {
                //수정본
                var response = await _httpClient.PostAsJsonAsync("api/missions", value);
                var jsonResponse = await response.Content.ReadAsStringAsync();

                var missionQueueResponse = new ApiResponseDto
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

        public async Task<ApiResponseDto> ElevatorDeletetMissionQueueAsync(string id)
        {
            if (!AcceptFilterUtility.WriteAccepted) { ApiLogger.Error($"IPAddress = {_httpClient.BaseAddress}" + "\r\n" + $"-- API NOT ALLOWED. [{nameof(ElevatorPostMissionQueueAsync)}] --"); return null; }

            try
            {
                //수정본
                var response = await _httpClient.DeleteAsync($"api/missions/{id}");
                var jsonResponse = await response.Content.ReadAsStringAsync();

                var missionQueueResponse = new ApiResponseDto
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

        public async Task<ApiResponseDto> WorkerPostMissionQueueAsync(object value)
        {
            if (!AcceptFilterUtility.WriteAccepted) { ApiLogger.Error($"IPAddress = {_httpClient.BaseAddress}" + "\r\n" + $"-- API NOT ALLOWED. [{nameof(WorkerPostMissionQueueAsync)}] --"); return null; }

            try
            {
                //수정본
                var response = await _httpClient.PostAsJsonAsync("missions/worker", value);
                var jsonResponse = await response.Content.ReadAsStringAsync();

                var missionQueueResponse = new ApiResponseDto
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

        public async Task<ApiResponseDto> WorkerDeleteMissionQueueAsync(string id)
        {
            if (!AcceptFilterUtility.WriteAccepted) { ApiLogger.Error($"IPAddress = {_httpClient.BaseAddress}" + "\r\n" + $"-- API NOT ALLOWED. [{nameof(WorkerDeleteMissionQueueAsync)}] --"); return null; }

            try
            {
                //수정본
                var response = await _httpClient.DeleteAsync($"missions/worker/{id}");
                var jsonResponse = await response.Content.ReadAsStringAsync();

                var missionQueueResponse = new ApiResponseDto
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

        public async Task<ApiResponseDto> MiddlewarePostMissionQueueAsync(object value)
        {
            if (!AcceptFilterUtility.WriteAccepted) { ApiLogger.Error($"IPAddress = {_httpClient.BaseAddress}" + "\r\n" + $"-- API NOT ALLOWED. [{nameof(MiddlewarePostMissionQueueAsync)}] --"); return null; }

            try
            {
                //수정본
                var response = await _httpClient.PostAsJsonAsync("missions/middleware", value);
                var jsonResponse = await response.Content.ReadAsStringAsync();

                var missionQueueResponse = new ApiResponseDto
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

        public async Task<ApiResponseDto> MiddlewareDeleteMissionQueueAsync(string id)
        {
            if (!AcceptFilterUtility.WriteAccepted) { ApiLogger.Error($"IPAddress = {_httpClient.BaseAddress}" + "\r\n" + $"-- API NOT ALLOWED. [{nameof(MiddlewareDeleteMissionQueueAsync)}] --"); return null; }

            try
            {
                //수정본
                var response = await _httpClient.DeleteAsync($"api/missions/middleware/{id}");
                var jsonResponse = await response.Content.ReadAsStringAsync();

                var missionQueueResponse = new ApiResponseDto
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

        public async Task<ApiResponseDto> PositionPatchAsync(string Id, object value)
        {
            if (!AcceptFilterUtility.WriteAccepted) { ApiLogger.Error($"IPAddress = {_httpClient.BaseAddress}" + "\r\n" + $"-- API NOT ALLOWED. [{nameof(MiddlewarePostMissionQueueAsync)}] --"); return null; }

            try
            {
                //수정본
                var response = await _httpClient.PatchAsJsonAsync($"api/positions/{Id}", value);
                var jsonResponse = await response.Content.ReadAsStringAsync();

                var missionQueueResponse = new ApiResponseDto
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