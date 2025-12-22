using MediatR;
using Microsoft.Extensions.Logging;
using NTC.QoS.Application.Features.Reports.WirelessBroadband.DTOs;
using NTC.QoS.Application.Features.Reports.WirelessBroadband.Requests;
using NTC.QoS.Application.Services;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Analytics;
using NTC.QoS.Domain.Models.DTOs.Report;
using NTC.QoS.Infrastructure.Services;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.IO.Compression;
using Color = System.Drawing.Color;

namespace NTC.QoS.Application.Features.Reports.WirelessBroadband.Handlers;

public class GetWirelessBroadbandReportHandler : IRequestHandler<GetWirelessBroadbandReportQuery, ApiResponse<WirelessBroadbandReportResult>>
{
    private readonly IRegionRepository _regionRepository;
    private readonly IProvinceRepository _provinceRepository;
    private readonly IMunicipalityRepository _municipalityRepository;
    private readonly IBarangayRepository _barangayRepository;
    private readonly ISpeedtestRepository _speedtestRepository;
    private readonly IProviderRepository _providerRepository;
    private readonly ILogger<GetWirelessBroadbandReportHandler> _logger;
    private readonly IProviderTechnologyService _providerTechService;
    private const int StartColumn = 3;

    public GetWirelessBroadbandReportHandler(
        IRegionRepository regionRepository,
        IProvinceRepository provinceRepository,
        IMunicipalityRepository municipalityRepository,
        IBarangayRepository barangayRepository,
        ISpeedtestRepository speedtestRepository,
        IProviderRepository providerRepository,
        ILogger<GetWirelessBroadbandReportHandler> logger,
        IProviderTechnologyService providerTechService)
    {
        _regionRepository = regionRepository;
        _provinceRepository = provinceRepository;
        _municipalityRepository = municipalityRepository;
        _barangayRepository = barangayRepository;
        _speedtestRepository = speedtestRepository;
        _providerRepository = providerRepository;
        _logger = logger;
        _providerTechService = providerTechService;
    }

