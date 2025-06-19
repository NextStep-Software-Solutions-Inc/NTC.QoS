using MediatR;
using NTC.QoS.Application.Features.Dashboard.Requests;
using NTC.QoS.Application.Services;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.GeoEntity;

namespace NTC.QoS.Application.Features.Dashboard.Handlers.Queries;

public class GetTopProvincesInRegionByCellCountHandler(GeospatialQueryService geospatialQueryService) : IRequestHandler<GetTopProvincesInRegionByCellCountRequest, ApiResponse<List<GeoEntityCellCountDto>>>
{
    private readonly GeospatialQueryService _geospatialQueryService = geospatialQueryService;

    public async Task<ApiResponse<List<GeoEntityCellCountDto>>> Handle(GetTopProvincesInRegionByCellCountRequest request, CancellationToken cancellationToken)
    {
        var result = await _geospatialQueryService.GetTopChildEntitiesByCellCountAsync<Domain.Entities.Region, Domain.Entities.Province>(request.RegionId, "RegionId", request.TopCount, request.StartDate, request.EndDate);

        return new ApiResponse<List<GeoEntityCellCountDto>>
        {
            Succeeded = true,
            Message = $"Top provinces in RegionId [{request.RegionId}] retrieved successfully.",
            Data = result
        };
    }
}
