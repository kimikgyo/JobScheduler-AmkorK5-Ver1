using Common.DTOs.Bases;
using Common.DTOs.Jobs;
using Common.Models.Bases;

namespace Common.Interfaces
{
    public interface IApi
    {
        Uri BaseAddress { get; }

        Task<List<ApiGetResponseDtoResourceWorker>> GetResourceWorker();
        Task<List<ApiGetResponseDtoResourceMap>> GetResourceMap();
        Task<List<ApiGetResponseDtoResourcePosition>> GetResourcePosition(); 
        Task<List<ApiGetResponseDtoResourceCarrier>> GetResourceCarrier();
        Task<List<ApiGetResponseDtoResourceJobTemplate>> STIGetResourceJobTemplate();
        Task<List<ApiGetResponseDtoResourceJobTemplate>> AmkorGetResourceJobTemplate();
        Task<ApResponseDto> WorkerPostMissionQueueAsync(object value);
        Task<ApResponseDto> MiddlewarePostMissionQueueAsync(object value);
        Task<ApResponseDto> WorkerDeleteMissionQueueAsync(string id);
        Task<ApResponseDto> MiddlewareDeleteMissionQueueAsync(string id);
        Task<ApResponseDto> PositionPatchAsync(string id, object value);

    }
}