    public async Task<ApiResponse<WirelessBroadbandReportResult>> Handle(
        GetWirelessBroadbandReportQuery request,
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
                var munQuery = _municipalityRepository.Query(m => m.Id == request.MunicipalityId);
                var mun = await _municipalityRepository.FirstOrDefaultAsync(munQuery, cancellationToken);
                if (mun == null) return new ApiResponse<WirelessBroadbandReportResult>("Municipality not found.");
                reportTitleName = mun.Name;
                targetBoundaries = mun.Boundaries;
                
                var barangayQuery = _barangayRepository.Query(b => b.MunicipalityId == request.MunicipalityId)
                    .OrderBy(b => b.Name);
                barangays = await _barangayRepository.ToListAsync(barangayQuery, cancellationToken);
            }
            else if (request.ProvinceId.HasValue)
            {
                var provQuery = _provinceRepository.Query(p => p.Id == request.ProvinceId);
                var prov = await _provinceRepository.FirstOrDefaultAsync(provQuery, cancellationToken);
                if (prov == null) return new ApiResponse<WirelessBroadbandReportResult>("Province not found.");
                reportTitleName = prov.Name;
                targetBoundaries = prov.Boundaries;
                
                var barangayQuery = _barangayRepository.Query(b => b.Municipality.ProvinceId == request.ProvinceId)
                    .OrderBy(b => b.Municipality.Name).ThenBy(b => b.Name);
                barangays = await _barangayRepository.ToListAsync(barangayQuery, cancellationToken);
            }
            else if (request.RegionId.HasValue)
            {
                var regQuery = _regionRepository.Query(r => r.Id == request.RegionId);
                var reg = await _regionRepository.FirstOrDefaultAsync(regQuery, cancellationToken);
                if (reg == null) return new ApiResponse<WirelessBroadbandReportResult>("Region not found.");
                reportTitleName = reg.Name;
                targetBoundaries = reg.Boundaries;
                
                var barangayQuery = _barangayRepository.Query(b => b.Municipality.Province.RegionId == request.RegionId)
                    .OrderBy(b => b.Municipality.Name).ThenBy(b => b.Name);
                barangays = await _barangayRepository.ToListAsync(barangayQuery, cancellationToken);
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

            _logger.LogInformation("Generating wireless broadband report for {Count} barangays in {Area}", 
                barangays.Count, reportTitleName);

            // 3. Fetch Providers for dynamic headers
            var providersQuery = _providerRepository.Query(
                p => p.ServiceProviders.Any(sp => sp.Service.Type == ServiceTypeEnum.Wireless))
                .OrderBy(p => p.Name);
            var providers = await _providerRepository.ToListAsync(providersQuery, cancellationToken);

            // 4. Optimize data fetching using batch operations
            var barangayIds = barangays.Select(b => b.Id).ToList();

            // Batch calculate provider technology breakdowns for all barangays (optimized - 3 queries total)
            var batchTechBreakdowns = await _providerTechService.CalculateBatchProviderTechnologyBreakdownAsync(
                barangayIds,
                targetBoundaries,
                cancellationToken);

            // Query speedtests using spatial filter (database-side, leveraging spatial indexes)
            _logger.LogInformation("Loading speedtests for {Area}...", reportTitleName);
            
            var speedtestsByBarangay = await LoadSpeedtestsByBarangayAsync(
                barangays, 
                targetBoundaries, 
                cancellationToken);
            
            _logger.LogInformation("Loaded speedtests for {Count} barangays", speedtestsByBarangay.Count);

            // 5. Process barangays with progress tracking
            var reportData = new List<BarangayWirelessEntryDto>();
            const int progressBatchSize = 500;
            var processedCount = 0;

            foreach (var barangay in barangays)
            {
                var entry = new BarangayWirelessEntryDto
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
                    var providerTechData = providerTechBreakdown.GetValueOrDefault(provider.Name);
                    var providerSpeedtests = barangaySpeedtests
                        .Where(s => s.ProviderName.Contains(provider.Name, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                    
                    entry.ProviderStats[provider.Name] = MapProviderStats(providerTechData, providerSpeedtests);
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

            // 6. Generate Report based on Format
            byte[] fileContent;
            string extension;

            if (request.Format == ReportFormat.Excel)
            {
                ExcelPackage.License.SetNonCommercialPersonal("NTC QoS");
                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("Wireless Broadband");

                // Collect all unique provider/generation pairs from technology breakdowns
                var providerGenerationsMap = new Dictionary<string, List<string>>();
                
                foreach (var provider in providers)
                {
                    var generations = new HashSet<string>();
                    
                    // Scan all barangay tech breakdowns for this provider
                    foreach (var techBreakdown in batchTechBreakdowns.Values)
                    {
                        if (techBreakdown.TryGetValue(provider.Name, out var breakdown))
                        {
                            if (breakdown.Has2G) generations.Add("2G");
                            if (breakdown.Has3G) generations.Add("3G");
                            if (breakdown.Has4G) generations.Add("4G");
                            if (breakdown.Has5G) generations.Add("5G");
                        }
                    }
                    
                    // Add generations from speedtests
                    foreach (var speedtests in speedtestsByBarangay.Values)
                    {
                        foreach (var st in speedtests.Where(s => s.ProviderName.Contains(provider.Name, StringComparison.OrdinalIgnoreCase)))
                        {
                            if (!string.IsNullOrEmpty(st.GenerationName))
                            {
                                if (st.GenerationName.Equals("2G", StringComparison.OrdinalIgnoreCase)) generations.Add("2G");
                                if (st.GenerationName.Equals("3G", StringComparison.OrdinalIgnoreCase)) generations.Add("3G");
                                if (st.GenerationName.Equals("4G", StringComparison.OrdinalIgnoreCase) || 
                                    st.GenerationName.Equals("LTE", StringComparison.OrdinalIgnoreCase)) generations.Add("4G");
                                if (st.GenerationName.Equals("5G", StringComparison.OrdinalIgnoreCase)) generations.Add("5G");
                            }
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

                // --- Headers ---
                int col = StartColumn;

                // Helper to draw provider section
                void DrawProviderSection(string providerName, string displayName, List<string> generations, Color color)
                {
                    int startCol = col;
                    int width = generations.Count + 1 + 2; // Gens + Tech + Speedtest(2)
                    int endCol = startCol + width - 1;

                    // Header Row 2
                    worksheet.Cells[2, startCol, 2, endCol].Merge = true;
                    worksheet.Cells[2, startCol].Value = displayName;
                    worksheet.Cells[2, startCol].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheet.Cells[2, startCol].Style.Fill.BackgroundColor.SetColor(color);

                    // Sub-headers Row 3
                    foreach (var gen in generations)
                    {
                        worksheet.Cells[3, col].Value = gen;
                        worksheet.Cells[3, col, 4, col].Merge = true; // Merge down
                        col++;
                    }

                    worksheet.Cells[3, col].Value = "Tech used";
                    worksheet.Cells[3, col, 4, col].Merge = true;
                    col++;

                    worksheet.Cells[3, col].Value = "Speedtest (x~) (Mbps)";
                    worksheet.Cells[3, col, 3, col + 1].Merge = true;

                    worksheet.Cells[4, col].Value = "Upload";
                    worksheet.Cells[4, col + 1].Value = "Download";
                    col += 2;
                }

                // Dynamic Provider Sections
                foreach (var provider in providers)
                {
                    if (!providerGenerationsMap.TryGetValue(provider.Name, out var generations)) continue;

                    Color color;
                    try
                    {
                        // Try to parse color from DB, fallback if invalid
                        color = !string.IsNullOrWhiteSpace(provider.Color) 
                            ? ColorTranslator.FromHtml(provider.Color) 
                            : Color.LightGray;
                    }
                    catch
                    {
                        color = Color.LightGray;
                    }

                    DrawProviderSection(provider.Name, provider.Name.ToUpper(), generations, color);
                }

                // Row 1: Title (Merge across all columns)
                int totalCols = col - 1;
                worksheet.Cells[1, 1, 1, totalCols].Merge = true;
                worksheet.Cells["A1"].Value = "WIRELESS BROADBAND";
                worksheet.Cells["A1"].Style.Font.Bold = true;
                worksheet.Cells["A1"].Style.Font.Size = 14;
                worksheet.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheet.Cells["A1"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                worksheet.Cells["A1"].Style.Fill.BackgroundColor.SetColor(Color.Cyan);

                // Row 2: Provider Headers (Municipality & Barangay)
                worksheet.Cells["A2:A4"].Merge = true;
                worksheet.Cells["A2"].Value = "Municipality";
                worksheet.Cells["B2:B4"].Merge = true;
                worksheet.Cells["B2"].Value = "Barangay";

                // Center align headers
                worksheet.Cells[2, 1, 4, totalCols].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheet.Cells[2, 1, 4, totalCols].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                worksheet.Cells[2, 1, 4, totalCols].Style.Font.Bold = true;
                worksheet.Cells[2, 1, 4, totalCols].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                worksheet.Cells[2, 1, 4, totalCols].Style.Border.Left.Style = ExcelBorderStyle.Thin;
                worksheet.Cells[2, 1, 4, totalCols].Style.Border.Right.Style = ExcelBorderStyle.Thin;
                worksheet.Cells[2, 1, 4, totalCols].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

                // --- Data ---
                int row = 5;
                
                // Group by Municipality to handle merging
                var groupedData = reportData.GroupBy(x => x.MunicipalityName);
                
                foreach (var group in groupedData)
                {
                    int startRow = row;
                    foreach (var item in group)
                    {
                        worksheet.Cells[row, 2].Value = item.BarangayName;
                        int currentCol = StartColumn;

                        // Dynamic Providers
                        foreach (var provider in providers)
                        {
                            if (providerGenerationsMap.TryGetValue(provider.Name, out var generations))
                            {
                                var stats = item.ProviderStats.ContainsKey(provider.Name) ? item.ProviderStats[provider.Name] : new ProviderWirelessStatsDto();
                                
                                foreach (var gen in generations)
                                {
                                    if (HasGeneration(stats, gen)) worksheet.Cells[row, currentCol].Value = "✓";
                                    currentCol++;
                                }
                                worksheet.Cells[row, currentCol].Value = stats.TechUsed;
                                currentCol++; // Move past Tech Used

                                // Speedtest Data
                                if (stats.UploadSpeedMbps.HasValue)
                                    worksheet.Cells[row, currentCol].Value = Math.Round(stats.UploadSpeedMbps.Value, 2);
                                
                                if (stats.DownloadSpeedMbps.HasValue)
                                    worksheet.Cells[row, currentCol + 1].Value = Math.Round(stats.DownloadSpeedMbps.Value, 2);
                                
                                currentCol += 2; // Move past Speedtest columns
                            }
                        }

                        row++;
                    }
                    
                    // Merge Municipality Column
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
                // Align barangay names to the left
                worksheet.Cells[5, 2, row - 1, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;

                worksheet.Cells.AutoFitColumns();
                
                // Freeze Panes
                worksheet.View.FreezePanes(5, 3);

                fileContent = package.GetAsByteArray();
                extension = "xlsx";
            }
            else
            {
                // Collect all unique provider/generation pairs from technology breakdowns
                var providerGenerationsMap = new Dictionary<string, List<string>>();
                
                foreach (var provider in providers)
                {
                    var generations = new HashSet<string>();
                    
                    // Scan all barangay tech breakdowns for this provider
                    foreach (var techBreakdown in batchTechBreakdowns.Values)
                    {
                        if (techBreakdown.TryGetValue(provider.Name, out var breakdown))
                        {
                            if (breakdown.Has2G) generations.Add("2G");
                            if (breakdown.Has3G) generations.Add("3G");
                            if (breakdown.Has4G) generations.Add("4G");
                            if (breakdown.Has5G) generations.Add("5G");
                        }
                    }
                    
                    // Add generations from speedtests
                    foreach (var speedtests in speedtestsByBarangay.Values)
                    {
                        foreach (var st in speedtests.Where(s => s.ProviderName.Contains(provider.Name, StringComparison.OrdinalIgnoreCase)))
                        {
                            if (!string.IsNullOrEmpty(st.GenerationName))
                            {
                                if (st.GenerationName.Equals("2G", StringComparison.OrdinalIgnoreCase)) generations.Add("2G");
                                if (st.GenerationName.Equals("3G", StringComparison.OrdinalIgnoreCase)) generations.Add("3G");
                                if (st.GenerationName.Equals("4G", StringComparison.OrdinalIgnoreCase) || 
                                    st.GenerationName.Equals("LTE", StringComparison.OrdinalIgnoreCase)) generations.Add("4G");
                                if (st.GenerationName.Equals("5G", StringComparison.OrdinalIgnoreCase)) generations.Add("5G");
                            }
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

                fileContent = GeneratePdf(reportTitleName, providers, reportData, providerGenerationsMap);
                extension = "pdf";
            }

            var result = new WirelessBroadbandReportResult
            {
                FileContent = fileContent,
                FileName = $"{reportTitleName}_WirelessBroadbandReport_{DateTime.UtcNow:yyyyMMdd}.{extension}"
            };

            return new ApiResponse<WirelessBroadbandReportResult>(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating wireless broadband report.");
            return new ApiResponse<WirelessBroadbandReportResult>("An error occurred while generating the report.");
        }
    }

    private byte[] GeneratePdf(
        string title, 
        List<Domain.Entities.Provider> providers, 
        List<BarangayWirelessEntryDto> data,
        Dictionary<string, List<string>> providerGenerationsMap)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(1, QuestPDF.Infrastructure.Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(9));

                // Updated Header to match the image style
                page.Header().Column(column =>
                {
                    column.Item().AlignCenter().Text("NATIONAL TELECOMMUNICATIONS COMMISSION")
                        .FontSize(15).SemiBold();
                    
                    column.Item().AlignCenter().Text("Quality of Service Management System")
                        .FontSize(10).FontColor(Colors.Grey.Darken1);

                    column.Item().PaddingVertical(5).LineHorizontal(1.5f).LineColor(Colors.Black);

                    column.Item().AlignCenter().Text($"Wireless Broadband Report - {title}")
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
                            columns.ConstantColumn(100); // Municipality
                            columns.ConstantColumn(100); // Barangay
                            
                            // Dynamic columns for providers
                            foreach (var provider in providers)
                            {
                                if (providerGenerationsMap.TryGetValue(provider.Name, out var generations))
                                {
                                    // 1 col per generation + Tech + Upload + Download
                                    for(int i=0; i < generations.Count + 3; i++) 
                                        columns.RelativeColumn();
                                }
                            }
                        });

                        // Header
                        table.Header(header =>
                        {
                            // Row 1: Provider Names
                            header.Cell().RowSpan(2).Element(CellStyle).Text("Municipality");
                            header.Cell().RowSpan(2).Element(CellStyle).Text("Barangay");

                            foreach (var provider in providers)
                            {
                                if (providerGenerationsMap.TryGetValue(provider.Name, out var generations))
                                {
                                    int span = generations.Count + 3;
                                    header.Cell().ColumnSpan((uint)span).Element(CellStyle).Background(Colors.Grey.Lighten3).Text(provider.Name);
                                }
                            }

                            // Row 2: Subheaders
                            foreach (var provider in providers)
                            {
                                if (providerGenerationsMap.TryGetValue(provider.Name, out var generations))
                                {
                                    foreach (var gen in generations)
                                    {
                                        header.Cell().Element(CellStyle).Text(gen);
                                    }
                                    header.Cell().Element(CellStyle).Text("Tech");
                                    header.Cell().Element(CellStyle).Text("Up (Mbps)");
                                    header.Cell().Element(CellStyle).Text("Down (Mbps)");
                                }
                            }
                        });

                        // Data
                        foreach (var item in data)
                        {
                            table.Cell().Element(CellStyle).Text(item.MunicipalityName);
                            table.Cell().Element(CellStyle).Text(item.BarangayName);

                            foreach (var provider in providers)
                            {
                                if (providerGenerationsMap.TryGetValue(provider.Name, out var generations))
                                {
                                    var stats = item.ProviderStats.ContainsKey(provider.Name) ? item.ProviderStats[provider.Name] : new ProviderWirelessStatsDto();

                                    foreach (var gen in generations)
                                    {
                                        table.Cell().Element(CellStyle).Text(HasGeneration(stats, gen) ? "✓" : "");
                                    }
                                    table.Cell().Element(CellStyle).Text(stats.TechUsed);
                                    
                                    table.Cell().Element(CellStyle).Text(stats.UploadSpeedMbps.HasValue ? Math.Round(stats.UploadSpeedMbps.Value, 1).ToString() : "-");
                                    table.Cell().Element(CellStyle).Text(stats.DownloadSpeedMbps.HasValue ? Math.Round(stats.DownloadSpeedMbps.Value, 1).ToString() : "-");
                                }
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

    private ProviderWirelessStatsDto MapProviderStats(TechnologyBandBreakdownDto? techBreakdown, List<SpeedtestInfo> speedtests)
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
        else
        {
            stats.TechUsed = "";
        }

        return stats;
    }

    /// <summary>
    /// Load speedtests grouped by barangay using database-side spatial filtering.
    /// Optimized to use individual spatial queries per barangay to leverage spatial indexes.
    /// This is faster than loading all data into memory for millions of records.
    /// </summary>
    private async Task<Dictionary<Guid, List<SpeedtestInfo>>> LoadSpeedtestsByBarangayAsync(
        List<Domain.Entities.Barangay> barangays,
        NetTopologySuite.Geometries.MultiPolygon? overallBoundary,
        CancellationToken cancellationToken)
    {
        if (overallBoundary == null || !barangays.Any())
            return new Dictionary<Guid, List<SpeedtestInfo>>();

        var result = new Dictionary<Guid, List<SpeedtestInfo>>();
        
        // Process barangays individually with database-side spatial queries
        // This leverages spatial indexes optimally and avoids loading millions into memory
        const int batchSize = 50; // Smaller batches for better progress tracking
        var barangayBatches = barangays.Chunk(batchSize);
        int processedCount = 0;

        foreach (var batch in barangayBatches)
        {
            foreach (var barangay in batch)
            {
                if (barangay.Boundaries != null)
                {
                    var speedtestQuery = _speedtestRepository.QueryAsNoTracking(
                        s => s.Location != null 
                         && barangay.Boundaries.Contains(s.Location)
                         && s.ServiceProvider.Service.Type == ServiceTypeEnum.Wireless)
                        .Select(s => new SpeedtestInfo
                        {
                            ProviderName = s.ServiceProvider.Provider.Name,
                            UploadSpeed = s.UploadSpeed,
                            DownloadSpeed = s.DownloadSpeed,
                            Location = s.Location,
                            GenerationName = s.Generation != null ? s.Generation.Name : ""
                        });
                    
                    var speedtests = await _speedtestRepository.ToListAsync(speedtestQuery, cancellationToken);

                    result[barangay.Id] = speedtests;
                }
                else
                {
                    result[barangay.Id] = new List<SpeedtestInfo>();
                }
            }

            processedCount += batch.Length;
            if (processedCount % 500 == 0)
            {
                _logger.LogInformation("Loaded speedtests for {Processed}/{Total} barangays", 
                    processedCount, barangays.Count);
            }
        }

        return result;
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
}
