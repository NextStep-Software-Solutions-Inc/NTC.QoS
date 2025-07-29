using MediatR;
using NTC.QoS.Application.Features.Dashboard.Requests;
using NTC.QoS.Application.Services;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Dashboard;

namespace NTC.QoS.Application.Features.Dashboard.Handlers.Queries;

public class GetWirelessCoverageHandler(GeospatialQueryService geospatialQueryService) 
    : IRequestHandler<GetWirelessCoverageRequest, ApiResponse<WirelessCoverageResponseDto>>
{
    private readonly GeospatialQueryService _geospatialQueryService = geospatialQueryService;

    public async Task<ApiResponse<WirelessCoverageResponseDto>> Handle(GetWirelessCoverageRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _geospatialQueryService.GetWirelessCoverageAsync(
                request.StartDate,
                request.EndDate,
                request.RegionId);

            return new ApiResponse<WirelessCoverageResponseDto>
            {
                Succeeded = true,
                Message = "Wireless coverage data retrieved successfully.",
                Data = result
            };
        }
        catch (Exception ex)
        {
            return new ApiResponse<WirelessCoverageResponseDto>
            {
                Succeeded = false,
                Message = "Failed to retrieve wireless coverage data.",
                Errors = new Dictionary<string, string[]>
                {
                    { "Error", new[] { ex.Message } }
                }
            };
        }
    }
}
