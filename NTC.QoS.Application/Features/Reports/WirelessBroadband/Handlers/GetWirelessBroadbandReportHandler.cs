using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NTC.QoS.Application.Features.Reports.WirelessBroadband.DTOs;
using NTC.QoS.Application.Features.Reports.WirelessBroadband.Requests;
using NTC.QoS.Application.Services;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Analytics;
using NTC.QoS.Domain.Models.DTOs.Report;
using NTC.QoS.Domain.Models.DTOs.Reports;
using NTC.QoS.Infrastructure.Services;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.IO.Compression;
using System.Text.Json;
using Color = System.Drawing.Color;

namespace NTC.QoS.Application.Features.Reports.WirelessBroadband.Handlers;

/// <summary>
/// Handler for generating wireless broadband reports with optional connectivity scores.
/// When IncludeScores is true, reuses assessment scoring logic for consistency across the system.
/// Scores are based on 2 CMTS criteria: Internet Access (speedtest) and Technology Type (wireless infrastructure).
/// </summary>
public class GetWirelessBroadbandReportHandler : IRequestHandler<GetWirelessBroadbandReportQuery, ApiResponse<WirelessBroadbandReportResult>>
{
    private readonly IRegionRepository _regionRepository;
    private readonly IProvinceRepository _provinceRepository;
    private readonly IMunicipalityRepository _municipalityRepository;
    private readonly IBarangayRepository _barangayRepository;
    private readonly IBarangayCoverageRepository _barangayCoverageRepository;
    private readonly ISpeedtestRepository _speedtestRepository;
    private readonly IProviderRepository _providerRepository;
    private readonly ILogger<GetWirelessBroadbandReportHandler> _logger;
    private readonly IConnectivityAssessmentRepository _connectivityAssessmentRepository;
    private readonly IWirelessScoringService _scoringService;
    private const int StartColumn = 3;

    public GetWirelessBroadbandReportHandler(
        IRegionRepository regionRepository,
        IProvinceRepository provinceRepository,
        IMunicipalityRepository municipalityRepository,
        IBarangayRepository barangayRepository,
        IBarangayCoverageRepository barangayCoverageRepository,
        ISpeedtestRepository speedtestRepository,
        IProviderRepository providerRepository,
        ILogger<GetWirelessBroadbandReportHandler> logger,
        IConnectivityAssessmentRepository connectivityAssessmentRepository,
        IWirelessScoringService scoringService)
    {
        _regionRepository = regionRepository;
        _provinceRepository = provinceRepository;
        _municipalityRepository = municipalityRepository;
        _barangayRepository = barangayRepository;
        _barangayCoverageRepository = barangayCoverageRepository;
        _speedtestRepository = speedtestRepository;
        _providerRepository = providerRepository;
        _logger = logger;
        _connectivityAssessmentRepository = connectivityAssessmentRepository;
        _scoringService = scoringService;
    }

