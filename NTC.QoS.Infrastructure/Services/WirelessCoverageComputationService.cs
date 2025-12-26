using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Infrastructure.Persistence;

namespace NTC.QoS.Infrastructure.Services;

/// <summary>
/// Service for computing wireless coverage data to be cached in ConnectivityAssessment.
/// Optimized for wireless broadband report performance.
/// </summary>
public interface IWirelessCoverageComputationService
{
    /// <summary>
    /// Computes wireless coverage summary for a barangay with provider-level breakdown.
    /// Returns JSON string to be stored in ConnectivityAssessment.WirelessCoverageJson.
    /// </summary>
    Task<string?> ComputeBarangayWirelessCoverageAsync(
        Guid barangayId,
        Geometry? boundary,
        Point center,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// OPTIMIZED: Computes wireless coverage for multiple barangays using bulk queries.
    /// Queries cellulars/speedtests for the entire region once, then groups in-memory.
    /// Returns dictionary of barangayId -> JSON string for batch processing.
    /// </summary>
    Task<Dictionary<Guid, string>> ComputeBatchWirelessCoverageAsync(
        List<BarangayProjection> barangays,
        Geometry? regionBoundary,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Projection for barangay data needed for wireless coverage computation
/// </summary>
public class BarangayProjection
{
    public Guid Id { get; set; }
    public string PSGCCode { get; set; } = string.Empty;
    public Geometry? Boundaries { get; set; }
}

public class WirelessCoverageComputationService : IWirelessCoverageComputationService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<WirelessCoverageComputationService> _logger;

    public WirelessCoverageComputationService(
        ApplicationDbContext context,
        ILogger<WirelessCoverageComputationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<string?> ComputeBarangayWirelessCoverageAsync(
        Guid barangayId,
        Geometry? boundary,
        Point center,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (boundary == null)
            {
                _logger.LogWarning("No boundary provided for barangay {BarangayId}, skipping wireless coverage computation", barangayId);
                return null;
            }

            // Get all wireless providers with coverage in this barangay
            var providerCoverageList = new List<BarangayWirelessCoverageDto>();

            // Query cellulars within barangay boundary - optimized with projection
            var cellularsInBarangay = await _context.Cellulars
                .AsNoTracking()
                .Where(c => c.CellSector != null && c.CellSector.Cell != null && c.CellSector.Cell.Location.Within(boundary))
                .Select(c => new
                {
                    c.ServiceProviderId,
                    ProviderName = c.ServiceProvider!.Provider.Name,
                    Generation = c.Technology!.Generation!.Name,
                    c.CreatedAt,
                    c.UpdatedAt
                })
                .ToListAsync(cancellationToken);

            // Query wireless validations for this barangay - optimized with projection
            var wirelessValidationsInBarangay = await _context.WirelessValidations
                .AsNoTracking()
                .Where(wv => wv.BarangayId == barangayId && !wv.IsMarginal)
                .Select(wv => new
                {
                    wv.ServiceProviderId,
                    ProviderName = wv.ServiceProvider!.Provider.Name,
                    Generation = wv.Technology!.Generation!.Name,
                    wv.CreatedAt,
                    wv.UpdatedAt
                })
                .ToListAsync(cancellationToken);

            // Query speedtests within barangay boundary - optimized with projection
            var speedtestsInBarangay = await _context.Speedtests
                .AsNoTracking()
                .Where(s => s.Location.Within(boundary))
                .Select(s => new
                {
                    s.ServiceProviderId,
                    ProviderName = s.ServiceProvider!.Provider.Name,
                    GenerationName = s.Generation!.Name,
                    s.DownloadSpeed,
                    s.UploadSpeed,
                    s.CreatedAt,
                    s.UpdatedAt
                })
                .ToListAsync(cancellationToken);

            // Group by provider (from cellulars, wireless validations, AND speedtests)
            var providerIds = cellularsInBarangay.Select(c => c.ServiceProviderId)
                .Concat(wirelessValidationsInBarangay.Select(wv => wv.ServiceProviderId))
                .Concat(speedtestsInBarangay.Select(s => s.ServiceProviderId))
                .Distinct()
                .ToList();

            foreach (var providerId in providerIds)
            {
                var providerCellulars = cellularsInBarangay.Where(c => c.ServiceProviderId == providerId).ToList();
                var providerValidations = wirelessValidationsInBarangay.Where(wv => wv.ServiceProviderId == providerId).ToList();
                var providerSpeedtests = speedtestsInBarangay.Where(s => s.ServiceProviderId == providerId).ToList();

                var providerName = providerCellulars.FirstOrDefault()?.ProviderName 
                    ?? providerValidations.FirstOrDefault()?.ProviderName
                    ?? providerSpeedtests.FirstOrDefault()?.ProviderName 
                    ?? "Unknown";

                // Count technologies from BOTH cellulars AND wireless validations (cross-checking)
                var cellularGenerations = providerCellulars.Select(c => c.Generation).ToList();
                var validationGenerations = providerValidations.Select(wv => wv.Generation).ToList();
                var allGenerations = cellularGenerations.Concat(validationGenerations).ToList();

                var count5G = allGenerations.Count(g => g == "5G" || g == "5G NR");
                var count4G = allGenerations.Count(g => g == "4G" || g == "LTE");
                var count3G = allGenerations.Count(g => g == "3G" || g == "UMTS" || g == "HSPA" || g == "HSPA+");
                var count2G = allGenerations.Count(g => g == "2G" || g == "GSM" || g == "EDGE");

                // Calculate speedtest statistics
                decimal? avgDownload = null;
                decimal? avgUpload = null;
                decimal? medianDownload = null;
                decimal? medianUpload = null;
                string? medianTechnology = null;

                if (providerSpeedtests.Any())
                {
                    avgDownload = (decimal)providerSpeedtests.Average(s => (double)s.DownloadSpeed);
                    avgUpload = (decimal)providerSpeedtests.Average(s => (double)s.UploadSpeed);

                    var downloadSpeeds = providerSpeedtests.Select(s => s.DownloadSpeed).OrderBy(s => s).ToList();
                    var uploadSpeeds = providerSpeedtests.Select(s => s.UploadSpeed).OrderBy(s => s).ToList();

                    medianDownload = CalculateMedian(downloadSpeeds);
                    medianUpload = CalculateMedian(uploadSpeeds);

                    // Get median technology from speedtest closest to median download speed
                    if (medianDownload.HasValue && downloadSpeeds.Any())
                    {
                        var medianIndex = downloadSpeeds.Count / 2;
                        var medianSpeedValue = downloadSpeeds[medianIndex];
                        var medianSpeedtest = providerSpeedtests.FirstOrDefault(s => s.DownloadSpeed == medianSpeedValue);
                        medianTechnology = medianSpeedtest?.GenerationName;
                    }
                }

                // Get timestamp metadata
                var lastCellularUpdate = providerCellulars.Any() 
                    ? providerCellulars.Max(c => c.UpdatedAt > c.CreatedAt ? c.UpdatedAt : c.CreatedAt)
                    : DateTime.MinValue;

                var lastSpeedtestUpdate = providerSpeedtests.Any()
                    ? providerSpeedtests.Max(s => s.UpdatedAt > s.CreatedAt ? s.UpdatedAt : s.CreatedAt)
                    : (DateTime?)null;

                providerCoverageList.Add(new BarangayWirelessCoverageDto
                {
                    BarangayId = barangayId,
                    ProviderId = providerId,
                    ProviderName = providerName,
                    Has2G = count2G > 0,
                    Has3G = count3G > 0,
                    Has4G = count4G > 0,
                    Has5G = count5G > 0,
                    Count2G = count2G,
                    Count3G = count3G,
                    Count4G = count4G,
                    Count5G = count5G,
                    TotalCellularCount = providerCellulars.Count,
                    SpeedtestCount = providerSpeedtests.Count,
                    AverageDownloadSpeedMbps = avgDownload,
                    AverageUploadSpeedMbps = avgUpload,
                    MedianDownloadSpeedMbps = medianDownload,
                    MedianUploadSpeedMbps = medianUpload,
                    MedianTechnology = medianTechnology,
                    LastCellularUpdate = lastCellularUpdate,
                    LastSpeedtestUpdate = lastSpeedtestUpdate,
                    ComputedDate = DateTime.UtcNow
                });
            }

            // Create summary
            var summary = new BarangayWirelessSummaryDto
            {
                BarangayId = barangayId,
                TotalWirelessProviders = providerCoverageList.Count,
                TotalCellularCount = providerCoverageList.Sum(p => p.TotalCellularCount),
                TotalSpeedtestCount = providerCoverageList.Sum(p => p.SpeedtestCount),
                Providers = providerCoverageList,
                LastUpdated = DateTime.UtcNow
            };

            // Determine overall median technology across all providers
            var allMedianTechs = providerCoverageList
                .Where(p => p.MedianTechnology != null)
                .Select(p => p.MedianTechnology!)
                .ToList();

            if (allMedianTechs.Any())
            {
                // Get the most common median technology
                summary.OverallMedianTechnology = allMedianTechs
                    .GroupBy(t => t)
                    .OrderByDescending(g => g.Count())
                    .First()
                    .Key;
            }

            // Determine best available technology
            if (providerCoverageList.Any(p => p.Has5G))
                summary.BestTechnology = "5G";
            else if (providerCoverageList.Any(p => p.Has4G))
                summary.BestTechnology = "4G/LTE";
            else if (providerCoverageList.Any(p => p.Has3G))
                summary.BestTechnology = "3G";
            else if (providerCoverageList.Any(p => p.Has2G))
                summary.BestTechnology = "2G";

            // Serialize to JSON
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false // Compact JSON for storage
            };

            return JsonSerializer.Serialize(summary, options);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error computing wireless coverage for barangay {BarangayId}", barangayId);
            return null;
        }
    }

    private static decimal CalculateMedian(List<decimal> values)
    {
        if (values.Count == 0) return 0;

        int mid = values.Count / 2;
        return values.Count % 2 == 0
            ? Math.Round((values[mid - 1] + values[mid]) / 2, 2)
            : Math.Round(values[mid], 2);
    }

    /// <summary>
    /// OPTIMIZED: Computes wireless coverage for multiple barangays using bulk queries.
    /// Strategy: Query all cellulars/speedtests for the region once, then group by barangay in-memory.
    /// Performance: ~20-30x faster than individual queries (2 queries vs 4,246 queries for 2,123 barangays).
    /// </summary>
    public async Task<Dictionary<Guid, string>> ComputeBatchWirelessCoverageAsync(
        List<BarangayProjection> barangays,
        Geometry? regionBoundary,
        CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<Guid, string>();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            if (regionBoundary == null || !barangays.Any())
            {
                _logger.LogWarning("No region boundary or barangays provided for batch wireless coverage computation");
                return result;
            }

            var barangayCount = barangays.Count;
            _logger.LogInformation("Starting batch wireless coverage computation for {Count} barangays", barangayCount);

            // Step 1: Load ALL cellulars for the region in ONE query
            _logger.LogInformation("Step 1: Loading cellulars for region...");
            var cellularLoadStopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            var allCellulars = await _context.Cellulars
                .AsNoTracking()
                .Where(c => c.CellSector != null && 
                           c.CellSector.Cell != null && 
                           c.CellSector.Cell.Location != null &&
                           c.CellSector.Cell.Location.Within(regionBoundary) &&
                           c.ServiceProvider != null &&
                           c.ServiceProvider.Provider != null &&
                           c.Technology != null &&
                           c.Technology.Generation != null)
                .Select(c => new
                {
                    c.ServiceProviderId,
                    ProviderName = c.ServiceProvider!.Provider!.Name,
                    Generation = c.Technology!.Generation!.Name,
                    Location = c.CellSector!.Cell!.Location,
                    c.CreatedAt,
                    c.UpdatedAt
                })
                .ToListAsync(cancellationToken);

            cellularLoadStopwatch.Stop();
            _logger.LogInformation("Loaded {Count} cellulars in {ElapsedMs}ms", 
                allCellulars.Count, cellularLoadStopwatch.ElapsedMilliseconds);

            // Step 2: Load ALL wireless validations for these barangays in ONE query
            _logger.LogInformation("Step 2: Loading wireless validations for barangays...");
            var validationLoadStopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            var barangayIds = barangays.Select(b => b.Id).ToList();
            var allWirelessValidations = await _context.WirelessValidations
                .AsNoTracking()
                .Where(wv => barangayIds.Contains(wv.BarangayId) && 
                            !wv.IsMarginal &&
                            wv.ServiceProvider != null &&
                            wv.Technology != null &&
                            wv.Technology.Generation != null)
                .Select(wv => new
                {
                    wv.BarangayId,
                    wv.ServiceProviderId,
                    ProviderName = wv.ServiceProvider!.Provider.Name,
                    Generation = wv.Technology!.Generation!.Name,
                    wv.CreatedAt,
                    wv.UpdatedAt
                })
                .ToListAsync(cancellationToken);

            validationLoadStopwatch.Stop();
            _logger.LogInformation("Loaded {Count} wireless validations in {ElapsedMs}ms", 
                allWirelessValidations.Count, validationLoadStopwatch.ElapsedMilliseconds);

            // Step 3: Load ALL speedtests for the region in ONE query
            _logger.LogInformation("Step 3: Loading speedtests for region...");
            var speedtestLoadStopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            var allSpeedtests = await _context.Speedtests
                .AsNoTracking()
                .Where(s => s.Location.Within(regionBoundary))
                .Select(s => new
                {
                    s.ServiceProviderId,
                    ProviderName = s.ServiceProvider!.Provider.Name,
                    GenerationName = s.Generation!.Name,
                    s.DownloadSpeed,
                    s.UploadSpeed,
                    s.Location,
                    s.CreatedAt,
                    s.UpdatedAt
                })
                .ToListAsync(cancellationToken);

            speedtestLoadStopwatch.Stop();
            _logger.LogInformation("Loaded {Count} speedtests in {ElapsedMs}ms", 
                allSpeedtests.Count, speedtestLoadStopwatch.ElapsedMilliseconds);

            // Step 4: Group cellulars, wireless validations, and speedtests by barangay in-memory
            _logger.LogInformation("Step 4: Grouping data by barangay in-memory...");
            var groupingStopwatch = System.Diagnostics.Stopwatch.StartNew();

            var cellularsByBarangay = new Dictionary<Guid, List<(Guid ServiceProviderId, string ProviderName, string Generation, DateTime CreatedAt, DateTime UpdatedAt)>>();
            var validationsByBarangay = new Dictionary<Guid, List<(Guid ServiceProviderId, string ProviderName, string Generation, DateTime CreatedAt, DateTime UpdatedAt)>>();
            var speedtestsByBarangay = new Dictionary<Guid, List<(Guid ServiceProviderId, string ProviderName, string GenerationName, decimal DownloadSpeed, decimal UploadSpeed, DateTime CreatedAt, DateTime UpdatedAt)>>();

            // Initialize dictionaries
            foreach (var barangay in barangays)
            {
                cellularsByBarangay[barangay.Id] = new List<(Guid, string, string, DateTime, DateTime)>();
                validationsByBarangay[barangay.Id] = new List<(Guid, string, string, DateTime, DateTime)>();
                speedtestsByBarangay[barangay.Id] = new List<(Guid, string, string, decimal, decimal, DateTime, DateTime)>();
            }

            // Group cellulars
            foreach (var cellular in allCellulars)
            {
                foreach (var barangay in barangays)
                {
                    if (barangay.Boundaries != null && cellular.Location.Within(barangay.Boundaries))
                    {
                        cellularsByBarangay[barangay.Id].Add((
                            cellular.ServiceProviderId,
                            cellular.ProviderName,
                            cellular.Generation,
                            cellular.CreatedAt,
                            cellular.UpdatedAt
                        ));
                        break; // Cellular can only belong to one barangay
                    }
                }
            }

            // Group wireless validations (already have BarangayId, no spatial check needed)
            foreach (var validation in allWirelessValidations)
            {
                validationsByBarangay[validation.BarangayId].Add((
                    validation.ServiceProviderId,
                    validation.ProviderName,
                    validation.Generation,
                    validation.CreatedAt,
                    validation.UpdatedAt
                ));
            }

            // Group speedtests
            foreach (var speedtest in allSpeedtests)
            {
                foreach (var barangay in barangays)
                {
                    if (barangay.Boundaries != null && speedtest.Location.Within(barangay.Boundaries))
                    {
                        speedtestsByBarangay[barangay.Id].Add((
                            speedtest.ServiceProviderId,
                            speedtest.ProviderName,
                            speedtest.GenerationName,
                            speedtest.DownloadSpeed,
                            speedtest.UploadSpeed,
                            speedtest.CreatedAt,
                            speedtest.UpdatedAt
                        ));
                        break; // Speedtest can only belong to one barangay
                    }
                }
            }

            groupingStopwatch.Stop();
            _logger.LogInformation("Grouped data in {ElapsedMs}ms", groupingStopwatch.ElapsedMilliseconds);

            // Step 5: Process each barangay to compute coverage
            _logger.LogInformation("Step 5: Computing coverage for each barangay...");
            var computationStopwatch = System.Diagnostics.Stopwatch.StartNew();

            foreach (var barangay in barangays)
            {
                var barangayCellulars = cellularsByBarangay[barangay.Id];
                var barangayValidations = validationsByBarangay[barangay.Id];
                var barangaySpeedtests = speedtestsByBarangay[barangay.Id];

                // Compute coverage for this barangay
                var coverage = ComputeCoverageForBarangay(
                    barangay.Id,
                    barangay.PSGCCode,
                    barangayCellulars,
                    barangayValidations,
                    barangaySpeedtests
                );

                result[barangay.Id] = coverage;
            }

            computationStopwatch.Stop();
            _logger.LogInformation("Computed coverage for {Count} barangays in {ElapsedMs}ms (avg {AvgMs}ms/barangay)", 
                barangayCount, computationStopwatch.ElapsedMilliseconds,
                barangayCount > 0 ? computationStopwatch.ElapsedMilliseconds / barangayCount : 0);

            stopwatch.Stop();
            _logger.LogInformation(
                "Batch wireless coverage computation completed in {TotalMs}ms. " +
                "Cellulars: {CellularCount}, Speedtests: {SpeedtestCount}, Barangays: {BarangayCount}",
                stopwatch.ElapsedMilliseconds, allCellulars.Count, allSpeedtests.Count, barangayCount);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in batch wireless coverage computation");
            return result;
        }
    }

