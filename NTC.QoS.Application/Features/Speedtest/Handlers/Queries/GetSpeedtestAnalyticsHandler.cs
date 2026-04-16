using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
using NTC.QoS.Application.Features.Speedtest.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Speedtest;
using NTC.QoS.Infrastructure.Persistence;

namespace NTC.QoS.Application.Features.Speedtest.Handlers.Queries;

/// <summary>
/// Handler for speedtest analytics dashboard
/// Calculates ALL statistics on backend (not limited by pagination)
/// </summary>
public class GetSpeedtestAnalyticsHandler : IRequestHandler<GetSpeedtestAnalyticsRequest, ApiResponse<SpeedtestAnalyticsDto>>
{
    private readonly ISpeedtestRepository _speedtestRepository;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<GetSpeedtestAnalyticsHandler> _logger;

    public GetSpeedtestAnalyticsHandler(
        ISpeedtestRepository speedtestRepository,
        ApplicationDbContext context,
        ILogger<GetSpeedtestAnalyticsHandler> logger)
    {
        _speedtestRepository = speedtestRepository;
        _context = context;
        _logger = logger;
    }

    public async Task<ApiResponse<SpeedtestAnalyticsDto>> Handle(GetSpeedtestAnalyticsRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Calculating speedtest analytics with filters: RegionIds={RegionIds}, ProvinceIds={ProvinceIds}, MunicipalityIds={MunicipalityIds}, BarangayIds={BarangayIds}, StartDate={StartDate}, EndDate={EndDate}",
                request.RegionIds != null ? string.Join(",", request.RegionIds) : "null",
                request.ProvinceIds != null ? string.Join(",", request.ProvinceIds) : "null",
                request.MunicipalityIds != null ? string.Join(",", request.MunicipalityIds) : "null",
                request.BarangayIds != null ? string.Join(",", request.BarangayIds) : "null",
                request.StartDate,
                request.EndDate);

            // Build query with geographic filters
            var query = _speedtestRepository.GetEntitiesWithinBoundaries(
                _speedtestRepository.QueryAsNoTracking(),
                regionIds: request.RegionIds,
                provinceIds: request.ProvinceIds,
                municipalityIds: request.MunicipalityIds,
                barangayIds: request.BarangayIds);

            if (request.StartDate.HasValue)
            {
                query = query.Where(s => s.DateTime >= request.StartDate.Value);
            }

            if (request.EndDate.HasValue)
            {
                var endOfDay = request.EndDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(s => s.DateTime <= endOfDay);
            }

            // Fetch ALL speedtests matching filters (not paginated)
            // Project only necessary fields for performance
            var speedtests = await query
                .Select(s => new SpeedtestAnalyticsItem
                {
                    DownloadSpeed = s.DownloadSpeed,
                    UploadSpeed = s.UploadSpeed,
                    Ping = s.Ping,
                    SignalQuality = s.SignalQuality,
                    ProviderName = s.ServiceProvider.Provider.Name,
                    ProviderColor = s.ServiceProvider.Provider.Color,
                    Location = s.Location,
                    IsNoBroadbandCoverage = s.IsNoBroadbandCoverage
                })
                .ToListAsync(cancellationToken);

            _logger.LogInformation("Retrieved {Count} speedtests for analytics calculation", speedtests.Count);

            if (speedtests.Count == 0)
            {
                return new ApiResponse<SpeedtestAnalyticsDto>
                {
                    Succeeded = true,
                    Message = "No speedtests found matching the specified filters.",
                    Data = new SpeedtestAnalyticsDto()
                };
            }

            // Calculate overview statistics
            var overview = CalculateOverview(speedtests);

            // Calculate provider performance
            var providerPerformance = CalculateProviderPerformance(speedtests);

            // Calculate signal quality distribution
            var signalDistribution = CalculateSignalQualityDistribution(speedtests);

            // Calculate provider rankings
            var rankings = CalculateProviderRankings(providerPerformance);

            var analytics = new SpeedtestAnalyticsDto
            {
                Overview = overview,
                ProviderPerformance = providerPerformance,
                SignalQualityDistribution = signalDistribution,
                ProviderRankings = rankings
            };

            _logger.LogInformation("Successfully calculated speedtest analytics. Total tests: {TotalTests}, Providers: {ProviderCount}",
                overview.TotalTests, overview.ProviderCount);

