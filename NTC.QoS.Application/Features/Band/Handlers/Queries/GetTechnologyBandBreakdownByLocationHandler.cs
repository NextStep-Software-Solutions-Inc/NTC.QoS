using MediatR;
using Microsoft.Extensions.Logging;
using NTC.QoS.Application.Features.Band.Requests;
using NTC.QoS.Application.Services;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Analytics;

namespace NTC.QoS.Application.Features.Band.Handlers.Queries;

public class GetTechnologyBandBreakdownByLocationHandler : IRequestHandler<GetTechnologyBandBreakdownByLocationRequest, ApiResponse<TechnologyBandBreakdownDto>>
{
    private readonly IConnectivityCacheService _cacheService;
    private readonly ILogger<GetTechnologyBandBreakdownByLocationHandler> _logger;

    public GetTechnologyBandBreakdownByLocationHandler(
        IConnectivityCacheService cacheService,
        ILogger<GetTechnologyBandBreakdownByLocationHandler> logger)
    {
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<ApiResponse<TechnologyBandBreakdownDto>> Handle(GetTechnologyBandBreakdownByLocationRequest request, CancellationToken cancellationToken)
    {
        try
        {
            // Get from cache
            var techBandCacheKey = $"tech_band:{request.LocationType}:{request.LocationId}";
            var cachedBreakdown = await _cacheService.GetCachedResultAsync<TechnologyBandBreakdownDto>(techBandCacheKey);

            if (cachedBreakdown != null)
            {
                _logger.LogInformation("Retrieved technology-band breakdown from cache for {LocationType} {LocationId}", 
                    request.LocationType, request.LocationId);
                
                return new ApiResponse<TechnologyBandBreakdownDto>
                {
                    Succeeded = true,
                    Message = "Technology-band breakdown retrieved successfully",
                    Data = cachedBreakdown
                };
            }

            // Data not in cache
            _logger.LogWarning("Technology-band breakdown not found in cache for {LocationType} {LocationId}. Please generate connectivity assessment first.", 
                request.LocationType, request.LocationId);

            return new ApiResponse<TechnologyBandBreakdownDto>
            {
                Succeeded = false,
                Message = "Technology-band breakdown not found. Please generate a connectivity assessment for this location first.",
                Data = new TechnologyBandBreakdownDto()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving technology-band breakdown for {LocationType} {LocationId}", 
                request.LocationType, request.LocationId);
            
            return new ApiResponse<TechnologyBandBreakdownDto>
            {
                Succeeded = false,
                Message = "An error occurred while retrieving the technology-band breakdown",
                Errors = new Dictionary<string, string[]>
                {
                    { "Error", [ex.Message] }
                }
            };
        }
    }
}
