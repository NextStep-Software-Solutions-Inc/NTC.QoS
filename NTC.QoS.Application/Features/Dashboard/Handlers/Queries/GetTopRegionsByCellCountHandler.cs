using MediatR;
using NTC.QoS.Application.Features.Dashboard.Requests;
using NTC.QoS.Application.Services;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.GeoEntity;

namespace NTC.QoS.Application.Features.Dashboard.Handlers.Queries;

public class GetTopRegionsByCellCountHandler(GeospatialQueryService geospatialQueryService) : IRequestHandler<GetTopRegionsByCellCountRequest, ApiResponse<List<GeoEntityCellCountDto>>>
{
    private readonly GeospatialQueryService _geospatialQueryService = geospatialQueryService;

    public async Task<ApiResponse<List<GeoEntityCellCountDto>>> Handle(GetTopRegionsByCellCountRequest request, CancellationToken cancellationToken)
    {
        var result = await _geospatialQueryService.GetTopGeoEntitiesByCellCountAsync<Domain.Entities.Region>(request.TopCount);

        return new ApiResponse<List<GeoEntityCellCountDto>>
        {
            Succeeded = true,
            Message = "Top regions retrieved successfully.",
            Data = result
        };
    }
}
