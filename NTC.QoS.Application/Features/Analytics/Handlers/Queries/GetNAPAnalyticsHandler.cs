using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NTC.QoS.Application.Features.Analytics.Requests;
using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Analytics;
using NTC.QoS.Infrastructure.Helper;

namespace NTC.QoS.Application.Features.Analytics.Handlers.Queries;

/// <summary>
/// Handler for NAP Analytics Dashboard requests
/// </summary>
public class GetNAPAnalyticsHandler : IRequestHandler<GetNAPAnalyticsRequest, ApiResponse<NAPAnalyticsResponseDto>>
{
    private readonly INAPRepository _napRepository;
    private readonly IBarangayRepository _barangayRepository;
    private readonly IMunicipalityRepository _municipalityRepository;
    private readonly IRepository<Domain.Entities.Province> _provinceRepository;
    private readonly IRepository<Domain.Entities.Region> _regionRepository;
    private readonly ILogger<GetNAPAnalyticsHandler> _logger;

    public GetNAPAnalyticsHandler(
        INAPRepository napRepository,
        IBarangayRepository barangayRepository,
        IMunicipalityRepository municipalityRepository,
        IRepository<Domain.Entities.Province> provinceRepository,
        IRepository<Domain.Entities.Region> regionRepository,
        ILogger<GetNAPAnalyticsHandler> logger)
    {
        _napRepository = napRepository;
        _barangayRepository = barangayRepository;
        _municipalityRepository = municipalityRepository;
        _provinceRepository = provinceRepository;
        _regionRepository = regionRepository;
        _logger = logger;
    }

    public async Task<ApiResponse<NAPAnalyticsResponseDto>> Handle(GetNAPAnalyticsRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Processing NAP analytics request");

            // Build query with filters
            var napQuery = _napRepository.Query();

            // Apply geographic filters
            if (request.BarangayIds is { Count: > 0 })
            {
                napQuery = await GeoQueryHelper.ApplyBoundaryFilterAsync(
                    request.BarangayIds, _barangayRepository, napQuery, cancellationToken);
            }
            else if (request.MunicipalityIds is { Count: > 0 })
            {
                napQuery = await GeoQueryHelper.ApplyBoundaryFilterAsync(
                    request.MunicipalityIds, _municipalityRepository, napQuery, cancellationToken);
            }
            else if (request.ProvinceIds is { Count: > 0 })
            {
                napQuery = await GeoQueryHelper.ApplyBoundaryFilterAsync(
                    request.ProvinceIds, _provinceRepository, napQuery, cancellationToken);
            }
            else if (request.RegionIds is { Count: > 0 })
            {
                napQuery = await GeoQueryHelper.ApplyBoundaryFilterAsync(
                    request.RegionIds, _regionRepository, napQuery, cancellationToken);
            }

            // Apply service provider filter
            if (request.ServiceProviderIds is { Count: > 0 })
            {
                napQuery = napQuery.Where(n => request.ServiceProviderIds.Contains(n.ServiceProviderId));
            }

            // Get NAPs with provider information
            var naps = await _napRepository.ToListAsync(
                napQuery.Select(n => new
                {
                    n.Id,
                    n.Location,
                    n.ServiceProviderId,
                    ProviderName = n.ServiceProvider.Provider.Name,
                    ProviderColor = n.ServiceProvider.Provider.Color
                }),
                cancellationToken);

            if (!naps.Any())
            {
                return new ApiResponse<NAPAnalyticsResponseDto>
                {
                    Succeeded = true,
                    Message = "No NAPs found for the specified filters",
                    Data = new NAPAnalyticsResponseDto()
                };
            }

            // Calculate summary statistics
            var totalNAPs = naps.Count;
            var providerGroups = naps
                .GroupBy(n => new { n.ServiceProviderId, n.ProviderName, n.ProviderColor })
                .Select(g => new
                {
                    g.Key.ServiceProviderId,
                    g.Key.ProviderName,
                    g.Key.ProviderColor,
                    Count = g.Count()
                })
                .OrderByDescending(p => p.Count)
                .ToList();

            var activeProviders = providerGroups.Count;
            var avgNAPsPerProvider = activeProviders > 0 ? Math.Round((double)totalNAPs / activeProviders, 2) : 0;

            // Calculate geographic coverage
            GeographicCoverageDto? geographicCoverage = null;
            double densityPerKm2 = 0;

            if (request.IncludeGeographicCoverage && naps.Any())
            {
                var latitudes = naps.Select(n => n.Location.Y).ToList();
                var longitudes = naps.Select(n => n.Location.X).ToList();

                var minLat = latitudes.Min();
                var maxLat = latitudes.Max();
                var minLon = longitudes.Min();
                var maxLon = longitudes.Max();

                // Approximate area calculation (simplified for small regions)
                // Using approximate conversion: 1 degree latitude ≈ 111 km, 1 degree longitude varies by latitude
                var latRange = maxLat - minLat;
                var lonRange = maxLon - minLon;
                var avgLat = (minLat + maxLat) / 2;
                var lonKmPerDegree = 111.0 * Math.Cos(avgLat * Math.PI / 180.0);
                var areaKm2 = Math.Max(latRange * 111.0 * lonRange * lonKmPerDegree, 0.01); // Prevent division by zero

                densityPerKm2 = Math.Round(totalNAPs / areaKm2, 2);

                geographicCoverage = new GeographicCoverageDto
                {
                    MinLatitude = Math.Round(minLat, 2),
                    MaxLatitude = Math.Round(maxLat, 2),
                    MinLongitude = Math.Round(minLon, 2),
                    MaxLongitude = Math.Round(maxLon, 2),
                    TotalAreaKm2 = Math.Round(areaKm2, 2)
                };
            }

            // Build market share distribution
            var marketShareDistribution = providerGroups.Select(p => new ProviderMarketShareDto
            {
                ProviderName = p.ProviderName,
                NAPCount = p.Count,
                MarketSharePercentage = Math.Round((double)p.Count / totalNAPs * 100, 2),
                ProviderColor = p.ProviderColor
            }).ToList();

            // Build provider comparison (top providers)
            var providerComparison = providerGroups.Select(p => new ProviderComparisonDto
            {
                ProviderName = p.ProviderName,
                NAPCount = p.Count,
                ProviderColor = p.ProviderColor
            }).ToList();

            // Get leading provider
            var leadingProviderData = providerGroups.FirstOrDefault();
            LeadingProviderDto? leadingProvider = null;
            if (leadingProviderData != null)
            {
                leadingProvider = new LeadingProviderDto
                {
                    ProviderName = leadingProviderData.ProviderName,
                    NAPCount = leadingProviderData.Count,
                    MarketSharePercentage = Math.Round((double)leadingProviderData.Count / totalNAPs * 100, 2)
                };
            }

            // Calculate market analysis
            var marketAnalysis = CalculateMarketAnalysis(activeProviders, marketShareDistribution, densityPerKm2);

            var response = new NAPAnalyticsResponseDto
            {
                Summary = new NAPSummaryDto
                {
                    TotalNAPs = totalNAPs,
                    ActiveProviders = activeProviders,
                    AvgNAPsPerProvider = avgNAPsPerProvider,
                    DensityPerKm2 = densityPerKm2
                },
                MarketShareDistribution = marketShareDistribution,
                ProviderComparison = providerComparison,
                LeadingProvider = leadingProvider,
                GeographicCoverage = geographicCoverage,
                MarketAnalysis = marketAnalysis
            };

            return new ApiResponse<NAPAnalyticsResponseDto>
            {
                Succeeded = true,
                Message = "NAP analytics retrieved successfully",
                Data = response
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing NAP analytics request");
            return new ApiResponse<NAPAnalyticsResponseDto>
            {
                Succeeded = false,
                Message = "An error occurred while processing NAP analytics",
                Errors = new Dictionary<string, string[]>
                {
                    { "Error", new[] { ex.Message } }
                }
            };
        }
    }

