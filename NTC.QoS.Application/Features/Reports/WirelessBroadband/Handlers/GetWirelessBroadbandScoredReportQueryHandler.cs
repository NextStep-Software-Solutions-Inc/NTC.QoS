using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NTC.QoS.Application.Features.Reports.WirelessBroadband.DTOs;
using NTC.QoS.Application.Features.Reports.WirelessBroadband.Requests;
using NTC.QoS.Application.Services;
using NTC.QoS.Domain.Enums;
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
using System.Drawing;
using Color = System.Drawing.Color;

namespace NTC.QoS.Application.Features.Reports.WirelessBroadband.Handlers;

/// <summary>
/// Handler for generating wireless broadband reports with connectivity scores.
/// Reuses assessment scoring logic for consistency across the system.
/// Scores are based on 2 CMTS criteria: Internet Access (speedtest) and Technology Type (wireless infrastructure).
/// </summary>
public class GetWirelessBroadbandScoredReportHandler : IRequestHandler<GetWirelessBroadbandScoredReportQuery, ApiResponse<WirelessBroadbandReportResult>>
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<GetWirelessBroadbandScoredReportHandler> _logger;
    private readonly IWirelessScoringService _scoringService;
    private readonly IProviderTechnologyService _providerTechService;
    private const int StartColumn = 3;

    public GetWirelessBroadbandScoredReportHandler(
        ApplicationDbContext context,
        ILogger<GetWirelessBroadbandScoredReportHandler> logger,
        IWirelessScoringService scoringService,
        IProviderTechnologyService providerTechService)
    {
        _context = context;
        _logger = logger;
        _scoringService = scoringService;
        _providerTechService = providerTechService;
    }

    public async Task<ApiResponse<WirelessBroadbandReportResult>> Handle(
        GetWirelessBroadbandScoredReportQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            // 1. Determine Scope and Fetch Target Area
            string reportTitleName = "";
            NetTopologySuite.Geometries.MultiPolygon? targetBoundaries = null;
            List<Domain.Entities.Barangay> barangays;

            if (request.MunicipalityId.HasValue)
            {
                var mun = await _context.Municipalities.AsNoTracking()
                    .FirstOrDefaultAsync(m => m.Id == request.MunicipalityId, cancellationToken);
                if (mun == null) return new ApiResponse<WirelessBroadbandReportResult>("Municipality not found.");
                reportTitleName = mun.Name;
                targetBoundaries = mun.Boundaries;
                barangays = await _context.Barangays.AsNoTracking()
                    .Where(b => b.MunicipalityId == request.MunicipalityId)
                    .Include(b => b.Municipality)
                    .OrderBy(b => b.Name)
                    .ToListAsync(cancellationToken);
            }
            else if (request.ProvinceId.HasValue)
            {
                var prov = await _context.Provinces.AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == request.ProvinceId, cancellationToken);
                if (prov == null) return new ApiResponse<WirelessBroadbandReportResult>("Province not found.");
                reportTitleName = prov.Name;
                targetBoundaries = prov.Boundaries;
                barangays = await _context.Barangays.AsNoTracking()
                    .Where(b => b.Municipality.ProvinceId == request.ProvinceId)
                    .Include(b => b.Municipality)
                    .OrderBy(b => b.Municipality.Name).ThenBy(b => b.Name)
                    .ToListAsync(cancellationToken);
            }
            else if (request.RegionId.HasValue)
            {
                var reg = await _context.Regions.AsNoTracking()
                    .FirstOrDefaultAsync(r => r.Id == request.RegionId, cancellationToken);
                if (reg == null) return new ApiResponse<WirelessBroadbandReportResult>("Region not found.");
                reportTitleName = reg.Name;
                targetBoundaries = reg.Boundaries;
                barangays = await _context.Barangays.AsNoTracking()
                    .Where(b => b.Municipality.Province.RegionId == request.RegionId)
                    .Include(b => b.Municipality)
                    .OrderBy(b => b.Municipality.Name).ThenBy(b => b.Name)
                    .ToListAsync(cancellationToken);
            }
            else
            {
                return new ApiResponse<WirelessBroadbandReportResult>(
                    "Please provide a Municipality, Province, or Region ID.");
            }

            // 2. Fetch Providers
            var providers = await _context.Providers
                .AsNoTracking()
                .Where(p => p.ServiceProviders.Any(sp => sp.Service.Type == ServiceTypeEnum.Wireless))
                .OrderBy(p => p.Name)
                .ToListAsync(cancellationToken);

            // 3. Safeguard against excessive scope
            if (barangays.Count > 5000)
            {
                return new ApiResponse<WirelessBroadbandReportResult>(
                    $"Report scope too large. Selected area contains {barangays.Count} barangays (maximum: 5,000). " +
                    "Please select a smaller geographic area (province or municipality).");
            }

            _logger.LogInformation("Generating wireless broadband scored report for {Count} barangays in {Area}", 
                barangays.Count, reportTitleName);

            // 4. Process barangays with scoring
            var reportData = new List<BarangayWirelessScoredEntryDto>();
            var barangayIds = barangays.Select(b => b.Id).ToList();

            // Batch calculate provider technology breakdowns for all barangays (optimized - 3 queries total)
            var batchTechBreakdowns = await _providerTechService.CalculateBatchProviderTechnologyBreakdownAsync(
                barangayIds,
                targetBoundaries,
                cancellationToken);

            // Query speedtests using spatial filter (database-side, leveraging spatial indexes)
            // Group by barangay in-memory to avoid loading full dataset multiple times
            _logger.LogInformation("Loading speedtests for {Area}...", reportTitleName);
            
            var speedtestsByBarangay = await LoadSpeedtestsByBarangayAsync(
                barangays, 
                targetBoundaries, 
                cancellationToken);
            
            _logger.LogInformation("Loaded speedtests for {Count} barangays", speedtestsByBarangay.Count);

            // Process barangays in batches for progress tracking
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

            // Apply filtering
            if (request.FilterByStatus.HasValue)
            {
                reportData = reportData
                    .Where(r => r.ServiceStatus == request.FilterByStatus.Value)
                    .ToList();
            }

            // 5. Generate Report
            byte[] fileContent;
            string extension;

            if (request.Format == ReportFormat.Excel)
            {
                fileContent = GenerateExcel(reportTitleName, providers, reportData);
                extension = "xlsx";
            }
            else
            {
                fileContent = GeneratePdf(reportTitleName, providers, reportData);
                extension = "pdf";
            }

            var result = new WirelessBroadbandReportResult
            {
                FileContent = fileContent,
                FileName = $"{reportTitleName}_WirelessBroadbandScored_{DateTime.UtcNow:yyyyMMdd}.{extension}"
            };

            return new ApiResponse<WirelessBroadbandReportResult>(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating wireless broadband scored report.");
            return new ApiResponse<WirelessBroadbandReportResult>(
                "An error occurred while generating the report.");
        }
    }

    /// <summary>
    /// Calculate connectivity score based on CMTS criteria.
    /// Uses shared scoring service for consistency with connectivity assessment.
    /// Scores are based on 2 criteria: Internet Access (speedtest) + Technology Type (wireless infrastructure).
    /// </summary>
    private WirelessConnectivityScoreDto CalculateConnectivityScore(
        TechnologyBandBreakdownDto? techBreakdown,
        SpeedStatsDto? speedStats)
    {
        var score = new WirelessConnectivityScoreDto();

        // Calculate scores using shared service (aligned with assessment logic)
        score.TechnologyTypeScore = _scoringService.CalculateTechnologyTypeScore(techBreakdown);
        score.InternetAccessScore = _scoringService.CalculateInternetAccessScore(speedStats);
        score.HighestTechnology = _scoringService.GetHighestTechnology(techBreakdown);

        // Populate speed data if available
        if (speedStats != null && speedStats.SpeedTestCount > 0)
        {
            score.AverageDownloadSpeed = (double)(speedStats.AverageDownloadSpeedMbps ?? 0);
            score.AverageUploadSpeed = (double)(speedStats.AverageUploadSpeedMbps ?? 0);
        }

        score.TotalScore = score.TechnologyTypeScore + score.InternetAccessScore;
        score.ScoreBreakdown = $"Tech: {score.TechnologyTypeScore}/3, Internet: {score.InternetAccessScore}/3";

        return score;
    }

    /// <summary>
    /// Determine service status from total score.
    /// Uses shared scoring service for consistency with connectivity assessment.
    /// </summary>
    private ServiceStatusEnum DetermineServiceStatus(int totalScore)
    {
        return _scoringService.DetermineServiceStatus(totalScore);
    }

    private string GenerateRemarks(WirelessConnectivityScoreDto score)
    {
        var status = DetermineServiceStatus(score.TotalScore);
        var remarks = status.ToString().ToUpper();

        remarks += $" (Score: {score.TotalScore}/6)";

        if (!string.IsNullOrEmpty(score.HighestTechnology) && score.HighestTechnology != "None")
        {
            remarks += $" - {score.HighestTechnology}";
        }

        if (score.AverageDownloadSpeed.HasValue)
        {
            remarks += $" - Avg: {Math.Round(score.AverageDownloadSpeed.Value, 1)} Mbps";
        }

        return remarks;
    }

    private static WirelessBroadbandScoredSummaryDto GenerateSummary(
        List<BarangayWirelessScoredEntryDto> data)
    {
        var summary = new WirelessBroadbandScoredSummaryDto
        {
            TotalBarangays = data.Count,
            TotalMunicipalities = data.Select(d => d.MunicipalityName).Distinct().Count(),
            ServedBarangays = data.Count(d => d.ServiceStatus == ServiceStatusEnum.Served),
            UnderservedBarangays = data.Count(d => d.ServiceStatus == ServiceStatusEnum.Underserved),
            UnservedBarangays = data.Count(d => d.ServiceStatus == ServiceStatusEnum.Unserved),
            GeneratedAt = DateTime.UtcNow
        };

        if (data.Any())
        {
            summary.AverageTechnologyScore = data.Average(d => d.ConnectivityScore.TechnologyTypeScore);
            summary.AverageInternetAccessScore = data.Average(d => d.ConnectivityScore.InternetAccessScore);
            summary.AverageTotalScore = data.Average(d => d.ConnectivityScore.TotalScore);

            summary.ServedPercentage = (double)summary.ServedBarangays / summary.TotalBarangays * 100;
            summary.UnderservedPercentage = (double)summary.UnderservedBarangays / summary.TotalBarangays * 100;
            summary.UnservedPercentage = (double)summary.UnservedBarangays / summary.TotalBarangays * 100;

            // Technology distribution
            var techGroups = data
                .GroupBy(d => d.ConnectivityScore.HighestTechnology)
                .OrderByDescending(g => g.Count());

            foreach (var group in techGroups)
            {
                summary.TechnologyDistribution[group.Key] = group.Count();
            }
        }

        return summary;
    }

    // ===== Helper methods =====

    /// <summary>
    /// Map provider statistics from technology breakdown and speedtest data
    /// </summary>
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

    /// <summary>
    /// Aggregate multiple provider technology breakdowns into a single overall breakdown
    /// </summary>
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

            // Aggregate counts
            aggregated.Count2G += breakdown.Count2G;
            aggregated.Count3G += breakdown.Count3G;
            aggregated.Count4G += breakdown.Count4G;
            aggregated.Count5G += breakdown.Count5G;
        }

        // Build the dictionary
        foreach (var (tech, bands) in allTechnologies)
        {
            aggregated[tech] = bands.OrderBy(b => b).ToList();
        }

        // Calculate summary properties
        aggregated.TotalTechnologyCount = aggregated.Count2G + aggregated.Count3G + 
                                         aggregated.Count4G + aggregated.Count5G;
        
        if (aggregated.TotalTechnologyCount > 0)
        {
            aggregated.ModernCoverageRatio = (double)(aggregated.Count4G + aggregated.Count5G) / 
                                            aggregated.TotalTechnologyCount;
            aggregated.HasModernCoverage = aggregated.ModernCoverageRatio > 0;
        }

        var generations = new List<string>();
        if (aggregated.Has2G) generations.Add("2G");
        if (aggregated.Has3G) generations.Add("3G");
        if (aggregated.Has4G) generations.Add("4G");
        if (aggregated.Has5G) generations.Add("5G");
        aggregated.AvailableGenerations = generations;

        return aggregated;
    }

    /// <summary>
    /// Build SpeedStatsDto from speedtest information list
    /// </summary>
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

    /// <summary>
    /// Calculate median from a sorted list of values
    /// </summary>
    private static decimal CalculateMedian(List<decimal> sortedValues)
    {
        if (sortedValues.Count == 0) return 0;
        
        int mid = sortedValues.Count / 2;
        return sortedValues.Count % 2 == 0
            ? (sortedValues[mid - 1] + sortedValues[mid]) / 2
            : sortedValues[mid];
    }

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
        string title,
        List<Domain.Entities.Provider> providers,
        List<BarangayWirelessScoredEntryDto> data)
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

                    column.Item().AlignCenter().Text($"Wireless Broadband Report with Scores - {title}")
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

                        foreach (var item in data)
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

    // ===== Optimized Data Loading =====

    /// <summary>
    /// Load speedtests grouped by barangay using database-side spatial filtering.
    /// Each barangay gets its own simple spatial query to maximize spatial index usage.
    /// Processes in batches for progress tracking and to avoid overwhelming the database.
    /// </summary>
    private async Task<Dictionary<Guid, List<SpeedtestInfo>>> LoadSpeedtestsByBarangayAsync(
        List<Domain.Entities.Barangay> barangays,
        NetTopologySuite.Geometries.MultiPolygon? overallBoundary,
        CancellationToken cancellationToken)
    {
        if (overallBoundary == null || !barangays.Any())
            return new Dictionary<Guid, List<SpeedtestInfo>>();

        var result = new Dictionary<Guid, List<SpeedtestInfo>>();
        
        // Process barangays in batches for progress tracking
        const int batchSize = 100;
        var barangayBatches = barangays.Chunk(batchSize);

        foreach (var batch in barangayBatches)
        {
            // Query speedtests for each barangay in this batch
            // Each query is simple and leverages spatial indexes optimally
            foreach (var barangay in batch)
            {
                if (barangay.Boundaries != null)
                {
                    var barangaySpeedtests = await _context.Speedtests
                        .AsNoTracking()
                        .Where(s => s.Location != null 
                                 && s.ServiceProvider.Service.Type == ServiceTypeEnum.Wireless
                                 && barangay.Boundaries.Contains(s.Location))
                        .Select(s => new SpeedtestInfo
                        {
                            ProviderName = s.ServiceProvider.Provider.Name,
                            UploadSpeed = s.UploadSpeed,
                            DownloadSpeed = s.DownloadSpeed,
                            Location = s.Location!,
                            GenerationName = s.Generation != null ? s.Generation.Name : ""
                        })
                        .ToListAsync(cancellationToken);

                    result[barangay.Id] = barangaySpeedtests;
                }
                else
                {
                    result[barangay.Id] = new List<SpeedtestInfo>();
                }
            }
        }

        return result;
    }

    // ===== Private DTOs for data aggregation =====

    /// <summary>
    /// Speedtest information with location for spatial filtering
    /// </summary>
    private class SpeedtestInfo
    {
        public string ProviderName { get; set; } = string.Empty;
        public decimal UploadSpeed { get; set; }
        public decimal DownloadSpeed { get; set; }
        public NetTopologySuite.Geometries.Point? Location { get; set; }
        public string GenerationName { get; set; } = string.Empty;
    }

    /// <summary>
    /// Cellular location information for spatial queries
    /// </summary>
    private class CellularLocationInfo
    {
        public string ProviderName { get; set; } = string.Empty;
        public string GenerationName { get; set; } = string.Empty;
        public NetTopologySuite.Geometries.Point? Location { get; set; }
    }
}