    public async Task<ApiResponse<WirelessBroadbandReportResult>> Handle(
        GetWirelessBroadbandReportQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            // 1. Determine Scope and Fetch Target Area
            // Apply geographic filter precedence: Municipality > Province > Region
            // Lower admin levels take priority - only the most specific filter is applied
            string reportTitleName = "";
            NetTopologySuite.Geometries.MultiPolygon? targetBoundaries = null;
            List<BarangayProjection> barangays;

            if (request.MunicipalityId.HasValue)
            {
                var munQuery = _municipalityRepository.Query(m => m.Id == request.MunicipalityId)
                    .Select(m => new { m.Name, m.Boundaries });
                var mun = await _municipalityRepository.FirstOrDefaultAsync(munQuery, cancellationToken);
                if (mun == null) return new ApiResponse<WirelessBroadbandReportResult>("Municipality not found.");
                reportTitleName = mun.Name;
                targetBoundaries = mun.Boundaries;
                
                // Only include barangays with BarangayCoverage records
                var coveredBarangayIds = _barangayCoverageRepository.Query()
                    .Select(bc => bc.BarangayId);
                
                var barangayQuery = _barangayRepository.Query(b => 
                        b.MunicipalityId == request.MunicipalityId && 
                        coveredBarangayIds.Contains(b.Id))
                    .OrderBy(b => b.Name)
                    .Select(b => new { b.Id, b.Name, MunicipalityName = b.Municipality.Name, b.Boundaries });
                var barangayProjections = await _barangayRepository.ToListAsync(barangayQuery, cancellationToken);
                barangays = barangayProjections.Select(b => new BarangayProjection 
                { 
                    Id = b.Id, 
                    Name = b.Name, 
                    MunicipalityName = b.MunicipalityName, 
                    Boundaries = b.Boundaries 
                }).ToList();
            }
            else if (request.ProvinceId.HasValue)
            {
                var provQuery = _provinceRepository.Query(p => p.Id == request.ProvinceId)
                    .Select(p => new { p.Name, p.Boundaries });
                var prov = await _provinceRepository.FirstOrDefaultAsync(provQuery, cancellationToken);
                if (prov == null) return new ApiResponse<WirelessBroadbandReportResult>("Province not found.");
                reportTitleName = prov.Name;
                targetBoundaries = prov.Boundaries;
                
                // Only include barangays with BarangayCoverage records
                var coveredBarangayIds = _barangayCoverageRepository.Query()
                    .Select(bc => bc.BarangayId);
                
                var barangayQuery = _barangayRepository.Query(b => 
                        b.Municipality.ProvinceId == request.ProvinceId && 
                        coveredBarangayIds.Contains(b.Id))
                    .OrderBy(b => b.Municipality.Name).ThenBy(b => b.Name)
                    .Select(b => new { b.Id, b.Name, MunicipalityName = b.Municipality.Name, b.Boundaries });
                var barangayProjections = await _barangayRepository.ToListAsync(barangayQuery, cancellationToken);
                barangays = barangayProjections.Select(b => new BarangayProjection 
                { 
                    Id = b.Id, 
                    Name = b.Name, 
                    MunicipalityName = b.MunicipalityName, 
                    Boundaries = b.Boundaries 
                }).ToList();
            }
            else if (request.RegionId.HasValue)
            {
                var regQuery = _regionRepository.Query(r => r.Id == request.RegionId)
                    .Select(r => new { r.Name, r.Boundaries });
                var reg = await _regionRepository.FirstOrDefaultAsync(regQuery, cancellationToken);
                if (reg == null) return new ApiResponse<WirelessBroadbandReportResult>("Region not found.");
                reportTitleName = reg.Name;
                targetBoundaries = reg.Boundaries;
                
                // Only include barangays with BarangayCoverage records
                var coveredBarangayIds = _barangayCoverageRepository.Query()
                    .Select(bc => bc.BarangayId);
                
                var barangayQuery = _barangayRepository.Query(b => 
                        b.Municipality.Province.RegionId == request.RegionId && 
                        coveredBarangayIds.Contains(b.Id))
                    .OrderBy(b => b.Municipality.Name).ThenBy(b => b.Name)
                    .Select(b => new { b.Id, b.Name, MunicipalityName = b.Municipality.Name, b.Boundaries });
                var barangayProjections = await _barangayRepository.ToListAsync(barangayQuery, cancellationToken);
                barangays = barangayProjections.Select(b => new BarangayProjection 
                { 
                    Id = b.Id, 
                    Name = b.Name, 
                    MunicipalityName = b.MunicipalityName, 
                    Boundaries = b.Boundaries 
                }).ToList();
            }
            else
            {
                return new ApiResponse<WirelessBroadbandReportResult>("Please provide a Municipality, Province, or Region ID.");
            }

            // 2. Safeguard against excessive scope
            if (barangays.Count > 5000)
            {
                return new ApiResponse<WirelessBroadbandReportResult>(
                    $"Report scope too large. Selected area contains {barangays.Count} barangays (maximum: 5,000). " +
                    "Please select a smaller geographic area (province or municipality).");
            }

            _logger.LogInformation("Generating wireless broadband report (IncludeScores={IncludeScores}) for {Count} barangays in {Area}", 
                request.IncludeScores, barangays.Count, reportTitleName);

            // 3. Fetch Providers for dynamic headers
            var providersQuery = _providerRepository.Query(
                p => p.ServiceProviders.Any(sp => sp.Service.Type == ServiceTypeEnum.Wireless))
                .OrderBy(p => p.Name);
            var providers = await _providerRepository.ToListAsync(providersQuery, cancellationToken);

            // 4. Load wireless coverage from pre-computed assessments (OPTIMIZED)
            var barangayIds = barangays.Select(b => b.Id).ToList();
            
            _logger.LogInformation("Loading wireless coverage from assessments for {Area}...", reportTitleName);
            var wirelessCoverageByBarangay = await LoadWirelessCoverageFromAssessmentsAsync(
                barangayIds,
                cancellationToken);
            _logger.LogInformation("Loaded wireless coverage for {Count}/{Total} barangays", 
                wirelessCoverageByBarangay.Count, barangays.Count);

            // 5. Process barangays with progress tracking (using scored DTO for unified approach)
            var reportData = new List<BarangayWirelessScoredEntryDto>();
            const int progressBatchSize = 500;
            var processedCount = 0;

            foreach (var barangay in barangays)
            {
                var entry = new BarangayWirelessScoredEntryDto
                {
                    MunicipalityName = barangay.MunicipalityName,
                    BarangayName = barangay.Name
                };

                // Get wireless coverage from assessment
                var wirelessSummary = wirelessCoverageByBarangay.GetValueOrDefault(barangay.Id);

                // Map provider stats from assessment data
                foreach (var provider in providers)
                {
                    if (wirelessSummary != null)
                    {
                        var providerCoverage = wirelessSummary.Providers
                            .FirstOrDefault(p => p.ProviderName.Equals(provider.Name, StringComparison.OrdinalIgnoreCase));
                        entry.ProviderStats[provider.Name] = MapProviderStatsFromAssessment(providerCoverage);
                    }
                    else
                    {
                        // No assessment data - return empty stats
                        entry.ProviderStats[provider.Name] = new ProviderWirelessStatsDto();
                    }
                }

                // Calculate scores only if requested
                if (request.IncludeScores)
                {
                    if (wirelessSummary != null)
                    {
                        var overallTechBreakdown = BuildTechnologyBreakdownFromSummary(wirelessSummary);
                        var overallSpeedStats = BuildSpeedStatsFromSummary(wirelessSummary);
                        
                        entry.ConnectivityScore = CalculateConnectivityScore(overallTechBreakdown, overallSpeedStats);
                        entry.ServiceStatus = DetermineServiceStatus(entry.ConnectivityScore.TotalScore);
                        entry.Remarks = GenerateRemarks(entry.ConnectivityScore);
                    }
                    else
                    {
                        // No assessment data - default empty score
                        entry.ConnectivityScore = new WirelessConnectivityScoreDto();
                        entry.ServiceStatus = ServiceStatusEnum.Unserved;
                        entry.Remarks = "No connectivity assessment data available";
                    }
                }

                reportData.Add(entry);
                processedCount++;

                // Log progress for large reports
                if (processedCount % progressBatchSize == 0)
                {
                    _logger.LogInformation("Processed {Processed}/{Total} barangays ({Percentage:F1}%)", 
                        processedCount, barangays.Count, (processedCount * 100.0 / barangays.Count));
                }
            }

            // Apply filtering (only applicable when scores are included)
            if (request.IncludeScores && request.FilterByStatus.HasValue)
            {
                reportData = reportData
                    .Where(r => r.ServiceStatus == request.FilterByStatus.Value)
                    .ToList();
            }

            // 6. Generate Report based on Format
            byte[] fileContent;
            string extension;

            // Collect all unique provider/generation pairs from assessment wireless coverage data
            var providerGenerationsMap = new Dictionary<string, List<string>>();
            
            foreach (var provider in providers)
            {
                var generations = new HashSet<string>();
                
                // Scan all barangay wireless summaries for this provider
                foreach (var wirelessSummary in wirelessCoverageByBarangay.Values)
                {
                    var providerCoverage = wirelessSummary.Providers
                        .FirstOrDefault(p => p.ProviderName.Equals(provider.Name, StringComparison.OrdinalIgnoreCase));
                    
                    if (providerCoverage != null)
                    {
                        if (providerCoverage.Has2G) generations.Add("2G");
                        if (providerCoverage.Has3G) generations.Add("3G");
                        if (providerCoverage.Has4G) generations.Add("4G");
                        if (providerCoverage.Has5G) generations.Add("5G");
                    }
                }
                
                // Convert to sorted list
                var genList = new List<string>();
                if (generations.Contains("2G")) genList.Add("2G");
                if (generations.Contains("3G")) genList.Add("3G");
                if (generations.Contains("4G")) genList.Add("4G");
                if (generations.Contains("5G")) genList.Add("5G");
                
                providerGenerationsMap[provider.Name] = genList;
            }

            if (request.Format == ReportFormat.Excel)
            {
                fileContent = GenerateExcel(reportTitleName, providers, reportData, providerGenerationsMap, request.IncludeScores);
                extension = "xlsx";
            }
            else
            {
                fileContent = GeneratePdf(reportTitleName, providers, reportData, providerGenerationsMap, request.IncludeScores);
                extension = "pdf";
            }

            var fileNameSuffix = request.IncludeScores ? "WirelessBroadbandScored" : "WirelessBroadband";
            var result = new WirelessBroadbandReportResult
            {
                FileContent = fileContent,
                FileName = $"{reportTitleName}_{fileNameSuffix}_{DateTime.UtcNow:yyyyMMdd}.{extension}"
            };

            return new ApiResponse<WirelessBroadbandReportResult>(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating wireless broadband report.");
            return new ApiResponse<WirelessBroadbandReportResult>("An error occurred while generating the report.");
        }
    }

