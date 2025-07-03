using MediatR;
using NTC.QoS.Application.Features.Dashboard.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.GeoEntity;

namespace NTC.QoS.Application.Features.Dashboard.Handlers.Queries;

public class GetTopMunicipalitiesInProvinceByCellCountHandler(IGeospatialQueryService geospatialQueryService) : IRequestHandler<GetTopMunicipalitiesInProvinceByCellCountRequest, ApiResponse<List<GeoEntityCellCountDto>>>
{
    private readonly IGeospatialQueryService _geospatialQueryService = geospatialQueryService;

    public async Task<ApiResponse<List<GeoEntityCellCountDto>>> Handle(GetTopMunicipalitiesInProvinceByCellCountRequest request, CancellationToken cancellationToken)
    {
        var result = await _geospatialQueryService.GetTopMunicipalitiesInProvinceByCellCountAsync(request.ProvinceId, request.Limit, request.StartDate, request.EndDate);

        return new ApiResponse<List<GeoEntityCellCountDto>>
        {
            Succeeded = true,
            Message = $"Top municipalities in ProvinceId [{request.ProvinceId}] retrieved successfully.",
            Data = result
        };
    }
}
