//using MediatR;
//using NTC.QoS.Application.Features.Dashboard.Requests;
//using NTC.QoS.Domain.Interfaces;
//using NTC.QoS.Domain.Models.DTOs;

//namespace NTC.QoS.Application.Features.Dashboard.Handlers.Queries;

//public class GetTopProvincesByCellsCountHandler(IProvinceRepository provinceRepository) : IRequestHandler<GetTopProvincesByCellsCountRequest, ApiResponse<List<Dictionary<string, int>>>>
//{
//    private readonly IProvinceRepository _provinceRepository = provinceRepository;

//    public async Task<ApiResponse<List<Dictionary<string, int>>>> Handle(GetTopProvincesByCellsCountRequest request, CancellationToken cancellationToken)
//    {
//        var nAPs = _napRepository.Query(c => !c.IsDeleted && 
//            c.CreatedAt >= request.StartDate && c.CreatedAt <= request.EndDate).Count();

//        return await Task.FromResult(new ApiResponse<List<Dictionary<string, int>>>
//        {
//            Data = nAPs,
//            Succeeded = true,
//            Message = "Total N A P count retrieved successfully."
//        });
//    }
//}
