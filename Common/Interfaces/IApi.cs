using Common.DTOs.Rests.Areas;
using Common.DTOs.Rests.Carriers;
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

        Task<List<Response_WorkerDto>> Get_Worker_Async();

        Task<List<Response_MapDto>> Get_Map_Async();

        Task<List<Response_PositionDto>> Get_Position_Async();

        Task<List<Response_CarrierDto>> Get_Carrier_Async();

        Task<List<Response_ACS_AareDto>> Get_ACS_Area_Async();

        Task<ResponseDto> Post_Worker_Mission_Async(object value);

        Task<ResponseDto> Post_Elevator_Mission_Async(object value);

        Task<ResponseDto> Post_Middleware_Mission_Async(object value);

        Task<ResponseDto> Post_Traffic_Mission_Async(object value);

        Task<ResponseDto> Deletet_Traffic_Mission_Async(string id);


        Task<ResponseDto> Delete_Worker_Mission_Async(string id);

        Task<ResponseDto> Delete_Middleware_Mission_Async(string id);

        Task<ResponseDto> Deletet_Elevator_Mission_Async(string id);

        Task<ResponseDto> Patch_Position_Async(string id, object value);

        Task<Response_Node_EdgeDto> Post_Routes_Plan_Async(object value);
    }
}