    private byte[] GenerateExcel(
        string reportTitle,
        List<Domain.Entities.Provider> providers,
        List<BarangayWirelessScoredEntryDto> reportData,
        Dictionary<string, List<string>> providerGenerationsMap,
        bool includeScores)
    {
        ExcelPackage.License.SetNonCommercialPersonal("NTC QoS");
        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add(includeScores ? "Wireless Broadband Scored" : "Wireless Broadband");

        int col = StartColumn;

        // --- HEADERS ---

        // Helper to draw provider section
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

        // Add Score Columns after all providers (only if includeScores is true)
        int scoreStartCol = col;
        int totalCols;

        if (includeScores)
        {
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

            totalCols = scoreStartCol + 3;
        }
        else
        {
            totalCols = col - 1;
        }

        // Row 1: Main Title
        worksheet.Cells[1, 1, 1, totalCols].Merge = true;
        worksheet.Cells["A1"].Value = includeScores 
            ? "WIRELESS BROADBAND WITH CONNECTIVITY SCORES" 
            : "WIRELESS BROADBAND REPORT";
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

                // Score columns (only if includeScores is true)
                if (includeScores)
                {
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
        List<BarangayWirelessScoredEntryDto> data,
        Dictionary<string, List<string>> providerGenerationsMap,
        bool includeScores)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A3.Landscape());
                page.Margin(0.5f, QuestPDF.Infrastructure.Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(9));

