using MediatR;
using NTC.QoS.Application.Features.Dashboard.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.GeoEntity;

namespace NTC.QoS.Application.Features.Dashboard.Handlers.Queries;

public class GetTopBarangaysInMunicipalityByCellCountHandler(IGeospatialQueryService geospatialQueryService) : IRequestHandler<GetTopBarangaysInMunicipalityByCellCountRequest, ApiResponse<List<GeoEntityCellCountDto>>>
{
    private readonly IGeospatialQueryService _geospatialQueryService = geospatialQueryService;

    public async Task<ApiResponse<List<GeoEntityCellCountDto>>> Handle(GetTopBarangaysInMunicipalityByCellCountRequest request, CancellationToken cancellationToken)
    {
        var result = await _geospatialQueryService.GetTopBarangaysInMunicipalityByCellCountAsync(request.MunicipalityId, request.Limit, request.StartDate, request.EndDate);

        return new ApiResponse<List<GeoEntityCellCountDto>>
        {
            Succeeded = true,
            Message = $"Top barangays in MunicipalityId [{request.MunicipalityId}] retrieved successfully.",
            Data = result
        };
    }
}
