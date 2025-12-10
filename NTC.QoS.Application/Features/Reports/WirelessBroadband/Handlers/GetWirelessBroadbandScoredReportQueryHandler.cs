using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NTC.QoS.Application.Features.Reports.WirelessBroadband.DTOs;
using NTC.QoS.Application.Features.Reports.WirelessBroadband.Requests;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Report;
using NTC.QoS.Domain.Models.DTOs.Reports;
using NTC.QoS.Infrastructure.Persistence;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Drawing;
using Color = System.Drawing.Color;

namespace NTC.QoS.Application.Features.Reports.WirelessBroadband.Handlers;

public class GetWirelessBroadbandScoredReportHandler : IRequestHandler<GetWirelessBroadbandScoredReportQuery, ApiResponse<WirelessBroadbandReportResult>>
{
    private readonly IMunicipalityRepository _municipalityRepository;
    private readonly IBarangayRepository _barangayRepository;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<GetWirelessBroadbandScoredReportHandler> _logger;
    private const int StartColumn = 3;

    // Scoring thresholds based on CMTS criteria
    private const double MinimumSpeedThreshold = 25.0; // Mbps

    public GetWirelessBroadbandScoredReportHandler(
        ApplicationDbContext context,
        IMunicipalityRepository municipalityRepository,
        IBarangayRepository barangayRepository,
        ILogger<GetWirelessBroadbandScoredReportHandler> logger)
    {
        _context = context;
        _municipalityRepository = municipalityRepository;
        _barangayRepository = barangayRepository;
        _logger = logger;
    }