                // Header
                page.Header().Column(column =>
                {
                    column.Item().AlignCenter().Text("NATIONAL TELECOMMUNICATIONS COMMISSION")
                        .FontSize(15).SemiBold();
                    
                    column.Item().AlignCenter().Text("Quality of Service Management System")
                        .FontSize(10).FontColor(Colors.Grey.Darken1);

                    column.Item().PaddingVertical(5).LineHorizontal(1.5f).LineColor(Colors.Black);

                    column.Item().AlignCenter().Text(includeScores 
                        ? $"Wireless Broadband Report with Scores - {title}"
                        : $"Wireless Broadband Report - {title}")
                        .FontSize(12).Bold();

                    column.Item().AlignCenter().Text($"Generated: {DateTime.Now:MMMM dd, yyyy 'at' h:mm tt}")
                        .FontSize(8).FontColor(Colors.Grey.Darken1);
                });

                page.Content()
                    .PaddingVertical(1, QuestPDF.Infrastructure.Unit.Centimetre)
                    .Table(table =>
                    {
                        // Define Columns
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(65); // Municipality
                            columns.ConstantColumn(85); // Barangay
                            
                            // Dynamic columns for providers
                            foreach (var provider in providers)
                            {
                                if (providerGenerationsMap.TryGetValue(provider.Name, out var generations))
                                {
                                    foreach (var _ in generations)
                                        columns.ConstantColumn(22);
                                    columns.ConstantColumn(35); // Tech used
                                    columns.ConstantColumn(40); // Upload
                                    columns.ConstantColumn(40); // Download
                                }
                            }

                            // Score columns (only if includeScores)
                            if (includeScores)
                            {
                                columns.ConstantColumn(35); // Tech Score
                                columns.ConstantColumn(35); // Internet Score
                                columns.ConstantColumn(35); // Total Score
                                columns.ConstantColumn(65); // Status
                            }
                        });