    /// <summary>
    /// Computes wireless coverage for a single barangay from in-memory grouped data
    /// </summary>
    private string ComputeCoverageForBarangay(
        Guid barangayId,
        string barangayPsgcCode,
        List<(Guid ServiceProviderId, string ProviderName, string Generation, DateTime CreatedAt, DateTime UpdatedAt)> cellulars,
        List<(Guid ServiceProviderId, string ProviderName, string Generation, DateTime CreatedAt, DateTime UpdatedAt)> validations,
        List<(Guid ServiceProviderId, string ProviderName, string GenerationName, decimal DownloadSpeed, decimal UploadSpeed, DateTime CreatedAt, DateTime UpdatedAt)> speedtests)
    {
        var providerCoverageList = new List<BarangayWirelessCoverageDto>();

        // Group by provider (cellulars + validations + speedtests)
        var providerIds = cellulars.Select(c => c.ServiceProviderId)
            .Concat(validations.Select(v => v.ServiceProviderId))
            .Concat(speedtests.Select(s => s.ServiceProviderId))
            .Distinct()
            .ToList();

        foreach (var providerId in providerIds)
        {
            var providerCellulars = cellulars.Where(c => c.ServiceProviderId == providerId).ToList();
            var providerValidations = validations.Where(v => v.ServiceProviderId == providerId).ToList();
            var providerSpeedtests = speedtests.Where(s => s.ServiceProviderId == providerId).ToList();

            var providerName = providerCellulars.FirstOrDefault().ProviderName 
                ?? providerValidations.FirstOrDefault().ProviderName
                ?? providerSpeedtests.FirstOrDefault().ProviderName 
                ?? "Unknown";

            // Cross-check technologies from cellulars AND wireless validations
            var allGenerations = providerCellulars.Select(c => c.Generation)
                .Concat(providerValidations.Select(v => v.Generation))
                .ToList();

            var count5G = allGenerations.Count(g => g == "5G" || g == "5G NR");
            var count4G = allGenerations.Count(g => g == "4G" || g == "LTE");
            var count3G = allGenerations.Count(g => g == "3G" || g == "UMTS" || g == "HSPA" || g == "HSPA+");
            var count2G = allGenerations.Count(g => g == "2G" || g == "GSM" || g == "EDGE");

            // Calculate speedtest statistics
            decimal? avgDownload = null;
            decimal? avgUpload = null;
            decimal? medianDownload = null;
            decimal? medianUpload = null;
            string? medianTechnology = null;

            if (providerSpeedtests.Any())
            {
                avgDownload = (decimal)providerSpeedtests.Average(s => (double)s.DownloadSpeed);
                avgUpload = (decimal)providerSpeedtests.Average(s => (double)s.UploadSpeed);

                var downloadSpeeds = providerSpeedtests.Select(s => s.DownloadSpeed).OrderBy(s => s).ToList();
                var uploadSpeeds = providerSpeedtests.Select(s => s.UploadSpeed).OrderBy(s => s).ToList();

                medianDownload = CalculateMedian(downloadSpeeds);
                medianUpload = CalculateMedian(uploadSpeeds);

                // Get median technology from speedtest closest to median download speed
                if (medianDownload.HasValue && downloadSpeeds.Any())
                {
                    var medianIndex = downloadSpeeds.Count / 2;
                    var medianSpeedValue = downloadSpeeds[medianIndex];
                    var medianSpeedtest = providerSpeedtests.FirstOrDefault(s => s.DownloadSpeed == medianSpeedValue);
                    medianTechnology = medianSpeedtest.GenerationName;
                }
            }

            // Get timestamp metadata
            var lastCellularUpdate = providerCellulars.Any() 
                ? providerCellulars.Max(c => c.UpdatedAt > c.CreatedAt ? c.UpdatedAt : c.CreatedAt)
                : DateTime.MinValue;

            var lastValidationUpdate = providerValidations.Any()
                ? providerValidations.Max(v => v.UpdatedAt > v.CreatedAt ? v.UpdatedAt : v.CreatedAt)
                : DateTime.MinValue;

            var lastSpeedtestUpdate = providerSpeedtests.Any()
                ? providerSpeedtests.Max(s => s.UpdatedAt > s.CreatedAt ? s.UpdatedAt : s.CreatedAt)
                : (DateTime?)null;

            providerCoverageList.Add(new BarangayWirelessCoverageDto
            {
                BarangayId = barangayId,
                ProviderId = providerId,
                ProviderName = providerName,
                Has2G = count2G > 0,
                Has3G = count3G > 0,
                Has4G = count4G > 0,
                Has5G = count5G > 0,
                Count2G = count2G,
                Count3G = count3G,
                Count4G = count4G,
                Count5G = count5G,
                TotalCellularCount = providerCellulars.Count,
                WirelessValidationCount = providerValidations.Count,
                SpeedtestCount = providerSpeedtests.Count,
                AverageDownloadSpeedMbps = avgDownload,
                AverageUploadSpeedMbps = avgUpload,
                MedianDownloadSpeedMbps = medianDownload,
                MedianUploadSpeedMbps = medianUpload,
                MedianTechnology = medianTechnology,
                LastCellularUpdate = lastCellularUpdate,
                LastValidationUpdate = lastValidationUpdate,
                LastSpeedtestUpdate = lastSpeedtestUpdate,
                ComputedDate = DateTime.UtcNow
            });
        }

        // Create summary
        var summary = new BarangayWirelessSummaryDto
        {
            BarangayId = barangayId,
            TotalWirelessProviders = providerCoverageList.Count,
            TotalCellularCount = providerCoverageList.Sum(p => p.TotalCellularCount),
            TotalWirelessValidationCount = providerCoverageList.Sum(p => p.WirelessValidationCount),
            TotalSpeedtestCount = providerCoverageList.Sum(p => p.SpeedtestCount),
            Providers = providerCoverageList,
            LastUpdated = DateTime.UtcNow
        };

        // Determine overall median technology across all providers
        var allMedianTechs = providerCoverageList
            .Where(p => p.MedianTechnology != null)
            .Select(p => p.MedianTechnology!)
            .ToList();

        if (allMedianTechs.Any())
        {
            summary.OverallMedianTechnology = allMedianTechs
                .GroupBy(t => t)
                .OrderByDescending(g => g.Count())
                .First()
                .Key;
        }

        // Determine best available technology
        if (providerCoverageList.Any(p => p.Has5G))
            summary.BestTechnology = "5G";
        else if (providerCoverageList.Any(p => p.Has4G))
            summary.BestTechnology = "4G/LTE";
        else if (providerCoverageList.Any(p => p.Has3G))
            summary.BestTechnology = "3G";
        else if (providerCoverageList.Any(p => p.Has2G))
            summary.BestTechnology = "2G";

        // Serialize to JSON
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false // Compact JSON for storage
        };

        return JsonSerializer.Serialize(summary, options);
    }
}
