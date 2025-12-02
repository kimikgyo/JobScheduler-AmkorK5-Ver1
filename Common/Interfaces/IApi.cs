using Common.DTOs.Rests.Carriers;
using Common.DTOs.Rests.JobTemplates;
using Common.DTOs.Rests.Maps;
using Common.DTOs.Rests.Nodes_Edges;
using Common.DTOs.Rests.Positions;
using Common.DTOs.Rests.Workers;
using Common.Models.Bases;

namespace Common.Interfaces
{
    public interface IApi
    {
        Uri BaseAddress { get; }

        Task<List<Response_WorkerDto>> GetResourceWorker();

        Task<List<Response_MapDto>> GetResourceMap();

        Task<List<Response_PositionDto>> GetResourcePosition();

        Task<List<Response_CarrierDto>> GetResourceCarrier();

        Task<ApiResponseDto> WorkerPostMissionQueueAsync(object value);

        Task<ApiResponseDto> ElevatorPostMissionQueueAsync(object value);

        Task<ApiResponseDto> MiddlewarePostMissionQueueAsync(object value);

        Task<ApiResponseDto> WorkerDeleteMissionQueueAsync(string id);

        Task<ApiResponseDto> MiddlewareDeleteMissionQueueAsync(string id);

        Task<ApiResponseDto> ElevatorDeletetMissionQueueAsync(string id);
        
        Task<ApiResponseDto> PositionPatchAsync(string id, object value);

        Task<Response_Node_EdgeDto> Post_Routes_Plan_Async(object value);
    }
}