    /// <summary>
    /// Calculate market analysis metrics based on provider distribution and density
    /// </summary>
    private static MarketAnalysisDto CalculateMarketAnalysis(
        int activeProviders, 
        List<ProviderMarketShareDto> marketShare, 
        double densityPerKm2)
    {
        // Calculate competition score based on market concentration
        // Using Herfindahl-Hirschman Index (HHI) concept
        var hhi = marketShare.Sum(p => Math.Pow(p.MarketSharePercentage / 100, 2));
        
        // Convert HHI to competition score (inverted, 0-100)
        // HHI ranges from 1/n (perfect competition) to 1 (monopoly)
        // Competition score: 100 = highly competitive, 0 = monopoly
        var competitionScore = (int)Math.Round((1 - hhi) * 100);

        // Determine market concentration level
        string market;
        if (activeProviders <= 2)
        {
            market = "Monopolistic";
        }
        else if (activeProviders <= 4 || competitionScore < 40)
        {
            market = "Oligopolistic";
        }
        else if (competitionScore < 60)
        {
            market = "Moderate";
        }
        else
        {
            market = "Competitive";
        }

        // Determine coverage quality based on density
        string coverage;
        if (densityPerKm2 >= 10)
        {
            coverage = "Excellent";
        }
        else if (densityPerKm2 >= 5)
        {
            coverage = "Good";
        }
        else if (densityPerKm2 >= 2)
        {
            coverage = "Fair";
        }
        else if (densityPerKm2 >= 1)
        {
            coverage = "Limited";
        }
        else
        {
            coverage = "Poor";
        }

        return new MarketAnalysisDto
        {
            Market = market,
            CompetitionScore = competitionScore,
            Coverage = coverage
        };
    }
}
