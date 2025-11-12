using Common.DTOs.Workers;
using Data.Interfaces;
using log4net;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using static ExceptionFilterUtility;

namespace WorkerAPI.WorkerAPIs
{
    public class FleetApi : IFleetApi, IDisposable
    {
        private static readonly ILog ApiLogger = LogManager.GetLogger("ApiEvent");
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerSettings _settings;

        public Uri BaseAddress => _httpClient.BaseAddress;

        public FleetApi(string ip, double timeout, string connectId, string connectPassword, JsonSerializerSettings settings = null)
        {
            _httpClient = MakeHttpClient(ip, timeout, connectId, connectPassword);
            _settings = settings ?? new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, MissingMemberHandling = MissingMemberHandling.Ignore };
        }

        private HttpClient MakeHttpClient(string ip, double timeout, string connectId, string connectPassword)
        {
            var httpClient = new HttpClient();
            string auth = string.Format("{0}:{1}", connectId, connectPassword.ToSHA256());
            string accessToken = auth.ToBase64Encode();
            //string accessToken = "ZGlzdHJpYnV0b3I6NjJmMmYwZjFlZmYxMGQzMTUyYzk1ZjZmMDU5NjU3NmU0ODJiYjhlNDQ4MDY0MzNmNGNmOTI5NzkyODM0YjAxNA==";
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

        public async Task<List<int>> GetRobotIdsAsync()
        {
            try
            {
                var robotInfos = await _httpClient.GetFromJsonAsync<List<FleetRobotInfoResponse>>("robots");
                return new List<int>(robotInfos.Select(x => x.id));
            }
            catch (Exception ex) when (True(() => ApiLogger.Error(ex)))
            {
                return null;
            }
        }

        public async Task<FleetRobotInfoResponse> GetRobotByIdAsync(int id)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<FleetRobotInfoResponse>($"robots/{id}");
            }
            catch (Exception ex) when (True(() => ApiLogger.Error(ex)))
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
            catch (Exception ex) when (True(() => ApiLogger.Error(ex)))
            {
                return null;
            }
        }

        public async Task<List<MissionSchedulerSimpleResponse>> GetMissionSchedulerAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<MissionSchedulerSimpleResponse>>("mission_scheduler");
            }
            catch (Exception ex) when (True(() => ApiLogger.Error(ex)))
            {
                return null;
            }
        }

        public async Task<MissionSchedulerSimpleResponse> PostMissionSchedulerAsync(object value)
        {
            if (!AcceptFilterUtility.WriteAccepted) { ApiLogger.Error($"-- API NOT ALLOWED. [{nameof(PostMissionSchedulerAsync)}] --"); return null; }

            try
            {
                var response = await _httpClient.PostAsJsonAsync("mission_scheduler", value);
                var jsonResponse = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<MissionSchedulerSimpleResponse>(jsonResponse);
            }
            catch (Exception ex) when (True(() => ApiLogger.Error(ex)))
            {
                return null;
            }
        }

        public async Task<MissionSchedulerDetailResponse> GetMissionSchedulerByIdAsync(int id)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<MissionSchedulerDetailResponse>($"mission_scheduler/{id}");
            }
            catch (Exception ex) when (True(() => ApiLogger.Error(ex)))
            {
                return null;
            }
        }

        public async Task<bool> DeleteMissionSchedulerAsync()
        {
            if (!AcceptFilterUtility.WriteAccepted) { ApiLogger.Error($"-- API NOT ALLOWED. [{nameof(DeleteMissionSchedulerAsync)}] --"); return false; }

            try
            {
                var response = await _httpClient.DeleteFromJsonAsync("mission_scheduler");
                return true;
            }
            catch (Exception ex) when (True(() => ApiLogger.Error(ex)))
            {
                return false;
            }
        }

        public async Task<bool> DeleteMissionSchedulerByIdAsync(int id)
        {
            if (!AcceptFilterUtility.WriteAccepted) { ApiLogger.Error($"-- API NOT ALLOWED. [{nameof(DeleteMissionSchedulerByIdAsync)}] --"); return false; }

            try
            {
                var response = await _httpClient.DeleteFromJsonAsync($"mission_scheduler/{id}");
                return true;
            }
            catch (Exception ex) when (True(() => ApiLogger.Error(ex)))
            {
                return false;
            }
        }

        public async Task<List<FleetExperimentalDiagnosticsResponse>> GetExperimentalAsync()
        {
            try
            {
                var test = await _httpClient.GetFromJsonAsync<List<FleetExperimentalDiagnosticsResponse>>("experimental/robots_diagnostics");
                return await _httpClient.GetFromJsonAsync<List<FleetExperimentalDiagnosticsResponse>>("experimental/robots_diagnostics");
            }
            //catch (Exception ex) when (True(() => _logger.Error(ex)))
            catch (Exception ex) when (True(() => ApiLogger.Error($"IPAddress = {_httpClient.BaseAddress}" + "\r\n" + ex)))
            {
                return null;
            }
        }

        public async Task<List<FleetMapSimpleResponse>> GetMapsAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<FleetMapSimpleResponse>>("maps");
            }
            catch (Exception ex) when (True(() => ApiLogger.Error(ex)))
            {
                return null;
            }
        }

        public async Task<FleetMapDetailResponse> GetMapByIdAsync(string guid)
        {
            try
            {
                var newMap = await _httpClient.GetFromJsonAsync<FleetMapDetailResponse>($"maps/{guid}");

                // decode map image
                using (var ms = new System.IO.MemoryStream())
                {
                    byte[] mapDecodedBytes = Convert.FromBase64String(newMap.map);
                    ms.Write(mapDecodedBytes, 0, mapDecodedBytes.Length);
                    //newMap.Image = System.Drawing.Image.FromStream(ms);
                }

                return newMap;
            }
            catch (Exception ex) when (True(() => ApiLogger.Error(ex)))
            {
                return null;
            }
        }

        public async Task<List<FleetPositionTypesSimpleResponse>> GetPositionTypesAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<FleetPositionTypesSimpleResponse>>($"position_types");
            }
            catch (Exception ex) when (True(() => ApiLogger.Error(ex)))
            {
                return null;
            }
        }
        public async Task<FleetPositionTypesSimpleResponse> GetPositionTypeByIdAsync(int id)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<FleetPositionTypesSimpleResponse>($"position_types/{id}");
            }
            catch (Exception ex) when (True(() => ApiLogger.Error(ex)))
            {
                return null;
            }
        }
        public async Task<List<FleetPositionSimpleResponse>> GetMapPositionsAsync(string guid)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<FleetPositionSimpleResponse>>($"maps/{guid}/positions");
            }
            catch (Exception ex) when (True(() => ApiLogger.Error(ex)))
            {
                return null;
            }
        }

        public async Task<FleetPositionDetailResponse> GetPositionByIdAsync(string guid)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<FleetPositionDetailResponse>($"positions/{guid}");
            }
            catch (Exception ex) when (True(() => ApiLogger.Error(ex)))
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