                        // Header
                        table.Header(header =>
                        {
                            // Row 1: Municipality and Barangay span all 4 rows
                            header.Cell().RowSpan(4).Element(CellStyle).Text("Municipality").FontSize(8);
                            header.Cell().RowSpan(4).Element(CellStyle).Text("Barangay").FontSize(8);

                            // Row 1: Provider names
                            foreach (var provider in providers)
                            {
                                if (providerGenerationsMap.TryGetValue(provider.Name, out var generations))
                                {
                                    uint providerCols = (uint)(generations.Count + 3); // Gens + Tech + Upload + Download
                                    header.Cell().ColumnSpan(providerCols).Element(CellStyle)
                                        .Background(Colors.Grey.Lighten3).Text(provider.Name.ToUpper()).FontSize(8).Bold();
                                }
                            }

                            // Row 1: Connectivity Score section
                            if (includeScores)
                            {
                                header.Cell().ColumnSpan(3).Element(CellStyle)
                                    .Background(Colors.LightBlue.Lighten1).Text("SCORE").FontSize(8).Bold();
                                header.Cell().RowSpan(4).Element(CellStyle).Text("Status").FontSize(8);
                            }

                            // Row 2: Generation headers + Speedtest header + Score sub-headers
                            foreach (var provider in providers)
                            {
                                if (providerGenerationsMap.TryGetValue(provider.Name, out var generations))
                                {
                                    foreach (var gen in generations)
                                    {
                                        header.Cell().RowSpan(3).Element(CellStyle).Text(gen).FontSize(7);
                                    }
                                    header.Cell().RowSpan(3).Element(CellStyle).Text("Tech").FontSize(7);
                                    header.Cell().ColumnSpan(2).Element(CellStyle).Text("Speed (Mbps)").FontSize(7);
                                }
                            }

                            if (includeScores)
                            {
                                header.Cell().RowSpan(3).Element(CellStyle).Text("Tech\n(1-3)").FontSize(7);
                                header.Cell().RowSpan(3).Element(CellStyle).Text("Net\n(1-3)").FontSize(7);
                                header.Cell().RowSpan(3).Element(CellStyle).Text("Total\n(1-6)").FontSize(7);
                            }

                            // Row 3 & 4: Upload/Download for each provider
                            foreach (var provider in providers)
                            {
                                if (providerGenerationsMap.TryGetValue(provider.Name, out var generations))
                                {
                                    header.Cell().Element(CellStyle).Text("Up").FontSize(7);
                                    header.Cell().Element(CellStyle).Text("Down").FontSize(7);
                                }
                            }
                        });