            return new ApiResponse<SpeedtestAnalyticsDto>
            {
                Succeeded = true,
                Message = $"Speedtest analytics calculated successfully for {speedtests.Count} tests across {overview.ProviderCount} providers.",
                Data = analytics
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while calculating speedtest analytics");
            return new ApiResponse<SpeedtestAnalyticsDto>
            {
                Succeeded = false,
                Message = "An error occurred while calculating speedtest analytics.",
                Errors = new Dictionary<string, string[]>
                {
                    { "AnalyticsError", [ex.Message] }
                }
            };
        }
    }

    private static SpeedtestOverviewDto CalculateOverview(List<SpeedtestAnalyticsItem> speedtests)
    {
        // Exclude NBC records from speed averages — they have no meaningful speed data
        var speedRecords = speedtests.Where(s => !s.IsNoBroadbandCoverage).ToList();
        var downloadSpeeds = speedRecords.Select(s => s.DownloadSpeed).OrderBy(s => s).ToList();
        var uploadSpeeds = speedRecords.Select(s => s.UploadSpeed).OrderBy(s => s).ToList();
        var pings = speedRecords.Select(s => s.Ping).ToList();

        var avgDownload = downloadSpeeds.Count > 0 ? Math.Round(downloadSpeeds.Average(), 2) : 0;
        var avgUpload = uploadSpeeds.Count > 0 ? Math.Round(uploadSpeeds.Average(), 2) : 0;
        var medianDownload = CalculateMedian(downloadSpeeds);
        var medianUpload = CalculateMedian(uploadSpeeds);
        var avgPing = pings.Count > 0 ? Math.Round(pings.Average(), 2) : 0;

        // Group by provider to count unique providers (includes NBC records — providers were present in the area)
        var providers = speedtests.GroupBy(s => s.ProviderName).ToList();
        var providerCount = providers.Count;

        // Determine best provider (highest median download, excluding NBC records which have no speed data)
        var bestProvider = speedRecords
            .GroupBy(s => s.ProviderName)
            .Select(g => new
            {
                Name = g.Key,
                MedianDownload = CalculateMedian(g.Select(s => s.DownloadSpeed).OrderBy(s => s).ToList())
            })
            .OrderByDescending(p => p.MedianDownload)
            .FirstOrDefault();

        // Determine performance rating
        var performanceRating = avgDownload switch
        {
            >= 100 => "Excellent",
            >= 50 => "Good",
            >= 25 => "Fair",
            _ => "Poor"
        };

        // Determine competition level
        var competitionLevel = providerCount switch
        {
            >= 4 => "High",
            >= 2 => "Medium",
            _ => "Low"
        };

        // Calculate coverage area (estimate from unique locations)
        var uniqueLocations = speedtests
            .Where(s => s.Location != null)
            .Select(s => new { Lat = s.Location.Coordinate.Y, Lng = s.Location.Coordinate.X })
            .Distinct()
            .ToList();

        // Simple bounding box area calculation (rough estimate)
        double coverageAreaKm2 = 0;
        if (uniqueLocations.Count > 1)
        {
            var latitudes = uniqueLocations.Select(l => l.Lat).ToList();
            var longitudes = uniqueLocations.Select(l => l.Lng).ToList();
            
            var latRange = latitudes.Max() - latitudes.Min();
            var lngRange = longitudes.Max() - longitudes.Min();
            
            // Rough conversion: 1 degree latitude ≈ 111 km, 1 degree longitude ≈ 111 km * cos(latitude)
            var avgLat = latitudes.Average();
            var latKm = latRange * 111;
            var lngKm = lngRange * 111 * Math.Cos(avgLat * Math.PI / 180);
            
            coverageAreaKm2 = Math.Round(latKm * lngKm, 2);
        }

        return new SpeedtestOverviewDto
        {
            TotalTests = speedtests.Count,
            AverageDownloadMbps = avgDownload,
            AverageUploadMbps = avgUpload,
            MedianDownloadMbps = medianDownload,
            MedianUploadMbps = medianUpload,
            AveragePingMs = avgPing,
            ProviderCount = providerCount,
            CoverageAreaKm2 = coverageAreaKm2,
            PerformanceRating = performanceRating,
            BestProvider = bestProvider?.Name ?? string.Empty,
            CompetitionLevel = competitionLevel
        };
    }

    private static List<ProviderPerformanceDto> CalculateProviderPerformance(List<SpeedtestAnalyticsItem> speedtests)
    {
        // Exclude NBC records from speed calculations
        var speedRecords = speedtests.Where(s => !s.IsNoBroadbandCoverage).ToList();
        var providerGroups = speedRecords.GroupBy(s => new { Name = s.ProviderName, Color = s.ProviderColor });

        var performance = providerGroups.Select(g =>
        {
            var downloadSpeeds = g.Select(s => s.DownloadSpeed).OrderBy(s => s).ToList();
            var uploadSpeeds = g.Select(s => s.UploadSpeed).OrderBy(s => s).ToList();

            return new ProviderPerformanceDto
            {
                ProviderName = g.Key.Name,
                ProviderColor = g.Key.Color ?? string.Empty,
                AverageDownloadMbps = Math.Round(downloadSpeeds.Average(), 2),
                AverageUploadMbps = Math.Round(uploadSpeeds.Average(), 2),
                MedianDownloadMbps = CalculateMedian(downloadSpeeds),
                MedianUploadMbps = CalculateMedian(uploadSpeeds),
                TestCount = g.Count()
            };
        })
        .OrderByDescending(p => p.MedianDownloadMbps)
        .ToList();

        return performance;
    }

    private static SignalQualityDistributionDto CalculateSignalQualityDistribution(List<SpeedtestAnalyticsItem> speedtests)
    {
        var totalTests = speedtests.Count;

        var excellentCount = speedtests.Count(s => s.SignalQuality == "Excellent");
        var goodCount = speedtests.Count(s => s.SignalQuality == "Good");
        var averageCount = speedtests.Count(s => s.SignalQuality == "Average");
        var weakCount = speedtests.Count(s => s.SignalQuality == "Weak");
        var veryWeakCount = speedtests.Count(s => s.SignalQuality == "Very Weak");

        return new SignalQualityDistributionDto
        {
            TotalTests = totalTests,
            ExcellentCount = excellentCount,
            GoodCount = goodCount,
            AverageCount = averageCount,
            WeakCount = weakCount,
            VeryWeakCount = veryWeakCount,
            ExcellentPercent = totalTests > 0 ? Math.Round((decimal)excellentCount / totalTests * 100, 1) : 0,
            GoodPercent = totalTests > 0 ? Math.Round((decimal)goodCount / totalTests * 100, 1) : 0,
            AveragePercent = totalTests > 0 ? Math.Round((decimal)averageCount / totalTests * 100, 1) : 0,
            WeakPercent = totalTests > 0 ? Math.Round((decimal)weakCount / totalTests * 100, 1) : 0,
            VeryWeakPercent = totalTests > 0 ? Math.Round((decimal)veryWeakCount / totalTests * 100, 1) : 0
        };
    }

    private static List<ProviderRankingDto> CalculateProviderRankings(List<ProviderPerformanceDto> providers)
    {
        var rankings = providers.Select(p =>
        {
            // Calculate score: weighted formula (download speed is most important)
            // Score = (Download * 0.6) + (Upload * 0.4)
            // Normalized to 0-100 scale
            var downloadScore = (double)p.MedianDownloadMbps;
            var uploadScore = (double)p.MedianUploadMbps;
            
            var rawScore = (downloadScore * 0.6) + (uploadScore * 0.4);
            var score = Math.Min(100, (int)Math.Round(rawScore));

            // Determine badge
            var badge = score switch
            {
                >= 80 => "Best",
                >= 60 => "Great",
                >= 40 => "Good",
                _ => "Fair"
            };

            return new ProviderRankingDto
            {
                ProviderName = p.ProviderName,
                ProviderColor = p.ProviderColor,
                TestCount = p.TestCount,
                DownloadMbps = p.MedianDownloadMbps,
                UploadMbps = p.MedianUploadMbps,
                AveragePingMs = 0, // Will be calculated if needed
                Score = score,
                Badge = badge
            };
        })
        .OrderByDescending(r => r.Score)
        .Select((r, index) => 
        {
            r.Rank = index + 1;
            return r;
        })
        .ToList();

        return rankings;
    }

    /// <summary>
    /// Calculate median from ordered list
    /// </summary>
    private static decimal CalculateMedian(List<decimal> orderedValues)
    {
        if (orderedValues.Count == 0)
            return 0;

        int count = orderedValues.Count;
        if (count % 2 == 0)
        {
            return Math.Round((orderedValues[count / 2 - 1] + orderedValues[count / 2]) / 2, 2);
        }
        else
        {
            return Math.Round(orderedValues[count / 2], 2);
        }
    }

    /// <summary>
    /// Internal class for speedtest analytics item projection
    /// </summary>
    private class SpeedtestAnalyticsItem
    {
        public decimal DownloadSpeed { get; set; }
        public decimal UploadSpeed { get; set; }
        public decimal Ping { get; set; }
        public string? SignalQuality { get; set; }
        public string ProviderName { get; set; } = string.Empty;
        public string? ProviderColor { get; set; }
        public Point Location { get; set; } = null!;
        public bool IsNoBroadbandCoverage { get; set; }
    }
}
