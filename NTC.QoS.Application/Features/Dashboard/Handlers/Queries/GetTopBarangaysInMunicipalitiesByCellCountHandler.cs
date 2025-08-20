using MediatR;
using NTC.QoS.Application.Features.Dashboard.Requests;
using NTC.QoS.Application.Services;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.GeoEntity;

namespace NTC.QoS.Application.Features.Dashboard.Handlers.Queries;

public class GetTopBarangaysInMunicipalityByCellCountHandler(GeospatialQueryService geospatialQueryService) : IRequestHandler<GetTopBarangaysInMunicipalityByCellCountRequest, ApiResponse<List<GeoEntityCellCountDto>>>
{
    private readonly GeospatialQueryService _geospatialQueryService = geospatialQueryService;

    public async Task<ApiResponse<List<GeoEntityCellCountDto>>> Handle(GetTopBarangaysInMunicipalityByCellCountRequest request, CancellationToken cancellationToken)
    {
        var result = await _geospatialQueryService.GetTopChildEntitiesByCellCountAsync<Domain.Entities.Municipality, Domain.Entities.Barangay>(request.MunicipalityId, "MunicipalityId", request.Limit, request.StartDate, request.EndDate);

        return new ApiResponse<List<GeoEntityCellCountDto>>
        {
            Succeeded = true,
            Message = $"Top barangays in MunicipalityId [{request.MunicipalityId}] retrieved successfully.",
            Data = result
        };
    }
}
