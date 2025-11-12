using Common.DTOs.Workers;
using Data.Interfaces;
using log4net;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using static ExceptionFilterUtility;

namespace WorkerAPI.WorkerAPIs
{
    public class MirApi : IMirApi, IDisposable
    {
        private static readonly ILog ApiLogger = LogManager.GetLogger("ApiEvent");
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerSettings _settings;

        public Uri BaseAddress => _httpClient.BaseAddress;

        public MirApi( string ip, double timeout, JsonSerializerSettings settings = null)
        {
            _httpClient = MakeHttpClient(ip, timeout);
            _settings = settings ?? new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, MissingMemberHandling = MissingMemberHandling.Ignore };
        }

        private HttpClient MakeHttpClient(string ip, double timeout)
        {
            var httpClient = new HttpClient();
            string auth = string.Format("{0}:{1}", "distributor", "distributor".ToSHA256());
            //string auth = string.Format("{0}:{1}", "admin", "admin".ToSHA256());
            string accessToken = auth.ToBase64Encode();
            //string accessToken = "YWRtaW46OGM2OTc2ZTViNTQxMDQxNWJkZTkwOGJkNGRlZTE1ZGZiMTY3YTljODczZmM0YmI4YTgxZjZmMmFiNDQ4YTkxOA==";
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", accessToken);
            httpClient.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("en_US"));
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.Timeout = TimeSpan.FromMilliseconds(timeout);
#if MIRDEMO
            string uriString = $"http://localhost:5000/api/v2.0.0/";
#else
            string uriString = $"http://{ip.TrimEnd('/')}/api/v2.0.0/";
#endif
            httpClient.BaseAddress = new Uri(uriString);

