using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
using NetTopologySuite.Index.Strtree;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Analytics;
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
                    TechnologyStandard = c.Technology!.Standard,
                    c.CreatedAt,
                    c.UpdatedAt
                })
                .ToListAsync(cancellationToken);

            // Query wireless validations for this barangay - includes marginal for complete inventory
            var wirelessValidationsInBarangay = await _context.WirelessValidations
                .AsNoTracking()
                .Where(wv => wv.BarangayId == barangayId)
                .Select(wv => new
                {
                    wv.ServiceProviderId,
                    ProviderName = wv.ServiceProvider!.Provider.Name,
                    Generation = wv.Technology!.Generation!.Name,
                    TechnologyStandard = wv.Technology!.Standard,
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

                // CROSS-CHECKING: Track unique technologies from all sources (cellulars, validations, speedtests)
                // Uses TechnologyStandard when available to distinguish LTE vs FDD-LTE vs TDD-LTE, etc.
                var uniqueTechnologies = new HashSet<string>();

                // Add cellulars (use TechnologyStandard as key, or Generation if standard is empty)
                foreach (var c in providerCellulars)
                {
                    var techKey = !string.IsNullOrEmpty(c.TechnologyStandard) ? c.TechnologyStandard : c.Generation;
                    uniqueTechnologies.Add(techKey);
                }

                // Add wireless validations
                foreach (var v in providerValidations)
                {
                    var techKey = !string.IsNullOrEmpty(v.TechnologyStandard) ? v.TechnologyStandard : v.Generation;
                    uniqueTechnologies.Add(techKey);
                }

                // Add speedtests (only have generation, no technology standard)
                foreach (var s in providerSpeedtests)
                {
                    uniqueTechnologies.Add(s.GenerationName);
                }

                // Count unique technologies per generation (e.g., LTE + FDD-LTE + TDD-LTE = 3 unique 4G techs)
                var generationCounts = new Dictionary<string, int>();
                foreach (var tech in uniqueTechnologies)
                {
                    var normalizedGen = NormalizeGeneration(tech);
                    if (!string.IsNullOrEmpty(normalizedGen))
                        generationCounts[normalizedGen] = generationCounts.GetValueOrDefault(normalizedGen) + 1;
                }

                var count5G = generationCounts.GetValueOrDefault("5G");
                var count4G = generationCounts.GetValueOrDefault("4G");
                var count3G = generationCounts.GetValueOrDefault("3G");
                var count2G = generationCounts.GetValueOrDefault("2G");

                // Calculate TechnologyBandBreakdownDto-compatible properties
                var totalTechnologyCount = count2G + count3G + count4G + count5G;
                var modernCoverageRatio = totalTechnologyCount > 0
                    ? (double)(count4G + count5G) / totalTechnologyCount
                    : 0.0;
                var hasModernCoverage = count4G > 0 || count5G > 0;
                var isLegacyDominated = (count2G + count3G) > (count4G + count5G) && hasModernCoverage;

                // Determine dominant generation
                string? dominantGeneration = null;
                if (generationCounts.Any())
                {
                    var maxCount = generationCounts.Max(kvp => kvp.Value);
                    dominantGeneration = generationCounts.FirstOrDefault(kvp => kvp.Value == maxCount).Key;
                }

                // Build available generations list
                var availableGenerations = new List<string>();
                if (count2G > 0) availableGenerations.Add("2G");
                if (count3G > 0) availableGenerations.Add("3G");
                if (count4G > 0) availableGenerations.Add("4G");
                if (count5G > 0) availableGenerations.Add("5G");

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
                    TotalTechnologyCount = totalTechnologyCount,
                    ModernCoverageRatio = modernCoverageRatio,
                    HasModernCoverage = hasModernCoverage,
                    IsLegacyDominated = isLegacyDominated,
                    DominantGeneration = dominantGeneration,
                    AvailableGenerations = availableGenerations,
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
    /// OPTIMIZED: Computes wireless coverage using R-Tree spatial indexing.
    /// Performance: 50-100x faster than nested loops. Handles 1,000+ barangays without timeout.
    /// Strategy: Build R-Tree index for barangays, query for spatial candidates, validate containment.
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
            _logger.LogInformation("Starting OPTIMIZED batch wireless coverage computation for {Count} barangays using R-Tree", barangayCount);

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
                    TechnologyStandard = c.Technology!.Standard,
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
                            wv.ServiceProvider != null &&
                            wv.Technology != null &&
                            wv.Technology.Generation != null)
                .Select(wv => new
                {
                    wv.BarangayId,
                    wv.ServiceProviderId,
                    ProviderName = wv.ServiceProvider!.Provider.Name,
                    Generation = wv.Technology!.Generation!.Name,
                    TechnologyStandard = wv.Technology!.Standard,
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

            // Step 4: Build R-Tree spatial index for barangays (OPTIMIZATION: O(n log n) instead of O(n²))
            _logger.LogInformation("Step 4: Building R-Tree spatial index for {Count} barangays...", barangayCount);
            var indexBuildStopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            var barangayTree = new STRtree<Guid>();
            var barangayLookup = new Dictionary<Guid, BarangayProjection>();
            
            foreach (var barangay in barangays)
            {
                if (barangay.Boundaries != null)
                {
                    barangayTree.Insert(barangay.Boundaries.EnvelopeInternal, barangay.Id);
                    barangayLookup[barangay.Id] = barangay;
                }
            }
            
            barangayTree.Build(); // Build index for optimal query performance
            indexBuildStopwatch.Stop();
            _logger.LogInformation("R-Tree index built in {ElapsedMs}ms", indexBuildStopwatch.ElapsedMilliseconds);

            // Step 5: Group data by barangay using R-Tree (50-100x faster than nested loops)
            _logger.LogInformation("Step 5: Grouping data by barangay using R-Tree spatial indexing...");
            var groupingStopwatch = System.Diagnostics.Stopwatch.StartNew();

            var cellularsByBarangay = new Dictionary<Guid, List<(Guid ServiceProviderId, string ProviderName, string Generation, string TechnologyStandard, DateTime CreatedAt, DateTime UpdatedAt)>>();
            var validationsByBarangay = new Dictionary<Guid, List<(Guid ServiceProviderId, string ProviderName, string Generation, string TechnologyStandard, DateTime CreatedAt, DateTime UpdatedAt)>>();
            var speedtestsByBarangay = new Dictionary<Guid, List<(Guid ServiceProviderId, string ProviderName, string GenerationName, decimal DownloadSpeed, decimal UploadSpeed, DateTime CreatedAt, DateTime UpdatedAt)>>();

            // Initialize dictionaries
            foreach (var barangay in barangays)
            {
                cellularsByBarangay[barangay.Id] = new List<(Guid, string, string, string, DateTime, DateTime)>();
                validationsByBarangay[barangay.Id] = new List<(Guid, string, string, string, DateTime, DateTime)>();
                speedtestsByBarangay[barangay.Id] = new List<(Guid, string, string, decimal, decimal, DateTime, DateTime)>();
            }

            // Group cellulars using R-Tree (OPTIMIZED - 50-100x faster)
            int assignedCellulars = 0;
            foreach (var cellular in allCellulars)
            {
                // Query R-Tree for candidate barangays (bounding box intersection)
                var candidateIds = barangayTree.Query(cellular.Location.EnvelopeInternal)
                    .Cast<Guid>()
                    .ToList();

                // Check actual containment for candidates only
                foreach (var barangayId in candidateIds)
                {
                    var barangay = barangayLookup[barangayId];
                    if (barangay.Boundaries != null && barangay.Boundaries.Contains(cellular.Location))
                    {
                        cellularsByBarangay[barangayId].Add((
                            cellular.ServiceProviderId,
                            cellular.ProviderName,
                            cellular.Generation,
                            cellular.TechnologyStandard,
                            cellular.CreatedAt,
                            cellular.UpdatedAt
                        ));
                        assignedCellulars++;
                        break; // Cellular assigned, move to next
                    }
                }
            }

            _logger.LogInformation("Assigned {Assigned}/{Total} cellulars to barangays using R-Tree", 
                assignedCellulars, allCellulars.Count);

            // Group wireless validations (already have BarangayId, no spatial check needed)
            foreach (var validation in allWirelessValidations)
            {
                if (validationsByBarangay.ContainsKey(validation.BarangayId))
                {
                    validationsByBarangay[validation.BarangayId].Add((
                        validation.ServiceProviderId,
                        validation.ProviderName,
                        validation.Generation,
                        validation.TechnologyStandard,
                        validation.CreatedAt,
                        validation.UpdatedAt
                    ));
                }
            }

            // Group speedtests using R-Tree (OPTIMIZED)
            int assignedSpeedtests = 0;
            foreach (var speedtest in allSpeedtests)
            {
                var candidateIds = barangayTree.Query(speedtest.Location.EnvelopeInternal)
                    .Cast<Guid>()
                    .ToList();

                foreach (var barangayId in candidateIds)
                {
                    var barangay = barangayLookup[barangayId];
                    if (barangay.Boundaries != null && barangay.Boundaries.Contains(speedtest.Location))
                    {
                        speedtestsByBarangay[barangayId].Add((
                            speedtest.ServiceProviderId,
                            speedtest.ProviderName,
                            speedtest.GenerationName,
                            speedtest.DownloadSpeed,
                            speedtest.UploadSpeed,
                            speedtest.CreatedAt,
                            speedtest.UpdatedAt
                        ));
                        assignedSpeedtests++;
                        break; // Speedtest assigned, move to next
                    }
                }
            }

            _logger.LogInformation("Assigned {Assigned}/{Total} speedtests to barangays using R-Tree", 
                assignedSpeedtests, allSpeedtests.Count);

            groupingStopwatch.Stop();
            _logger.LogInformation("Grouped data in {ElapsedMs}ms using R-Tree (vs hours with nested loops)", 
                groupingStopwatch.ElapsedMilliseconds);

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
        List<(Guid ServiceProviderId, string ProviderName, string Generation, string TechnologyStandard, DateTime CreatedAt, DateTime UpdatedAt)> cellulars,
        List<(Guid ServiceProviderId, string ProviderName, string Generation, string TechnologyStandard, DateTime CreatedAt, DateTime UpdatedAt)> validations,
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

            // CROSS-SOURCE AGGREGATION: Track unique technologies from all sources
            // Cellular found? Tech is present. Validation found? Tech is present. Speedtest found? Tech is present.
            // All three found? Tech is still just "present" (deduplicated union of all sources)
            var uniqueTechnologies = new HashSet<string>();

            // Add unique technologies from cellulars
            foreach (var c in providerCellulars)
            {
                var techKey = !string.IsNullOrEmpty(c.TechnologyStandard) ? c.TechnologyStandard : c.Generation;
                if (!string.IsNullOrEmpty(techKey))
                    uniqueTechnologies.Add(techKey);
            }

            // Add unique technologies from wireless validations
            foreach (var v in providerValidations)
            {
                var techKey = !string.IsNullOrEmpty(v.TechnologyStandard) ? v.TechnologyStandard : v.Generation;
                if (!string.IsNullOrEmpty(techKey))
                    uniqueTechnologies.Add(techKey);
            }

            // Add unique technologies from speedtests
            foreach (var s in providerSpeedtests)
            {
                if (!string.IsNullOrEmpty(s.GenerationName))
                    uniqueTechnologies.Add(s.GenerationName);
            }

            // Count unique technologies per generation (e.g., "LTE" and "FDD-LTE" both count as separate 4G techs)
            var generationCounts = new Dictionary<string, int>();
            foreach (var tech in uniqueTechnologies)
            {
                var normalizedGen = NormalizeGeneration(tech);
                if (!string.IsNullOrEmpty(normalizedGen))
                    generationCounts[normalizedGen] = generationCounts.GetValueOrDefault(normalizedGen) + 1;
            }

            var count5G = generationCounts.GetValueOrDefault("5G");
            var count4G = generationCounts.GetValueOrDefault("4G");
            var count3G = generationCounts.GetValueOrDefault("3G");
            var count2G = generationCounts.GetValueOrDefault("2G");

            // Calculate TechnologyBandBreakdownDto-compatible properties
            var totalTechnologyCount = count2G + count3G + count4G + count5G;
            var modernCoverageRatio = totalTechnologyCount > 0
                ? (double)(count4G + count5G) / totalTechnologyCount
                : 0.0;
            var hasModernCoverage = count4G > 0 || count5G > 0;
            var isLegacyDominated = (count2G + count3G) > (count4G + count5G) && hasModernCoverage;

            // Determine dominant generation
            string? dominantGeneration = null;
            if (generationCounts.Any())
            {
                var maxCount = generationCounts.Max(kvp => kvp.Value);
                dominantGeneration = generationCounts.FirstOrDefault(kvp => kvp.Value == maxCount).Key;
            }

            // Build available generations list
            var availableGenerations = new List<string>();
            if (count2G > 0) availableGenerations.Add("2G");
            if (count3G > 0) availableGenerations.Add("3G");
            if (count4G > 0) availableGenerations.Add("4G");
            if (count5G > 0) availableGenerations.Add("5G");

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
                TotalTechnologyCount = totalTechnologyCount,
                ModernCoverageRatio = modernCoverageRatio,
                HasModernCoverage = hasModernCoverage,
                IsLegacyDominated = isLegacyDominated,
                DominantGeneration = dominantGeneration,
                AvailableGenerations = availableGenerations,
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

    /// <summary>
    /// Normalize generation/technology names to standard format (2G, 3G, 4G, 5G)
    /// Matches the logic from ProviderTechnologyService for cross-checking consistency
    /// </summary>
    private static string NormalizeGeneration(string name)
    {
        if (string.IsNullOrEmpty(name)) return "";

        var normalized = name.ToUpperInvariant();

        // 5G patterns
        if (normalized.Contains("5G") || normalized.Contains("NR")) return "5G";

        // 4G/LTE patterns
        if (normalized.Contains("4G") || normalized.Contains("LTE")) return "4G";

        // 3G patterns
        if (normalized.Contains("3G") || normalized.Contains("UMTS") || 
            normalized.Contains("HSPA") || normalized.Contains("WCDMA"))
            return "3G";

        // 2G patterns
        if (normalized.Contains("2G") || normalized.Contains("GSM") || 
            normalized.Contains("EDGE") || normalized.Contains("GPRS"))
            return "2G";

        return "";
    }

    /// <summary>
    /// Convert BarangayWirelessCoverageDto to TechnologyBandBreakdownDto for scoring compatibility.
    /// This ensures WirelessScoringService can use the same logic as the old IProviderTechnologyService.
    /// Note: IsLegacyDominated is computed automatically from Count2G/3G/4G/5G in TechnologyBandBreakdownDto.
    /// </summary>
    public static TechnologyBandBreakdownDto ToTechnologyBandBreakdown(BarangayWirelessCoverageDto coverage)
    {
        return new TechnologyBandBreakdownDto
        {
            Count2G = coverage.Count2G,
            Count3G = coverage.Count3G,
            Count4G = coverage.Count4G,
            Count5G = coverage.Count5G,
            TotalTechnologyCount = coverage.TotalTechnologyCount,
            ModernCoverageRatio = coverage.ModernCoverageRatio,
            HasModernCoverage = coverage.HasModernCoverage,
            DominantGeneration = coverage.DominantGeneration,
            AvailableGenerations = coverage.AvailableGenerations
            // Note: IsLegacyDominated is a computed property in TechnologyBandBreakdownDto
            // It auto-calculates from Count2G/3G/4G/5G, so we don't need to set it
        };
    }
}