    public async Task<ApiResponse<WirelessBroadbandReportResult>> Handle(
        GetWirelessBroadbandScoredReportQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Generating wired broadband report with filters: RegionId={RegionId}, ProvinceId={ProvinceId}, MunicipalityId={MunicipalityId}}",
                request.RegionId, request.ProvinceId, request.MunicipalityId);
            // 1. Build municipality query with geographic filters (same as wired broadband)
            var municipalityQuery = _municipalityRepository.Query()
                .Include(m => m.Province)
                .ThenInclude(p => p.Region)
                .AsQueryable();

            // Apply filters
            if (request.RegionId.HasValue)
            {
                municipalityQuery = municipalityQuery.Where(m => m.Province.RegionId == request.RegionId.Value);
            }

            if (request.ProvinceId.HasValue)
            {
                municipalityQuery = municipalityQuery.Where(m => m.ProvinceId == request.ProvinceId.Value);
            }

            if (request.MunicipalityId.HasValue)
            {
                municipalityQuery = municipalityQuery.Where(m => m.Id == request.MunicipalityId.Value);
            }

            // Get municipalities
            var municipalities = await municipalityQuery
                .OrderBy(m => m.Name)
                .ToListAsync(cancellationToken);

            if (!municipalities.Any())
            {
                return new ApiResponse<WirelessBroadbandReportResult>("No municipalities found matching the specified criteria.");
            }

            // Determine report title and target boundaries
            string reportTitleName = "";
            NetTopologySuite.Geometries.MultiPolygon? targetBoundaries = null;

            if (request.MunicipalityId.HasValue)
            {
                var mun = municipalities.FirstOrDefault();
                if (mun == null) return new ApiResponse<WirelessBroadbandReportResult>("Municipality not found.");
                reportTitleName = mun.Name;
                targetBoundaries = mun.Boundaries;
            }
            else if (request.ProvinceId.HasValue)
            {
                var mun = municipalities.FirstOrDefault();
                if (mun?.Province == null) return new ApiResponse<WirelessBroadbandReportResult>("Province not found.");
                reportTitleName = mun.Province.Name;
                targetBoundaries = mun.Province.Boundaries;
            }
            else if (request.RegionId.HasValue)
            {
                var mun = municipalities.FirstOrDefault();
                if (mun?.Province?.Region == null) return new ApiResponse<WirelessBroadbandReportResult>("Region not found.");
                reportTitleName = mun.Province.Region.Name;
                targetBoundaries = mun.Province.Region.Boundaries;
            }

            // 2. Get all barangays for the municipalities
            var municipalityIds = municipalities.Select(m => m.Id).ToList();
            var barangays = await _barangayRepository
                .Query()
                .Where(b => municipalityIds.Contains(b.MunicipalityId))
                .Include(b => b.Municipality)
                .OrderBy(b => b.Municipality.Name)
                .ThenBy(b => b.Name)
                .ToListAsync(cancellationToken);

            // 3. Fetch Providers
            var providers = await _context.Providers
                .AsNoTracking()
                .Where(p => p.ServiceProviders.Any(sp => sp.Service.Type == ServiceTypeEnum.Wireless))
                .OrderBy(p => p.Name)
                .ToListAsync(cancellationToken);

            // 4. Fetch bulk data for optimization
            var barangayIds = barangays.Select(b => b.Id).ToList();

            var allValidations = await _context.WirelessValidations
                .AsNoTracking()
                .Include(wv => wv.ServiceProvider)
                    .ThenInclude(sp => sp.Provider)
                .Include(wv => wv.Technology)
                    .ThenInclude(t => t!.Generation)
                .Where(wv => barangayIds.Contains(wv.BarangayId))
                .Select(wv => new
                {
                    wv.BarangayId,
                    ProviderName = wv.ServiceProvider != null && wv.ServiceProvider.Provider != null
                        ? wv.ServiceProvider.Provider.Name : "",
                    GenerationName = wv.Technology != null && wv.Technology.Generation != null
                        ? wv.Technology.Generation.Name : ""
                })
                .ToListAsync(cancellationToken);

            var allCellulars = new List<CellularLocationInfo>();
            if (targetBoundaries != null)
            {
                allCellulars = await _context.Cellulars
                    .AsNoTracking()
                    .Where(c => c.CellSector != null
                             && c.CellSector.Cell != null
                             && c.CellSector.Cell.Location != null
                             && targetBoundaries.Contains(c.CellSector.Cell.Location))
                    .Select(c => new CellularLocationInfo
                    {
                        ProviderName = c.ServiceProvider != null && c.ServiceProvider.Provider != null
                            ? c.ServiceProvider.Provider.Name : "",
                        GenerationName = c.Technology != null && c.Technology.Generation != null
                            ? c.Technology.Generation.Name : "",
                        Location = c.CellSector!.Cell!.Location
                    })
                    .ToListAsync(cancellationToken);
            }

            var allSpeedtests = new List<SpeedtestInfo>();
            if (targetBoundaries != null)
            {
                allSpeedtests = await _context.Speedtests
                    .AsNoTracking()
                    .Where(s => s.Location != null && targetBoundaries.Contains(s.Location)
                             && s.ServiceProvider.Service.Type == ServiceTypeEnum.Wireless)
                    .Select(s => new SpeedtestInfo
                    {
                        ProviderName = s.ServiceProvider.Provider.Name,
                        UploadSpeed = s.UploadSpeed,
                        DownloadSpeed = s.DownloadSpeed,
                        Location = s.Location,
                        GenerationName = s.Generation != null ? s.Generation.Name : ""
                    })
                    .ToListAsync(cancellationToken);
            }

            // 5. Process barangays with scoring
            var reportData = new List<BarangayWirelessScoredEntryDto>();

            foreach (var barangay in barangays)
            {
                var entry = new BarangayWirelessScoredEntryDto
                {
                    MunicipalityName = barangay.Municipality.Name,
                    BarangayName = barangay.Name
                };

                var cellulars = new List<CellularInfo>();

                // Aggregate cellulars from spatial data
                if (barangay.Boundaries != null)
                {
                    var inBarangay = allCellulars
                        .Where(c => c.Location != null && barangay.Boundaries.Contains(c.Location))
                        .Select(c => new CellularInfo
                        {
                            ProviderName = c.ProviderName,
                            GenerationName = c.GenerationName
                        });

                    cellulars.AddRange(inBarangay);
                }

                // Add validations
                var validations = allValidations
                    .Where(v => v.BarangayId == barangay.Id)
                    .Select(v => new CellularInfo
                    {
                        ProviderName = v.ProviderName,
                        GenerationName = v.GenerationName
                    });

                cellulars.AddRange(validations);
                cellulars = cellulars.DistinctBy(c => new { c.ProviderName, c.GenerationName }).ToList();

                // Aggregate speedtests
                var barangaySpeedtests = new List<SpeedtestInfo>();
                if (barangay.Boundaries != null)
                {
                    barangaySpeedtests = allSpeedtests
                        .Where(s => s.Location != null && barangay.Boundaries.Contains(s.Location))
                        .ToList();
                }

                // Map provider stats
                foreach (var provider in providers)
                {
                    var providerCellulars = cellulars
                        .Where(c => c.ProviderName.Contains(provider.Name, StringComparison.OrdinalIgnoreCase));
                    var providerSpeedtests = barangaySpeedtests
                        .Where(s => s.ProviderName.Contains(provider.Name, StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    entry.ProviderStats[provider.Name] = MapProviderStats(providerCellulars, providerSpeedtests);
                }

                // Calculate connectivity score
                entry.ConnectivityScore = CalculateConnectivityScore(cellulars, barangaySpeedtests);
                entry.ServiceStatus = DetermineServiceStatus(entry.ConnectivityScore.TotalScore);
                entry.Remarks = GenerateRemarks(entry.ConnectivityScore);

                reportData.Add(entry);
            }

            // Apply filtering
            if (request.FilterByStatus.HasValue)
            {
                reportData = reportData
                    .Where(r => r.ServiceStatus == request.FilterByStatus.Value)
                    .ToList();
            }

            // Generate summary
            var summary = GenerateSummary(reportData);

            // 6. Generate Report
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
    /// Calculate connectivity score based on CMTS criteria
    /// </summary>
    private WirelessConnectivityScoreDto CalculateConnectivityScore(
        List<CellularInfo> cellulars,
        List<SpeedtestInfo> speedtests)
    {
        var score = new WirelessConnectivityScoreDto();

        // Determine highest technology
        var generations = cellulars.Select(c => c.GenerationName).ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Add generations from speedtests
        foreach (var st in speedtests.Where(s => !string.IsNullOrEmpty(s.GenerationName)))
        {
            generations.Add(st.GenerationName);
        }

        // Technology Type Score (1-3 points)
        // 3 = LTE/5G or high-speed technology
        // 2 = Legacy technologies (2G/3G)
        // 1 = No broadband technology
        if (generations.Contains("5G"))
        {
            score.TechnologyTypeScore = 3;
            score.HighestTechnology = "5G";
        }
        else if (generations.Contains("4G") || generations.Contains("LTE"))
        {
            score.TechnologyTypeScore = 3;
            score.HighestTechnology = "4G/LTE";
        }
        else if (generations.Contains("3G") || generations.Contains("2G"))
        {
            score.TechnologyTypeScore = 2;
            score.HighestTechnology = generations.Contains("3G") ? "3G" : "2G";
        }
        else
        {
            score.TechnologyTypeScore = 1;
            score.HighestTechnology = "None";
        }

        // Internet Access Score (1-3 points) - from Speedtest data
        // 3 = Speeds ≥25 Mbps
        // 2 = Speeds available but <25 Mbps
        // 1 = No internet access or minimal
        if (speedtests.Any())
        {
            score.AverageDownloadSpeed = (double)speedtests.Average(s => s.DownloadSpeed);
            score.AverageUploadSpeed = (double)speedtests.Average(s => s.UploadSpeed);

            if (score.AverageDownloadSpeed >= MinimumSpeedThreshold)
            {
                score.InternetAccessScore = 3;
            }
            else if (score.AverageDownloadSpeed > 0)
            {
                score.InternetAccessScore = 2;
            }
            else
            {
                score.InternetAccessScore = 1;
            }
        }
        else
        {
            // No speedtest data - infer from technology
            if (score.TechnologyTypeScore == 3)
            {
                score.InternetAccessScore = 2; // Assume available but not verified
            }
            else
            {
                score.InternetAccessScore = 1;
            }
        }

        score.TotalScore = score.TechnologyTypeScore + score.InternetAccessScore;
        score.ScoreBreakdown = $"Tech: {score.TechnologyTypeScore}/3, Internet: {score.InternetAccessScore}/3";

        return score;
    }

    /// <summary>
    /// Determine service status from total score
    /// 6 points = Served
    /// 4-5 points = Underserved
    /// 2-3 points = Unserved
    /// </summary>
    private static ServiceStatusEnum DetermineServiceStatus(int totalScore)
    {
        return totalScore switch
        {
            6 => ServiceStatusEnum.Served,
            >= 4 => ServiceStatusEnum.Underserved,
            _ => ServiceStatusEnum.Unserved
        };
    }

    private static string GenerateRemarks(WirelessConnectivityScoreDto score)
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

    // Helper methods (reused from original handler)
    private ProviderWirelessStatsDto MapProviderStats(
        IEnumerable<CellularInfo> data,
        List<SpeedtestInfo> speedtests)
    {
        var stats = new ProviderWirelessStatsDto();
        var generations = data.Select(d => d.GenerationName).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var st in speedtests.Where(s => !string.IsNullOrEmpty(s.GenerationName)))
        {
            generations.Add(st.GenerationName);
        }

        stats.Has2G = generations.Contains("2G");
        stats.Has3G = generations.Contains("3G");
        stats.Has4G = generations.Contains("4G") || generations.Contains("LTE");
        stats.Has5G = generations.Contains("5G");

        var speedtestGenerations = speedtests
            .Where(s => !string.IsNullOrEmpty(s.GenerationName))
            .Select(s => s.GenerationName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => g)
            .ToList();

        stats.TechUsed = speedtestGenerations.Any() ? string.Join(", ", speedtestGenerations) : "";

        if (speedtests.Any())
        {
            stats.UploadSpeedMbps = (double)speedtests.Average(s => s.UploadSpeed);
            stats.DownloadSpeedMbps = (double)speedtests.Average(s => s.DownloadSpeed);
        }

        return stats;
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
        var allCellularData = new List<CellularInfo>();

        foreach (var entry in reportData)
        {
            foreach (var providerStat in entry.ProviderStats)
            {
                if (providerStat.Value.Has2G) allCellularData.Add(new CellularInfo { ProviderName = providerStat.Key, GenerationName = "2G" });
                if (providerStat.Value.Has3G) allCellularData.Add(new CellularInfo { ProviderName = providerStat.Key, GenerationName = "3G" });
                if (providerStat.Value.Has4G) allCellularData.Add(new CellularInfo { ProviderName = providerStat.Key, GenerationName = "4G" });
                if (providerStat.Value.Has5G) allCellularData.Add(new CellularInfo { ProviderName = providerStat.Key, GenerationName = "5G" });
            }
        }

        foreach (var provider in providers)
        {
            var generations = GetPresentGenerations(allCellularData, provider.Name);
            providerGenerationsMap[provider.Name] = generations;
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
            if (!providerGenerationsMap.ContainsKey(provider.Name)) continue;

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
                providerGenerationsMap[provider.Name],
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

                    var stats = item.ProviderStats.ContainsKey(provider.Name)
                        ? item.ProviderStats[provider.Name]
                        : new ProviderWirelessStatsDto();

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

        // Borders for data
        var dataRange = worksheet.Cells[5, 1, row - 1, totalCols];
        dataRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
        dataRange.Style.Border.Left.Style = ExcelBorderStyle.Thin;
        dataRange.Style.Border.Right.Style = ExcelBorderStyle.Thin;
        dataRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
        dataRange.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

        // Left-align barangay names
        worksheet.Cells[5, 2, row - 1, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;

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
                            foreach (var provider in providers)
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
                                var hasService = item.ProviderStats.ContainsKey(provider.Name)
                                    && (item.ProviderStats[provider.Name].Has4G
                                        || item.ProviderStats[provider.Name].Has5G
                                        || item.ProviderStats[provider.Name].Has3G);

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

    private List<string> GetPresentGenerations(IEnumerable<CellularInfo> data, string providerName)
    {
        var providerData = data.Where(d => d.ProviderName.Contains(providerName, StringComparison.OrdinalIgnoreCase));
        var rawGens = providerData.Select(d => d.GenerationName).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var present = new List<string>();
        if (rawGens.Contains("2G")) present.Add("2G");
        if (rawGens.Contains("3G")) present.Add("3G");
        if (rawGens.Contains("4G") || rawGens.Contains("LTE")) present.Add("4G");
        if (rawGens.Contains("5G")) present.Add("5G");

        return present;
    }

    private bool HasGeneration(ProviderWirelessStatsDto stats, string generation)
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

    private class CellularInfo
    {
        public string ProviderName { get; set; } = string.Empty;
        public string GenerationName { get; set; } = string.Empty;
    }

    private class CellularLocationInfo
    {
        public string ProviderName { get; set; } = string.Empty;
        public string GenerationName { get; set; } = string.Empty;
        public NetTopologySuite.Geometries.Point? Location { get; set; }
    }

    private class SpeedtestInfo
    {
        public string ProviderName { get; set; } = string.Empty;
        public decimal UploadSpeed { get; set; }
        public decimal DownloadSpeed { get; set; }
        public NetTopologySuite.Geometries.Point? Location { get; set; }
        public string GenerationName { get; set; } = string.Empty;
    }
}