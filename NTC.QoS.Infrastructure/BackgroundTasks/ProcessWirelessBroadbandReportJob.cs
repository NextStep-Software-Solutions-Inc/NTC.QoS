using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NTC.QoS.Application.Features.Reports.WirelessBroadband.DTOs;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Analytics;
using NTC.QoS.Domain.Models.DTOs.Report;
using NTC.QoS.Domain.Models.DTOs.Reports;
using NTC.QoS.Infrastructure.Persistence;
using NTC.QoS.Infrastructure.Services;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Diagnostics;
using System.Drawing;
using Color = System.Drawing.Color;

namespace NTC.QoS.Infrastructure.BackgroundTasks;

/// <summary>
/// Background job for generating wireless broadband scored reports asynchronously.
/// Handles long-running report generation for regions with thousands of barangays.
/// </summary>
[Queue("reports")]
[AutomaticRetry(Attempts = 0, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
[DisableConcurrentExecution(timeoutInSeconds: 3600)] // 1 hour timeout
public class ProcessWirelessBroadbandReportJob : IProcessWirelessBroadbandReportJob
{
    private readonly ApplicationDbContext _context;
    private readonly IProviderTechnologyService _providerTechService;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<ProcessWirelessBroadbandReportJob> _logger;
    private const int StartColumn = 3;
    private const double MinimumSpeedThreshold = 25.0; // Mbps for CMTS "Served" criteria

    public ProcessWirelessBroadbandReportJob(
        ApplicationDbContext context,
        IProviderTechnologyService providerTechService,
        IMemoryCache memoryCache,
        ILogger<ProcessWirelessBroadbandReportJob> logger)
    {
        _context = context;
        _providerTechService = providerTechService;
        _memoryCache = memoryCache;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 0, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
    public async Task ExecuteAsync(
        string jobId,
        string userId,
        Guid? regionId,
        Guid? provinceId,
        Guid? municipalityId,
        ReportFormatEnum format,
        ServiceStatusEnum? filterByStatus,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var cacheKey = $"wireless_report_job_{jobId}";
        
        _logger.LogInformation("Starting wireless broadband report job {JobId} for user {UserId}", jobId, userId);

        try
        {
            // Update status to Processing
            UpdateJobStatus(cacheKey, status: ReportJobStatusEnum.Processing);

            // Generate the report
            var reportGenStopwatch = Stopwatch.StartNew();
            var (fileContent, fileName, barangayCount) = await GenerateReportAsync(
                regionId, 
                provinceId, 
                municipalityId, 
                format, 
                filterByStatus, 
                cancellationToken);
            reportGenStopwatch.Stop();

            _logger.LogInformation(
                "Report generation completed in {ElapsedMs}ms for {BarangayCount} barangays. Format: {Format}",
                reportGenStopwatch.ElapsedMilliseconds, barangayCount, format);

            // Save file to disk
            var fileSaveStopwatch = Stopwatch.StartNew();
            var reportsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Reports", "WirelessBroadband");
            Directory.CreateDirectory(reportsDirectory);

            var filePath = Path.Combine(reportsDirectory, fileName);
            await File.WriteAllBytesAsync(filePath, fileContent, cancellationToken);
            fileSaveStopwatch.Stop();

            _logger.LogInformation(
                "File saved to disk in {ElapsedMs}ms. Size: {FileSize} KB. Path: {FilePath}",
                fileSaveStopwatch.ElapsedMilliseconds, fileContent.Length / 1024, filePath);

            stopwatch.Stop();
            _logger.LogInformation(
                "Wireless broadband report job {JobId} completed successfully in {TotalMs}ms (Data: {DataMs}ms, File: {FileMs}ms). Barangays: {BarangayCount}",
                jobId, stopwatch.ElapsedMilliseconds, reportGenStopwatch.ElapsedMilliseconds, 
                fileSaveStopwatch.ElapsedMilliseconds, barangayCount);

            // Update status to Completed
            UpdateJobStatus(
                cacheKey,
                status: ReportJobStatusEnum.Completed,
                filePath: filePath,
                fileName: fileName,
                completedAt: DateTime.UtcNow,
                processingTimeMs: stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            var errorMessage = $"Error in job {jobId}: {ex.Message}";
            _logger.LogError(ex, "Wireless broadband report job {JobId} failed", jobId);

            // Update status to Failed
            UpdateJobStatus(
                cacheKey,
                status: ReportJobStatusEnum.Failed,
                errorMessage: errorMessage,
                completedAt: DateTime.UtcNow,
                processingTimeMs: stopwatch.ElapsedMilliseconds);

            throw; // Re-throw for Hangfire logging
        }
    }

    private void UpdateJobStatus(
        string cacheKey,
        ReportJobStatusEnum? status = null,
        string? filePath = null,
        string? fileName = null,
        DateTime? completedAt = null,
        long? processingTimeMs = null,
        string? errorMessage = null)
    {
        if (_memoryCache.TryGetValue<WirelessBroadbandReportJobStatus>(cacheKey, out var jobStatus) && jobStatus != null)
        {
            if (status.HasValue) jobStatus.Status = status.Value;
            if (filePath != null) jobStatus.FilePath = filePath;
            if (fileName != null) jobStatus.FileName = fileName;
            if (completedAt.HasValue) jobStatus.CompletedAt = completedAt.Value;
            if (processingTimeMs.HasValue) jobStatus.ProcessingTimeMs = processingTimeMs.Value;
            if (errorMessage != null) jobStatus.ErrorMessage = errorMessage;

            _memoryCache.Set(cacheKey, jobStatus, TimeSpan.FromHours(24));
        }
    }

    private async Task<(byte[] FileContent, string FileName, int BarangayCount)> GenerateReportAsync(
        Guid? regionId,
        Guid? provinceId,
        Guid? municipalityId,
        ReportFormatEnum format,
        ServiceStatusEnum? filterByStatus,
        CancellationToken cancellationToken)
    {
        var overallStopwatch = Stopwatch.StartNew();
        
        // NOTE: This is extracted from GetWirelessBroadbandScoredReportQueryHandler
        // TODO: Consider refactoring into a shared service to avoid code duplication

        // 1. Determine Scope and Fetch Target Area
        var scopeFetchStopwatch = Stopwatch.StartNew();
        string reportTitleName = "";
        NetTopologySuite.Geometries.MultiPolygon? targetBoundaries = null;
        List<Domain.Entities.Barangay> barangays;

        if (municipalityId.HasValue)
        {
            var mun = await _context.Municipalities.AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == municipalityId, cancellationToken);
            if (mun == null) throw new InvalidOperationException("Municipality not found.");
            reportTitleName = mun.Name;
            targetBoundaries = mun.Boundaries;
            barangays = await _context.Barangays.AsNoTracking()
                .Where(b => b.MunicipalityId == municipalityId)
                .Include(b => b.Municipality)
                .OrderBy(b => b.Name)
                .ToListAsync(cancellationToken);
        }
        else if (provinceId.HasValue)
        {
            var prov = await _context.Provinces.AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == provinceId, cancellationToken);
            if (prov == null) throw new InvalidOperationException("Province not found.");
            reportTitleName = prov.Name;
            targetBoundaries = prov.Boundaries;
            barangays = await _context.Barangays.AsNoTracking()
                .Where(b => b.Municipality.ProvinceId == provinceId)
                .Include(b => b.Municipality)
                .OrderBy(b => b.Municipality.Name).ThenBy(b => b.Name)
                .ToListAsync(cancellationToken);
        }
        else if (regionId.HasValue)
        {
            var reg = await _context.Regions.AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == regionId, cancellationToken);
            if (reg == null) throw new InvalidOperationException("Region not found.");
            reportTitleName = reg.Name;
            targetBoundaries = reg.Boundaries;
            barangays = await _context.Barangays.AsNoTracking()
                .Where(b => b.Municipality.Province.RegionId == regionId)
                .Include(b => b.Municipality)
                .OrderBy(b => b.Municipality.Name).ThenBy(b => b.Name)
                .ToListAsync(cancellationToken);
        }
        else
        {
            throw new InvalidOperationException("Please provide a Municipality, Province, or Region ID.");
        }
        scopeFetchStopwatch.Stop();
        _logger.LogInformation(
            "Step 1: Fetched {BarangayCount} barangays for {Area} in {ElapsedMs}ms",
            barangays.Count, reportTitleName, scopeFetchStopwatch.ElapsedMilliseconds);

        // 2. Fetch Providers
        var providerFetchStopwatch = Stopwatch.StartNew();
        var providers = await _context.Providers
            .AsNoTracking()
            .Where(p => p.ServiceProviders.Any(sp => sp.Service.Type == ServiceTypeEnum.Wireless))
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);
        providerFetchStopwatch.Stop();
        _logger.LogInformation(
            "Step 2: Fetched {ProviderCount} wireless providers in {ElapsedMs}ms",
            providers.Count, providerFetchStopwatch.ElapsedMilliseconds);

        _logger.LogInformation(
            "Step 2: Fetched {ProviderCount} wireless providers in {ElapsedMs}ms",
            providers.Count, providerFetchStopwatch.ElapsedMilliseconds);

        // 3. Safeguard against excessive scope
        if (barangays.Count > 5000)
        {
            throw new InvalidOperationException(
                $"Report scope too large. Selected area contains {barangays.Count} barangays (maximum: 5,000). " +
                "Please select a smaller geographic area (province or municipality).");
        }

        _logger.LogInformation("Generating wireless broadband scored report for {Count} barangays in {Area}", 
            barangays.Count, reportTitleName);

        // 4. Process barangays with scoring
        var reportData = new List<BarangayWirelessScoredEntryDto>();
        var barangayIds = barangays.Select(b => b.Id).ToList();

        // Batch calculate provider technology breakdowns for all barangays (optimized - 3 queries total)
        var techBreakdownStopwatch = Stopwatch.StartNew();
        var batchTechBreakdowns = await _providerTechService.CalculateBatchProviderTechnologyBreakdownAsync(
            barangayIds,
            targetBoundaries,
            cancellationToken);
        techBreakdownStopwatch.Stop();
        _logger.LogInformation(
            "Step 3: Calculated technology breakdowns for {BarangayCount} barangays in {ElapsedMs}ms (avg {AvgMs}ms/barangay)",
            barangayIds.Count, techBreakdownStopwatch.ElapsedMilliseconds,
            barangayIds.Count > 0 ? techBreakdownStopwatch.ElapsedMilliseconds / barangayIds.Count : 0);

        // Query speedtests using spatial filter (database-side, leveraging spatial indexes)
        // Group by barangay in-memory to avoid loading full dataset multiple times
        _logger.LogInformation("Step 4: Loading speedtests for {Area}...", reportTitleName);
        
        var speedtestLoadStopwatch = Stopwatch.StartNew();
        var speedtestsByBarangay = await LoadSpeedtestsByBarangayAsync(
            barangays, 
            targetBoundaries, 
            cancellationToken);
        speedtestLoadStopwatch.Stop();
        
        var totalSpeedtests = speedtestsByBarangay.Values.Sum(list => list.Count);
        _logger.LogInformation(
            "Step 4: Loaded {SpeedtestCount} speedtests for {BarangayCount} barangays in {ElapsedMs}ms (avg {AvgMs}ms/barangay)",
            totalSpeedtests, speedtestsByBarangay.Count, speedtestLoadStopwatch.ElapsedMilliseconds,
            speedtestsByBarangay.Count > 0 ? speedtestLoadStopwatch.ElapsedMilliseconds / speedtestsByBarangay.Count : 0);

        // Process barangays in batches for progress tracking
        _logger.LogInformation("Step 5: Processing scoring calculations for {BarangayCount} barangays...", barangays.Count);
        var scoringStopwatch = Stopwatch.StartNew();
        const int progressBatchSize = 500;
        var processedCount = 0;
        foreach (var barangay in barangays)
        {
            var entry = new BarangayWirelessScoredEntryDto
            {
                MunicipalityName = barangay.Municipality.Name,
                BarangayName = barangay.Name
            };

            // Get pre-calculated provider technology breakdown for this barangay
            var providerTechBreakdown = batchTechBreakdowns.GetValueOrDefault(barangay.Id) 
                ?? new Dictionary<string, TechnologyBandBreakdownDto>();

            // Get speedtests for this barangay
            var barangaySpeedtests = speedtestsByBarangay.GetValueOrDefault(barangay.Id) 
                ?? new List<SpeedtestInfo>();

            // Map provider stats using technology breakdown and speedtests
            foreach (var provider in providers)
            {
                var providerSpeedtests = barangaySpeedtests
                    .Where(s => s.ProviderName.Contains(provider.Name, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                var techBreakdown = providerTechBreakdown.GetValueOrDefault(provider.Name);
                entry.ProviderStats[provider.Name] = MapProviderStats(techBreakdown, providerSpeedtests);
            }

            // Calculate overall barangay connectivity score
            var overallTechBreakdown = AggregateProviderTechnologyBreakdowns(providerTechBreakdown.Values);
            
            // Build overall speed stats for the barangay
            var overallSpeedStats = barangaySpeedtests.Any() 
                ? BuildSpeedStats(barangaySpeedtests) 
                : null;

            entry.ConnectivityScore = CalculateConnectivityScore(overallTechBreakdown, overallSpeedStats);
            entry.ServiceStatus = DetermineServiceStatus(entry.ConnectivityScore.TotalScore);
            entry.Remarks = GenerateRemarks(entry.ConnectivityScore);

            reportData.Add(entry);
            processedCount++;

            // Log progress for large reports
            if (processedCount % progressBatchSize == 0)
            {
                _logger.LogInformation("Processed {Processed}/{Total} barangays ({Percentage:F1}%)", 
                    processedCount, barangays.Count, (processedCount * 100.0 / barangays.Count));
            }
        }
        
        scoringStopwatch.Stop();
        _logger.LogInformation(
            "Step 5: Completed scoring for {ProcessedCount} barangays in {ElapsedMs}ms (avg {AvgMs}ms/barangay)",
            processedCount, scoringStopwatch.ElapsedMilliseconds,
            processedCount > 0 ? scoringStopwatch.ElapsedMilliseconds / processedCount : 0);

        // Apply filtering
        if (filterByStatus.HasValue)
        {
            var beforeFilter = reportData.Count;
            reportData = reportData
                .Where(r => r.ServiceStatus == filterByStatus.Value)
                .ToList();
            _logger.LogInformation(
                "Filtered report data by status {Status}: {Before} -> {After} entries",
                filterByStatus.Value, beforeFilter, reportData.Count);
        }

        // 5. Generate Report
        _logger.LogInformation("Step 6: Generating {Format} report file...", format);
        var reportGenStopwatch = Stopwatch.StartNew();
        byte[] fileContent;
        string fileName;

        if (format == ReportFormatEnum.Excel)
        {
            fileContent = GenerateExcel(reportTitleName, providers, reportData);
            fileName = $"WirelessBroadband_{reportTitleName}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx";
        }
        else
        {
            fileContent = GeneratePdf(reportTitleName, providers, reportData);
            fileName = $"WirelessBroadband_{reportTitleName}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pdf";
        }
        reportGenStopwatch.Stop();
        
        overallStopwatch.Stop();
        _logger.LogInformation(
            "Step 6: Generated {Format} file ({FileSize} KB) in {ElapsedMs}ms",
            format, fileContent.Length / 1024, reportGenStopwatch.ElapsedMilliseconds);
        _logger.LogInformation(
            "Report generation pipeline completed in {TotalMs}ms - Breakdown: Scope:{Scope}ms + Providers:{Prov}ms + TechBreakdown:{Tech}ms + Speedtests:{Speed}ms + Scoring:{Score}ms + FileGen:{File}ms",
            overallStopwatch.ElapsedMilliseconds, 
            scopeFetchStopwatch.ElapsedMilliseconds, 
            providerFetchStopwatch.ElapsedMilliseconds,
            techBreakdownStopwatch.ElapsedMilliseconds,
            speedtestLoadStopwatch.ElapsedMilliseconds,
            scoringStopwatch.ElapsedMilliseconds,
            reportGenStopwatch.ElapsedMilliseconds);

        return (fileContent, fileName, barangays.Count);
    }

    // ===== Helper methods from GetWirelessBroadbandScoredReportQueryHandler =====
    // NOTE: This is code duplication - consider refactoring into a shared service

    private async Task<Dictionary<Guid, List<SpeedtestInfo>>> LoadSpeedtestsByBarangayAsync(
        List<Domain.Entities.Barangay> barangays,
        NetTopologySuite.Geometries.MultiPolygon? overallBoundary,
        CancellationToken cancellationToken)
    {
        if (overallBoundary == null || !barangays.Any())
            return new Dictionary<Guid, List<SpeedtestInfo>>();

        var totalStopwatch = Stopwatch.StartNew();
        
        // OPTIMIZATION: Load ALL speedtests in the region with a single bulk spatial query
        // PostgreSQL's GIST spatial index makes this very efficient
        _logger.LogInformation(
            "Loading speedtests for {BarangayCount} barangays using bulk spatial query...", 
            barangays.Count);
        
        var bulkLoadStopwatch = Stopwatch.StartNew();
        var allSpeedtests = await _context.Speedtests
            .AsNoTracking()
            .Where(s => s.Location != null 
                     && s.ServiceProvider.Service.Type == ServiceTypeEnum.Wireless
                     && overallBoundary.Contains(s.Location))  // Single spatial filter on region/province/municipality
            .Select(s => new 
            { 
                s.Id,
                s.Location,
                Info = new SpeedtestInfo
                {
                    ProviderName = s.ServiceProvider.Provider.Name,
                    UploadSpeed = s.UploadSpeed,
                    DownloadSpeed = s.DownloadSpeed,
                    Location = s.Location!,
                    GenerationName = s.Generation != null ? s.Generation.Name : ""
                }
            })
            .ToListAsync(cancellationToken);
        bulkLoadStopwatch.Stop();
        
        _logger.LogInformation(
            "Bulk load: Retrieved {SpeedtestCount} speedtests from database in {ElapsedMs}ms",
            allSpeedtests.Count, bulkLoadStopwatch.ElapsedMilliseconds);
        
        // Group speedtests by barangay using in-memory spatial operations
        // NetTopologySuite geometry operations are highly optimized
        var groupingStopwatch = Stopwatch.StartNew();
        var result = new Dictionary<Guid, List<SpeedtestInfo>>();
        
        // Initialize all barangays with empty lists
        foreach (var barangay in barangays)
        {
            result[barangay.Id] = new List<SpeedtestInfo>();
        }
        
        // Assign each speedtest to its containing barangay
        // This is much faster than N database queries because:
        // 1. No network roundtrips
        // 2. No query parsing/planning overhead
        // 3. NetTopologySuite's in-memory Contains() is highly optimized
        var assignedCount = 0;
        foreach (var speedtest in allSpeedtests)
        {
            // Find which barangay contains this speedtest
            // Most speedtests will match the first or second barangay checked
            // due to spatial locality (speedtests cluster in populated areas)
            foreach (var barangay in barangays)
            {
                if (barangay.Boundaries != null && 
                    barangay.Boundaries.Contains(speedtest.Location))
                {
                    result[barangay.Id].Add(speedtest.Info);
                    assignedCount++;
                    break; // Speedtest can only belong to one barangay
                }
            }
        }
        groupingStopwatch.Stop();
        totalStopwatch.Stop();
        
        var barangaysWithSpeedtests = result.Count(kvp => kvp.Value.Any());
        _logger.LogInformation(
            "Grouping: Assigned {AssignedCount}/{TotalCount} speedtests to {BarangayWithData}/{TotalBarangays} barangays in {GroupMs}ms",
            assignedCount, allSpeedtests.Count, barangaysWithSpeedtests, barangays.Count, 
            groupingStopwatch.ElapsedMilliseconds);
        
        _logger.LogInformation(
            "Total speedtest loading: {AssignedCount} speedtests for {TotalBarangays} barangays in {TotalMs}ms " +
            "(Bulk load: {BulkMs}ms, Grouping: {GroupMs}ms) - " +
            "Average: {AvgMs}ms/barangay",
            assignedCount, barangays.Count, totalStopwatch.ElapsedMilliseconds,
            bulkLoadStopwatch.ElapsedMilliseconds, groupingStopwatch.ElapsedMilliseconds,
            barangays.Count > 0 ? totalStopwatch.ElapsedMilliseconds / barangays.Count : 0);

        return result;
    }

    private static ProviderWirelessStatsDto MapProviderStats(
        TechnologyBandBreakdownDto? techBreakdown,
        List<SpeedtestInfo> speedtests)
    {
        var stats = new ProviderWirelessStatsDto();

        // Map generation availability from technology breakdown
        if (techBreakdown != null)
        {
            stats.Has2G = techBreakdown.Has2G;
            stats.Has3G = techBreakdown.Has3G;
            stats.Has4G = techBreakdown.Has4G;
            stats.Has5G = techBreakdown.Has5G;
        }

        // Populate TechUsed from the median speedtest (the one representing typical experience)
        if (speedtests.Any())
        {
            // Sort by download speed to find the median speedtest
            var sortedSpeedtests = speedtests.OrderBy(s => s.DownloadSpeed).ToList();
            int medianIndex = sortedSpeedtests.Count / 2;
            var medianSpeedtest = sortedSpeedtests.Count % 2 == 0
                ? sortedSpeedtests[medianIndex - 1]  // For even count, use lower middle
                : sortedSpeedtests[medianIndex];

            stats.TechUsed = medianSpeedtest.GenerationName ?? "";
            stats.UploadSpeedMbps = (double)speedtests.Average(s => s.UploadSpeed);
            stats.DownloadSpeedMbps = (double)speedtests.Average(s => s.DownloadSpeed);
        }

        return stats;
    }

    private static TechnologyBandBreakdownDto AggregateProviderTechnologyBreakdowns(
        IEnumerable<TechnologyBandBreakdownDto> breakdowns)
    {
        var aggregated = new TechnologyBandBreakdownDto();
        var allTechnologies = new Dictionary<string, HashSet<string>>();

        foreach (var breakdown in breakdowns)
        {
            foreach (var (tech, bands) in breakdown)
            {
                if (!allTechnologies.ContainsKey(tech))
                {
                    allTechnologies[tech] = new HashSet<string>();
                }
                foreach (var band in bands)
                {
                    allTechnologies[tech].Add(band);
                }
            }

            aggregated.Count2G += breakdown.Count2G;
            aggregated.Count3G += breakdown.Count3G;
            aggregated.Count4G += breakdown.Count4G;
            aggregated.Count5G += breakdown.Count5G;
        }

        foreach (var (tech, bands) in allTechnologies)
        {
            aggregated[tech] = bands.OrderBy(b => b).ToList();
        }

        aggregated.TotalTechnologyCount = aggregated.Count2G + aggregated.Count3G + 
                                         aggregated.Count4G + aggregated.Count5G;
        aggregated.AvailableGenerations = new List<string>();
        if (aggregated.Count2G > 0) aggregated.AvailableGenerations.Add("2G");
        if (aggregated.Count3G > 0) aggregated.AvailableGenerations.Add("3G");
        if (aggregated.Count4G > 0) aggregated.AvailableGenerations.Add("4G");
        if (aggregated.Count5G > 0) aggregated.AvailableGenerations.Add("5G");

        if (aggregated.TotalTechnologyCount > 0)
        {
            aggregated.ModernCoverageRatio = (double)(aggregated.Count4G + aggregated.Count5G) / aggregated.TotalTechnologyCount;
            aggregated.HasModernCoverage = aggregated.ModernCoverageRatio > 0;

            var genCounts = new Dictionary<string, int>
            {
                ["2G"] = aggregated.Count2G,
                ["3G"] = aggregated.Count3G,
                ["4G"] = aggregated.Count4G,
                ["5G"] = aggregated.Count5G
            };
            var maxCount = genCounts.Max(kvp => kvp.Value);
            aggregated.DominantGeneration = genCounts.FirstOrDefault(kvp => kvp.Value == maxCount).Key;
        }

        return aggregated;
    }

    private WirelessConnectivityScoreDto CalculateConnectivityScore(
        TechnologyBandBreakdownDto techBreakdown,
        SpeedStatsDto? speedStats)
    {
        int internetScore = CalculateInternetAccessScore(speedStats);
        int technologyScore = CalculateTechnologyTypeScore(techBreakdown);
        int totalScore = internetScore + technologyScore;

        return new WirelessConnectivityScoreDto
        {
            InternetAccessScore = internetScore,
            TechnologyTypeScore = technologyScore,
            TotalScore = totalScore,
            HighestTechnology = GetHighestTechnology(techBreakdown)
        };
    }

    private int CalculateInternetAccessScore(SpeedStatsDto? speedStats)
    {
        if (speedStats == null || speedStats.SpeedTestCount == 0)
            return 1;

        if (speedStats.SpeedTestCount < 3)
            return 1;

        var avgDownloadSpeed = (double)(speedStats.AverageDownloadSpeedMbps ?? 0);
        var medianDownloadSpeed = (double)(speedStats.MedianDownloadSpeedMbps ?? 0);

        var speedVariability = avgDownloadSpeed > 0
            ? Math.Abs((avgDownloadSpeed - medianDownloadSpeed) / avgDownloadSpeed)
            : 0;
        var isUnreliable = speedVariability > 0.3;

        if (avgDownloadSpeed >= MinimumSpeedThreshold && !isUnreliable)
            return 3;

        if (avgDownloadSpeed >= MinimumSpeedThreshold && isUnreliable)
            return 2;

        if (avgDownloadSpeed >= 10 && avgDownloadSpeed < MinimumSpeedThreshold)
            return 2;

        return 1;
    }

    private static int CalculateTechnologyTypeScore(TechnologyBandBreakdownDto? techBreakdown)
    {
        if (techBreakdown == null || techBreakdown.TotalTechnologyCount == 0)
            return 1;

        if (techBreakdown.Count5G > 0)
            return 3;

        if (techBreakdown.Count4G > 0 && !techBreakdown.IsLegacyDominated)
            return 3;

        if (techBreakdown.HasModernTechnology && techBreakdown.IsLegacyDominated)
            return 2;

        if (techBreakdown.Count3G > 0 || techBreakdown.Count2G > 0)
            return 2;

        return 1;
    }

    private static string GetHighestTechnology(TechnologyBandBreakdownDto? techBreakdown)
    {
        if (techBreakdown == null || techBreakdown.TotalTechnologyCount == 0)
            return "None";

        if (techBreakdown.Count5G > 0) return "5G";
        if (techBreakdown.Count4G > 0) return "4G/LTE";
        if (techBreakdown.Count3G > 0) return "3G";
        if (techBreakdown.Count2G > 0) return "2G";

        return "None";
    }

    private static ServiceStatusEnum DetermineServiceStatus(int totalScore)
    {
        return totalScore switch
        {
            >= 5 => ServiceStatusEnum.Served,
            >= 3 => ServiceStatusEnum.Underserved,
            _ => ServiceStatusEnum.Unserved
        };
    }

    private static string GenerateRemarks(WirelessConnectivityScoreDto score)
    {
        var remarks = new List<string>();

        if (score.InternetAccessScore == 0)
        {
            remarks.Add("No speedtest data available");
        }
        else if (score.InternetAccessScore == 1)
        {
            remarks.Add("Below minimum speed requirement (25 Mbps)");
        }

        if (score.TechnologyTypeScore == 0)
        {
            remarks.Add("No wireless infrastructure detected");
        }
        else if (score.TechnologyTypeScore <= 2)
        {
            remarks.Add("Limited modern coverage (4G/5G)");
        }

        return remarks.Any() ? string.Join("; ", remarks) : "Adequate connectivity";
    }

    private static SpeedStatsDto BuildSpeedStats(List<SpeedtestInfo> speedtests)
    {
        if (!speedtests.Any()) return new SpeedStatsDto();

        var downloads = speedtests.Select(s => s.DownloadSpeed).OrderBy(x => x).ToList();
        var uploads = speedtests.Select(s => s.UploadSpeed).OrderBy(x => x).ToList();

        return new SpeedStatsDto
        {
            SpeedTestCount = speedtests.Count,
            AverageDownloadSpeedMbps = speedtests.Average(s => s.DownloadSpeed),
            AverageUploadSpeedMbps = speedtests.Average(s => s.UploadSpeed),
            MedianDownloadSpeedMbps = CalculateMedian(downloads),
            MedianUploadSpeedMbps = CalculateMedian(uploads),
            MeetsMinimumSpeedRequirement = speedtests.Average(s => s.DownloadSpeed) >= 25m
        };
    }

    private static decimal CalculateMedian(List<decimal> sortedValues)
    {
        if (sortedValues.Count == 0) return 0;
        
        int mid = sortedValues.Count / 2;
        return sortedValues.Count % 2 == 0
            ? (sortedValues[mid - 1] + sortedValues[mid]) / 2
            : sortedValues[mid];
    }

    // ===== Report Generation Methods =====
    // NOTE: Excel and PDF generation methods would be copy-pasted from the handler
    // For brevity, I'll include just the signatures here - you'll need to copy the full implementations

    private byte[] GenerateExcel(
        string reportTitle,
        List<Domain.Entities.Provider> providers,
        List<BarangayWirelessScoredEntryDto> reportData)
    {
        ExcelPackage.License.SetNonCommercialPersonal("NTC QoS");
        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("Wireless Broadband Scored");

        int col = StartColumn;

        // Collect all present generations for dynamic headers
        var providerGenerationsMap = new Dictionary<string, List<string>>();

        foreach (var entry in reportData)
        {
            foreach (var (providerName, stats) in entry.ProviderStats)
            {
                if (!providerGenerationsMap.ContainsKey(providerName))
                {
                    providerGenerationsMap[providerName] = new List<string>();
                }

                var gens = providerGenerationsMap[providerName];
                if (stats.Has2G && !gens.Contains("2G")) gens.Add("2G");
                if (stats.Has3G && !gens.Contains("3G")) gens.Add("3G");
                if (stats.Has4G && !gens.Contains("4G")) gens.Add("4G");
                if (stats.Has5G && !gens.Contains("5G")) gens.Add("5G");
            }
        }

        // --- HEADERS ---

        // Helper to draw provider section with additional score columns
        void DrawProviderSection(string providerName, string displayName, List<string> generations, Color color)
        {
            int startCol = col;
            int width = generations.Count + 1 + 2; // Gens + Tech + Speedtest(2)
            int endCol = startCol + width - 1;

            // Row 2: Provider Name
            worksheet.Cells[2, startCol, 2, endCol].Merge = true;
            worksheet.Cells[2, startCol].Value = displayName;
            worksheet.Cells[2, startCol].Style.Fill.PatternType = ExcelFillStyle.Solid;
            worksheet.Cells[2, startCol].Style.Fill.BackgroundColor.SetColor(color);

            // Row 3: Sub-headers
            foreach (var gen in generations)
            {
                worksheet.Cells[3, col].Value = gen;
                worksheet.Cells[3, col, 4, col].Merge = true;
                col++;
            }

            worksheet.Cells[3, col].Value = "Tech used";
            worksheet.Cells[3, col, 4, col].Merge = true;
            col++;

            worksheet.Cells[3, col].Value = "Speedtest (Mbps)";
            worksheet.Cells[3, col, 3, col + 1].Merge = true;
            worksheet.Cells[4, col].Value = "Upload";
            worksheet.Cells[4, col + 1].Value = "Download";
            col += 2;
        }

        // Draw all provider sections
        foreach (var provider in providers)
        {
            if (!providerGenerationsMap.TryGetValue(provider.Name, out var generations)) continue;

            Color color;
            try
            {
                color = !string.IsNullOrWhiteSpace(provider.Color)
                    ? ColorTranslator.FromHtml(provider.Color)
                    : Color.LightGray;
            }
            catch
            {
                color = Color.LightGray;
            }

            DrawProviderSection(
                provider.Name,
                provider.Name.ToUpper(),
                generations,
                color);
        }

        // Add Score Columns after all providers
        int scoreStartCol = col;

        // Connectivity Score section
        worksheet.Cells[2, scoreStartCol, 2, scoreStartCol + 3].Merge = true;
        worksheet.Cells[2, scoreStartCol].Value = "CONNECTIVITY SCORE";
        worksheet.Cells[2, scoreStartCol].Style.Fill.PatternType = ExcelFillStyle.Solid;
        worksheet.Cells[2, scoreStartCol].Style.Fill.BackgroundColor.SetColor(Color.LightBlue);

        worksheet.Cells[3, scoreStartCol].Value = "Technology";
        worksheet.Cells[3, scoreStartCol, 4, scoreStartCol].Merge = true;

        worksheet.Cells[3, scoreStartCol + 1].Value = "Internet";
        worksheet.Cells[3, scoreStartCol + 1, 4, scoreStartCol + 1].Merge = true;

        worksheet.Cells[3, scoreStartCol + 2].Value = "Total";
        worksheet.Cells[3, scoreStartCol + 2, 4, scoreStartCol + 2].Merge = true;

        worksheet.Cells[3, scoreStartCol + 3].Value = "Status";
        worksheet.Cells[3, scoreStartCol + 3, 4, scoreStartCol + 3].Merge = true;

        int totalCols = scoreStartCol + 3;

        // Row 1: Main Title
        worksheet.Cells[1, 1, 1, totalCols].Merge = true;
        worksheet.Cells["A1"].Value = "WIRELESS BROADBAND WITH CONNECTIVITY SCORES";
        worksheet.Cells["A1"].Style.Font.Bold = true;
        worksheet.Cells["A1"].Style.Font.Size = 14;
        worksheet.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        worksheet.Cells["A1"].Style.Fill.PatternType = ExcelFillStyle.Solid;
        worksheet.Cells["A1"].Style.Fill.BackgroundColor.SetColor(Color.Cyan);

        // Municipality and Barangay headers
        worksheet.Cells["A2:A4"].Merge = true;
        worksheet.Cells["A2"].Value = "Municipality";
        worksheet.Cells["B2:B4"].Merge = true;
        worksheet.Cells["B2"].Value = "Barangay";

        // Style headers
        worksheet.Cells[2, 1, 4, totalCols].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        worksheet.Cells[2, 1, 4, totalCols].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
        worksheet.Cells[2, 1, 4, totalCols].Style.Font.Bold = true;
        worksheet.Cells[2, 1, 4, totalCols].Style.Border.Top.Style = ExcelBorderStyle.Thin;
        worksheet.Cells[2, 1, 4, totalCols].Style.Border.Left.Style = ExcelBorderStyle.Thin;
        worksheet.Cells[2, 1, 4, totalCols].Style.Border.Right.Style = ExcelBorderStyle.Thin;
        worksheet.Cells[2, 1, 4, totalCols].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

        // --- DATA ROWS ---
        int row = 5;
        var groupedData = reportData.GroupBy(x => x.MunicipalityName);

        foreach (var group in groupedData)
        {
            int startRow = row;

            foreach (var item in group)
            {
                worksheet.Cells[row, 2].Value = item.BarangayName;
                int currentCol = StartColumn;

                // Provider data
                foreach (var provider in providers)
                {
                    if (!providerGenerationsMap.TryGetValue(provider.Name, out var generations)) continue;

                    if (!item.ProviderStats.TryGetValue(provider.Name, out var stats))
                        stats = new ProviderWirelessStatsDto();

                    foreach (var gen in generations)
                    {
                        if (HasGeneration(stats, gen))
                            worksheet.Cells[row, currentCol].Value = "✓";
                        currentCol++;
                    }

                    worksheet.Cells[row, currentCol].Value = stats.TechUsed;
                    currentCol++;

                    if (stats.UploadSpeedMbps.HasValue)
                        worksheet.Cells[row, currentCol].Value = Math.Round(stats.UploadSpeedMbps.Value, 2);

                    if (stats.DownloadSpeedMbps.HasValue)
                        worksheet.Cells[row, currentCol + 1].Value = Math.Round(stats.DownloadSpeedMbps.Value, 2);

                    currentCol += 2;
                }

                // Score columns
                worksheet.Cells[row, scoreStartCol].Value = $"{item.ConnectivityScore.TechnologyTypeScore}/3";
                worksheet.Cells[row, scoreStartCol + 1].Value = $"{item.ConnectivityScore.InternetAccessScore}/3";
                worksheet.Cells[row, scoreStartCol + 2].Value = $"{item.ConnectivityScore.TotalScore}/6";
                worksheet.Cells[row, scoreStartCol + 3].Value = item.ServiceStatus.ToString().ToUpper();

                // Color code status
                var statusCell = worksheet.Cells[row, scoreStartCol + 3];
                statusCell.Style.Font.Bold = true;
                switch (item.ServiceStatus)
                {
                    case ServiceStatusEnum.Served:
                        statusCell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        statusCell.Style.Fill.BackgroundColor.SetColor(Color.LightGreen);
                        break;
                    case ServiceStatusEnum.Underserved:
                        statusCell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        statusCell.Style.Fill.BackgroundColor.SetColor(Color.Yellow);
                        break;
                    case ServiceStatusEnum.Unserved:
                        statusCell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        statusCell.Style.Fill.BackgroundColor.SetColor(Color.LightCoral);
                        break;
                }

                row++;
            }

            // Merge municipality column
            if (row > startRow)
            {
                worksheet.Cells[startRow, 1, row - 1, 1].Merge = true;
                worksheet.Cells[startRow, 1].Value = group.Key;
                worksheet.Cells[startRow, 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                worksheet.Cells[startRow, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheet.Cells[startRow, 1].Style.TextRotation = 90;
                worksheet.Cells[startRow, 1].Style.Font.Bold = true;
                worksheet.Cells[startRow, 1].Style.Font.Size = 14;
            }
        }

        // Borders for data (only if there's data)
        if (row > 5)
        {
            var dataRange = worksheet.Cells[5, 1, row - 1, totalCols];
            dataRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            dataRange.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            dataRange.Style.Border.Right.Style = ExcelBorderStyle.Thin;
            dataRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            dataRange.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            // Left-align barangay names
            worksheet.Cells[5, 2, row - 1, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
        }

        worksheet.Cells.AutoFitColumns();
        worksheet.View.FreezePanes(5, 3);

        return package.GetAsByteArray();
    }

    private byte[] GeneratePdf(
        string reportTitle,
        List<Domain.Entities.Provider> providers,
        List<BarangayWirelessScoredEntryDto> reportData)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(1, QuestPDF.Infrastructure.Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(8));

                page.Header().Column(column =>
                {
                    column.Item().AlignCenter().Text("NATIONAL TELECOMMUNICATIONS COMMISSION")
                        .FontSize(15).SemiBold();

                    column.Item().AlignCenter().Text("Quality of Service Management System")
                        .FontSize(10).FontColor(Colors.Grey.Darken1);

                    column.Item().PaddingVertical(5).LineHorizontal(1.5f).LineColor(Colors.Black);

                    column.Item().AlignCenter().Text($"Wireless Broadband Report with Scores - {reportTitle}")
                        .FontSize(12).Bold();

                    column.Item().AlignCenter().Text($"Generated: {DateTime.Now:MMMM dd, yyyy 'at' h:mm tt}")
                        .FontSize(8).FontColor(Colors.Grey.Darken1);
                });

                page.Content()
                    .PaddingVertical(1, QuestPDF.Infrastructure.Unit.Centimetre)
                    .Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(80); // Municipality
                            columns.ConstantColumn(100); // Barangay

                            // Provider columns (simplified for PDF)
                            foreach (var _ in providers)
                            {
                                columns.RelativeColumn(); // Has service indicator
                            }

                            // Score columns
                            columns.RelativeColumn(); // Tech Score
                            columns.RelativeColumn(); // Internet Score
                            columns.RelativeColumn(); // Total Score
                            columns.ConstantColumn(80); // Status
                        });

                        table.Header(header =>
                        {
                            header.Cell().RowSpan(2).Element(CellStyle).Text("Municipality");
                            header.Cell().RowSpan(2).Element(CellStyle).Text("Barangay");

                            foreach (var provider in providers)
                            {
                                header.Cell().RowSpan(2).Element(CellStyle).Text(provider.Name);
                            }

                            header.Cell().ColumnSpan(3).Element(CellStyle)
                                .Background(Colors.LightBlue.Lighten1).Text("Connectivity Score");
                            header.Cell().RowSpan(2).Element(CellStyle).Text("Status");

                            // Score sub-headers
                            header.Cell().Element(CellStyle).Text("Tech (1-3)");
                            header.Cell().Element(CellStyle).Text("Internet (1-3)");
                            header.Cell().Element(CellStyle).Text("Total (1-6)");
                        });

                        foreach (var item in reportData)
                        {
                            table.Cell().Element(CellStyle).Text(item.MunicipalityName);
                            table.Cell().Element(CellStyle).Text(item.BarangayName);

                            foreach (var provider in providers)
                            {
                                var hasService = item.ProviderStats.TryGetValue(provider.Name, out var stats)
                                    && (stats.Has4G || stats.Has5G || stats.Has3G);

                                table.Cell().Element(CellStyle).Text(hasService ? "✓" : "-");
                            }

                            table.Cell().Element(CellStyle).Text(item.ConnectivityScore.TechnologyTypeScore.ToString());
                            table.Cell().Element(CellStyle).Text(item.ConnectivityScore.InternetAccessScore.ToString());
                            table.Cell().Element(CellStyle).Text(item.ConnectivityScore.TotalScore.ToString());

                            var statusColor = item.ServiceStatus switch
                            {
                                ServiceStatusEnum.Served => Colors.Green.Lighten3,
                                ServiceStatusEnum.Underserved => Colors.Yellow.Lighten3,
                                _ => Colors.Red.Lighten3
                            };

                            table.Cell().Element(CellStyle).Background(statusColor)
                                .Text(item.ServiceStatus.ToString().ToUpper());
                        }
                    });

                page.Footer()
                    .AlignCenter()
                    .Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                    });
            });
        })
        .GeneratePdf();

        static IContainer CellStyle(IContainer container)
        {
            return container.Border(1).BorderColor(Colors.Grey.Lighten2)
                .Padding(2).AlignMiddle().AlignCenter();
        }
    }

    /// <summary>
    /// Helper to check if provider stats has a specific generation
    /// </summary>
    private static bool HasGeneration(ProviderWirelessStatsDto stats, string generation)
    {
        return generation switch
        {
            "2G" => stats.Has2G,
            "3G" => stats.Has3G,
            "4G" => stats.Has4G,
            "5G" => stats.Has5G,
            _ => false
        };
    }

    // ===== Private DTOs =====

    private class SpeedtestInfo
    {
        public string ProviderName { get; set; } = string.Empty;
        public decimal UploadSpeed { get; set; }
        public decimal DownloadSpeed { get; set; }
        public NetTopologySuite.Geometries.Point Location { get; set; } = null!;
        public string GenerationName { get; set; } = string.Empty;
    }
}