            return httpClient;
        }

        public async Task<RobotStatusResponse> GetStatusAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<RobotStatusResponse>("status", _settings);
            }
            //catch (Exception ex) when (True(() => _logger.Error(ex)))
            catch (Exception ex) when (True(() => ApiLogger.Error($"IPAddress = {_httpClient.BaseAddress}" + "\r\n" + ex)))
            {
                return null;
            }
        }

        public RobotStatusResponse PutStatusById(int value) => PutStatusByIdAsync(value).Result;

        public async Task<RobotStatusResponse> PutStatusByIdAsync(int id)
        {
            if (!AcceptFilterUtility.WriteAccepted) { ApiLogger.Error($"IPAddress = {_httpClient.BaseAddress}" + "\r\n" + $"-- API NOT ALLOWED. [{nameof(PutStatusByIdAsync)}] --"); return null; }

            try
            {
                //var response = await _httpClient.PutAsJsonAsync($"registers/{id}", new { value });
                var response = await _httpClient.PutAsJsonAsync($"status", new { state_id = id });
                var jsonResponse = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<RobotStatusResponse>(jsonResponse);
            }
            //catch (Exception ex) when (True(() => _logger.Error(ex)))
            catch (Exception ex) when (True(() => ApiLogger.Error($"IPAddress = {_httpClient.BaseAddress}" + "\r\n" + ex)))
            {
                return null;
            }
        }

        public async Task<GetHookStatusResponse> GetHookStatusAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<GetHookStatusResponse>("hook/status");
            }
            //catch (Exception ex) when (True(() => _logger.Error(ex)))
            catch (Exception ex) when (True(() => ApiLogger.Error($"IPAddress = {_httpClient.BaseAddress}" + "\r\n" + ex)))

            {
                return null;
            }
        }

        public async Task<List<MissionResponse>> GetMissionsAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<MissionResponse>>("missions");
            }
            //catch (Exception ex) when (True(() => _logger.Error(ex)))
            catch (Exception ex) when (True(() => ApiLogger.Error($"IPAddress = {_httpClient.BaseAddress}" + "\r\n" + ex)))
            {
                return null;
            }
        }

        public async Task<List<MissionQueueSimpleResponse>> GetMissionQueueAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<MissionQueueSimpleResponse>>("mission_queue");
            }
            //catch (Exception ex) when (True(() => _logger.Error(ex)))
            catch (Exception ex) when (True(() => ApiLogger.Error($"IPAddress = {_httpClient.BaseAddress}" + "\r\n" + ex)))
            {
                return null;
            }
        }

        public async Task<MissionQueueSimpleResponse> PostMissionQueueAsync(object value)
        {
            if (!AcceptFilterUtility.WriteAccepted) { ApiLogger.Error($"IPAddress = {_httpClient.BaseAddress}" + "\r\n" + $"-- API NOT ALLOWED. [{nameof(PostMissionQueueAsync)}] --"); return null; }

            try
            {
                var response = await _httpClient.PostAsJsonAsync("mission_queue", value);
                var jsonResponse = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<MissionQueueSimpleResponse>(jsonResponse);
            }
            //catch (Exception ex) when (True(() => _logger.Error(ex)))
            catch (Exception ex) when (True(() => ApiLogger.Error($"IPAddress = {_httpClient.BaseAddress}" + "\r\n" + ex)))
            {
                return null;
            }
        }

        public async Task<MissionQueueDetailResponse> GetMissionQueueByIdAsync(int id)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<MissionQueueDetailResponse>($"mission_queue/{id}");
            }
            //catch (Exception ex) when (True(() => _logger.Error(ex)))
            catch (Exception ex) when (True(() => ApiLogger.Error($"IPAddress = {_httpClient.BaseAddress}" + "\r\n" + ex)))
            {
                return null;
            }
        }

        public async Task<bool> DeleteMissionQueueAsync()
        {
            if (!AcceptFilterUtility.WriteAccepted) { ApiLogger.Error($"IPAddress = {_httpClient.BaseAddress}" + "\r\n" + $"-- API NOT ALLOWED. [{nameof(DeleteMissionQueueAsync)}] --"); return false; }

            try
            {
                var response = await _httpClient.DeleteFromJsonAsync("mission_queue");
                return true;
            }
            //catch (Exception ex) when (True(() => _logger.Error(ex)))
            catch (Exception ex) when (True(() => ApiLogger.Error($"IPAddress = {_httpClient.BaseAddress}" + "\r\n" + ex)))
            {
                return false;
            }
        }

        public async Task<bool> DeleteMissionQueueByIdAsync(int id)
        {
            if (!AcceptFilterUtility.WriteAccepted) { ApiLogger.Error($"IPAddress = {_httpClient.BaseAddress}" + "\r\n" + $"-- API NOT ALLOWED. [{nameof(DeleteMissionQueueByIdAsync)}] --"); return false; }

            try
            {
                var response = await _httpClient.DeleteFromJsonAsync($"mission_queue/{id}");
                return true;
            }
            //catch (Exception ex) when (True(() => _logger.Error(ex)))
            catch (Exception ex) when (True(() => ApiLogger.Error($"IPAddress = {_httpClient.BaseAddress}" + "\r\n" + ex)))
            {
                return false;
            }
        }

        public List<RegisterResponse> GetRegisters() => GetRegistersAsync().Result;

        public RegisterResponse GetRegisterById(int id) => GetRegisterByIdAsync(id).Result;

        public RegisterResponse PutRegisterById(int id, int value) => PutRegisterByIdAsync(id, value).Result;

        public async Task<List<RegisterResponse>> GetRegistersAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<RegisterResponse>>("registers");
            }
            //catch (Exception ex) when (True(() => _logger.Error(ex)))
            catch (Exception ex) when (True(() => ApiLogger.Error($"IPAddress = {_httpClient.BaseAddress}" + "\r\n" + ex)))
            {
                return null;
            }
        }

        public async Task<RegisterResponse> GetRegisterByIdAsync(int id)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<RegisterResponse>($"registers/{id}");
            }
            //catch (Exception ex) when (True(() => _logger.Error(ex)))
            catch (Exception ex) when (True(() => ApiLogger.Error($"IPAddress = {_httpClient.BaseAddress}" + "\r\n" + ex)))
            {
                return null;
            }
        }

        public async Task<RegisterResponse> PutRegisterByIdAsync(int id, int value)
        {
            if (!AcceptFilterUtility.WriteAccepted) { ApiLogger.Error($"IPAddress = {_httpClient.BaseAddress}" + "\r\n" + $"-- API NOT ALLOWED. [{nameof(PutRegisterById)}] --"); return null; }

            try
            {
                //var response = await _httpClient.PutAsJsonAsync($"registers/{id}", new { value });
                var response = await _httpClient.PutAsJsonAsync($"registers/{id}", new { value });
                var jsonResponse = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<RegisterResponse>(jsonResponse);
            }
            //catch (Exception ex) when (True(() => _logger.Error(ex)))
            catch (Exception ex) when (True(() => ApiLogger.Error($"IPAddress = {_httpClient.BaseAddress}" + "\r\n" + ex)))
            {
                return null;
            }
        }

        public async Task<List<ExperimentalDiagnosticsResponse>> GetExperimentalAsync() ///Power System/Battery Management System BMS데이터를 확인할수있다 Test 아직 진행중
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<ExperimentalDiagnosticsResponse>>("experimental/diagnostics");
            }
            //catch (Exception ex) when (True(() => _logger.Error(ex)))
            catch (Exception ex) when (True(() => ApiLogger.Error($"IPAddress = {_httpClient.BaseAddress}" + "\r\n" + ex)))
            {
                return null;
            }
        }

        public async Task<List<MirMapSimpleResponse>> GetMapsAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<MirMapSimpleResponse>>("maps");
            }
            //catch (Exception ex) when (True(() => _logger.Error(ex)))
            catch (Exception ex) when (True(() => ApiLogger.Error($"IPAddress = {_httpClient.BaseAddress}" + "\r\n" + ex)))
            {
                return null;
            }
        }

        public async Task<MirMapDetailResponse> GetMapByIdAsync(string guid)
        {
            try
            {
                var newMap = await _httpClient.GetFromJsonAsync<MirMapDetailResponse>($"maps/{guid}");

                // decode map image
                using (var ms = new System.IO.MemoryStream())
                {
                    byte[] mapDecodedBytes = Convert.FromBase64String(newMap.base_map);
                    ms.Write(mapDecodedBytes, 0, mapDecodedBytes.Length);
                    //newMap.Image = System.Drawing.Image.FromStream(ms);
                }

                return newMap;
            }
            //catch (Exception ex) when (True(() => _logger.Error(ex)))
            catch (Exception ex) when (True(() => ApiLogger.Error($"IPAddress = {_httpClient.BaseAddress}" + "\r\n" + ex)))
            {
                return null;
            }
        }

        public async Task<List<MirPositionSimpleResponse>> GetPositionsAsync(string guid)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<MirPositionSimpleResponse>>($"maps/{guid}/positions");
            }
            //catch (Exception ex) when (True(() => _logger.Error(ex)))
            catch (Exception ex) when (True(() => ApiLogger.Error($"IPAddress = {_httpClient.BaseAddress}" + "\r\n" + ex)))
            {
                return null;
            }
        }

        public async Task<MirPositionDetailResponse> GetPositionByIdAsync(string guid)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<MirPositionDetailResponse>($"positions/{guid}");
            }
            //catch (Exception ex) when (True(() => _logger.Error(ex)))
            catch (Exception ex) when (True(() => ApiLogger.Error($"IPAddress = {_httpClient.BaseAddress}" + "\r\n" + ex)))
            {
                return null;
            }
        }

        public async Task<ExperimentalDiagnosticsResponse> GetExperimentalDiagnosticsAsync() ///Power System/Battery Management System BMS데이터를 확인할수있다 Test 아직 진행중
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<ExperimentalDiagnosticsResponse>("experimental/diagnostics");
            }
            //catch (Exception ex) when (True(() => _logger.Error(ex)))
            catch (Exception ex) when (True(() => ApiLogger.Error($"{nameof(GetExperimentalDiagnosticsAsync)},IPAddress = {_httpClient.BaseAddress}" + "\r\n" + ex + ex.InnerException)))
            {
                return null;
            }
        }

        public async Task<List<DistanceResponse>> GetDistanceAsync() ///Power System/Battery Management System BMS데이터를 확인할수있다 Test 아직 진행중
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<DistanceResponse>>("statistics/distance");
            }
            //catch (Exception ex) when (True(() => _logger.Error(ex)))
            catch (Exception ex) when (True(() => ApiLogger.Error($"{nameof(GetDistanceAsync)},IPAddress = {_httpClient.BaseAddress}" + "\r\n" + ex)))
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