                        // Data
                        foreach (var item in data)
                        {
                            table.Cell().Element(CellStyle).Text(item.MunicipalityName).FontSize(7);
                            table.Cell().Element(CellStyle).Text(item.BarangayName).FontSize(7);

                            foreach (var provider in providers)
                            {
                                if (providerGenerationsMap.TryGetValue(provider.Name, out var generations))
                                {
                                    var stats = item.ProviderStats.TryGetValue(provider.Name, out var s) 
                                        ? s : new ProviderWirelessStatsDto();

                                    foreach (var gen in generations)
                                    {
                                        table.Cell().Element(CellStyle).Text(HasGeneration(stats, gen) ? "✓" : "-").FontSize(7);
                                    }
                                    table.Cell().Element(CellStyle).Text(stats.TechUsed ?? "-").FontSize(7);
                                    
                                    table.Cell().Element(CellStyle).Text(stats.UploadSpeedMbps.HasValue 
                                        ? Math.Round(stats.UploadSpeedMbps.Value, 1).ToString() 
                                        : "-").FontSize(7);
                                    table.Cell().Element(CellStyle).Text(stats.DownloadSpeedMbps.HasValue 
                                        ? Math.Round(stats.DownloadSpeedMbps.Value, 1).ToString() 
                                        : "-").FontSize(7);
                                }
                            }

                            if (includeScores)
                            {
                                table.Cell().Element(CellStyle).Text(item.ConnectivityScore.TechnologyTypeScore.ToString()).FontSize(7);
                                table.Cell().Element(CellStyle).Text(item.ConnectivityScore.InternetAccessScore.ToString()).FontSize(7);
                                table.Cell().Element(CellStyle).Text(item.ConnectivityScore.TotalScore.ToString()).FontSize(7);

                                var statusColor = item.ServiceStatus switch
                                {
                                    ServiceStatusEnum.Served => Colors.Green.Lighten3,
                                    ServiceStatusEnum.Underserved => Colors.Yellow.Lighten3,
                                    _ => Colors.Red.Lighten3
                                };

                                table.Cell().Element(CellStyle).Background(statusColor)
                                    .Text(item.ServiceStatus.ToString().ToUpper()).FontSize(7).Bold();
                            }
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
            return container.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(2).AlignMiddle().AlignCenter();
        }
    }

    /// <summary>
    /// Calculate connectivity score based on CMTS criteria.
    /// Uses shared scoring service for consistency with connectivity assessment.
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

        // Populate speed data if available (used in GenerateRemarks)
        if (speedStats != null && speedStats.SpeedTestCount > 0)
        {
            score.AverageDownloadSpeed = (double)(speedStats.AverageDownloadSpeedMbps ?? 0);
            score.AverageUploadSpeed = (double)(speedStats.AverageUploadSpeedMbps ?? 0);
        }

        score.TotalScore = score.TechnologyTypeScore + score.InternetAccessScore;
        
        // Add score breakdown for debugging and transparency
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
        // Use DetermineServiceStatus for consistency with scoring service
        var serviceStatus = DetermineServiceStatus(score.TotalScore);
        var status = serviceStatus.ToString().ToUpper();

        var remarks = status;
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

    /// <summary>
    /// Build SpeedStatsDto from BarangayWirelessSummaryDto for overall barangay scoring
    /// Optimized to use single loop instead of multiple LINQ queries
    /// </summary>
    private static SpeedStatsDto? BuildSpeedStatsFromSummary(
        BarangayWirelessSummaryDto summary)
    {
        if (summary.TotalSpeedtestCount == 0)
            return null;

        // Single loop to gather all data - more efficient than multiple LINQ queries
        int totalTests = 0;
        decimal weightedDownloadSum = 0;
        decimal weightedUploadSum = 0;
        List<decimal>? downloadMedians = null;
        List<decimal>? uploadMedians = null;

        foreach (var provider in summary.Providers)
        {
            // Sum all speedtest counts
            totalTests += provider.SpeedtestCount;
            
            // Weighted averages only from providers with tests
            if (provider.SpeedtestCount > 0)
            {
                if (provider.AverageDownloadSpeedMbps.HasValue)
                    weightedDownloadSum += provider.AverageDownloadSpeedMbps.Value * provider.SpeedtestCount;
                
                if (provider.AverageUploadSpeedMbps.HasValue)
                    weightedUploadSum += provider.AverageUploadSpeedMbps.Value * provider.SpeedtestCount;
            }

            // Collect medians from all providers (matches original LINQ behavior)
            if (provider.MedianDownloadSpeedMbps.HasValue)
            {
                downloadMedians ??= new List<decimal>(summary.Providers.Count);
                downloadMedians.Add(provider.MedianDownloadSpeedMbps.Value);
            }

            if (provider.MedianUploadSpeedMbps.HasValue)
            {
                uploadMedians ??= new List<decimal>(summary.Providers.Count);
                uploadMedians.Add(provider.MedianUploadSpeedMbps.Value);
            }
        }

        if (totalTests == 0)
            return null;

        var avgDownload = weightedDownloadSum / totalTests;
        var avgUpload = weightedUploadSum / totalTests;

        // Calculate median only if we have values
        decimal medianDownload = avgDownload;
        decimal medianUpload = avgUpload;

        if (downloadMedians != null && downloadMedians.Count > 0)
        {
            downloadMedians.Sort();
            medianDownload = CalculateMedian(downloadMedians);
        }

        if (uploadMedians != null && uploadMedians.Count > 0)
        {
            uploadMedians.Sort();
            medianUpload = CalculateMedian(uploadMedians);
        }

        return new SpeedStatsDto
        {
            SpeedTestCount = totalTests,
            AverageDownloadSpeedMbps = avgDownload,
            AverageUploadSpeedMbps = avgUpload,
            MedianDownloadSpeedMbps = medianDownload,
            MedianUploadSpeedMbps = medianUpload
        };
    }

    /// <summary>
    /// Load wireless coverage from connectivity assessments for the specified barangays.
    /// OPTIMIZATION: Reads pre-computed wireless coverage data from ConnectivityAssessment.WirelessCoverageJson
    /// instead of querying cellulars and speedtests directly (20-30x faster).
    /// </summary>
    private async Task<Dictionary<Guid, BarangayWirelessSummaryDto>> LoadWirelessCoverageFromAssessmentsAsync(
        List<Guid> barangayIds,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<Guid, BarangayWirelessSummaryDto>();
        
        if (!barangayIds.Any())
            return result;

        _logger.LogInformation(
            "Loading wireless coverage from {BarangayCount} barangay assessments...", 
            barangayIds.Count);

        // Query assessments for all barangays at once
        var assessments = await _connectivityAssessmentRepository.Query()
            .Where(a => barangayIds.Contains(a.LocationId) && a.LocationType == LocationTypeEnum.Barangay)
            .Where(a => !string.IsNullOrEmpty(a.WirelessCoverageJson))
            .Select(a => new
            {
                a.LocationId,
                a.WirelessCoverageJson,
                a.WirelessCoverageLastUpdated
            })
            .ToListAsync(cancellationToken);

        _logger.LogInformation(
            "Found {AssessmentCount} assessments with wireless coverage data out of {TotalCount} barangays",
            assessments.Count, barangayIds.Count);

        // Deserialize wireless coverage JSON for each assessment
        var successCount = 0;
        var failCount = 0;
        foreach (var assessment in assessments)
        {
            try
            {
                if (!string.IsNullOrEmpty(assessment.WirelessCoverageJson))
                {
                    var wirelessSummary = JsonSerializer.Deserialize<BarangayWirelessSummaryDto>(
                        assessment.WirelessCoverageJson,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (wirelessSummary != null)
                    {
                        result[assessment.LocationId] = wirelessSummary;
                        successCount++;
                    }
                    else
                    {
                        failCount++;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, 
                    "Failed to deserialize wireless coverage JSON for barangay {BarangayId}", 
                    assessment.LocationId);
                failCount++;
            }
        }

        _logger.LogInformation(
            "Successfully deserialized {SuccessCount} wireless coverage summaries, {FailCount} failures",
            successCount, failCount);

        return result;
    }

    /// <summary>
    /// Map BarangayWirelessCoverageDto from assessment to ProviderWirelessStatsDto for report
    /// </summary>
    private static ProviderWirelessStatsDto MapProviderStatsFromAssessment(
        BarangayWirelessCoverageDto? providerCoverage)
    {
        var stats = new ProviderWirelessStatsDto();

        if (providerCoverage == null)
            return stats;

        // Map technology availability
        stats.Has2G = providerCoverage.Has2G;
        stats.Has3G = providerCoverage.Has3G;
        stats.Has4G = providerCoverage.Has4G;
        stats.Has5G = providerCoverage.Has5G;

        // Map speedtest data
        if (providerCoverage.SpeedtestCount > 0)
        {
            stats.TechUsed = providerCoverage.MedianTechnology ?? "";
            stats.UploadSpeedMbps = (double)(providerCoverage.AverageUploadSpeedMbps ?? 0);
            stats.DownloadSpeedMbps = (double)(providerCoverage.AverageDownloadSpeedMbps ?? 0);
        }

        return stats;
    }

    /// <summary>
    /// Build TechnologyBandBreakdownDto from BarangayWirelessSummaryDto for overall barangay scoring
    /// </summary>
    private static TechnologyBandBreakdownDto BuildTechnologyBreakdownFromSummary(
        BarangayWirelessSummaryDto summary)
    {
        var breakdown = new TechnologyBandBreakdownDto();

        // Aggregate counts from all providers
        foreach (var provider in summary.Providers)
        {
            breakdown.Count2G += provider.Count2G;
            breakdown.Count3G += provider.Count3G;
            breakdown.Count4G += provider.Count4G;
            breakdown.Count5G += provider.Count5G;
        }

        breakdown.TotalTechnologyCount = breakdown.Count2G + breakdown.Count3G + 
                                         breakdown.Count4G + breakdown.Count5G;

        // Has2G, Has3G, Has4G, Has5G are computed properties (Count > 0)
        breakdown.AvailableGenerations = new List<string>();
        if (breakdown.Has2G) breakdown.AvailableGenerations.Add("2G");
        if (breakdown.Has3G) breakdown.AvailableGenerations.Add("3G");
        if (breakdown.Has4G) breakdown.AvailableGenerations.Add("4G");
        if (breakdown.Has5G) breakdown.AvailableGenerations.Add("5G");

        if (breakdown.TotalTechnologyCount > 0)
        {
            breakdown.ModernCoverageRatio = (double)(breakdown.Count4G + breakdown.Count5G) / breakdown.TotalTechnologyCount;
            breakdown.HasModernCoverage = breakdown.ModernCoverageRatio > 0;

            var genCounts = new Dictionary<string, int>
            {
                ["2G"] = breakdown.Count2G,
                ["3G"] = breakdown.Count3G,
                ["4G"] = breakdown.Count4G,
                ["5G"] = breakdown.Count5G
            };
            var maxCount = genCounts.Max(kvp => kvp.Value);
            breakdown.DominantGeneration = genCounts.FirstOrDefault(kvp => kvp.Value == maxCount).Key;
        }

        return breakdown;
    }

    private static decimal CalculateMedian(List<decimal> sortedValues)
    {
        int count = sortedValues.Count;
        
        if (count % 2 == 0)
        {
            // Even number - average of two middle values
            return (sortedValues[count / 2 - 1] + sortedValues[count / 2]) / 2;
        }
        else
        {
            // Odd number - middle value
            return sortedValues[count / 2];
        }
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

    // ===== Private DTOs for data aggregation =====

    /// <summary>
    /// Projection DTO for barangay data to avoid navigational property loading and boundary materialization
    /// </summary>
    private class BarangayProjection
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string MunicipalityName { get; set; } = string.Empty;
        public NetTopologySuite.Geometries.MultiPolygon? Boundaries { get; set; }
    }
}
