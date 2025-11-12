using MediatR;
using NTC.QoS.Application.Features.Dashboard.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.GeoEntity;

namespace NTC.QoS.Application.Features.Dashboard.Handlers.Queries;

public class GetTopProvincesInRegionByCellCountHandler(IProvinceRepository provinceRepository) : IRequestHandler<GetTopProvincesInRegionByCellCountRequest, ApiResponse<List<GeoEntityCellCountDto>>>
{
    private readonly IProvinceRepository _provinceRepository = provinceRepository;

    public async Task<ApiResponse<List<GeoEntityCellCountDto>>> Handle(GetTopProvincesInRegionByCellCountRequest request, CancellationToken cancellationToken)
    {
        var result = await _provinceRepository.GetTopProvincesByCellCountAsync(
  request.RegionId,
   request.Limit,
      request.StartDate,
            request.EndDate,
   cancellationToken);

        return new ApiResponse<List<GeoEntityCellCountDto>>
        {
   Succeeded = true,
     Message = $"Top provinces in RegionId [{request.RegionId}] retrieved successfully.",
            Data = result
        };
    }
}
