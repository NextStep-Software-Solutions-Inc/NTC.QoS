using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NTC.QoS.Application.Features.Reports.WirelessBroadband.DTOs;
using NTC.QoS.Application.Features.Reports.WirelessBroadband.Requests;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Report;
using NTC.QoS.Infrastructure.Persistence;
using NTC.QoS.Domain.Enums;
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
    private readonly ApplicationDbContext _context;
    private readonly ILogger<GetWirelessBroadbandReportHandler> _logger;
    private const int StartColumn = 3;

    public GetWirelessBroadbandReportHandler(
        ApplicationDbContext context,
        ILogger<GetWirelessBroadbandReportHandler> logger)
    {
        _context = context;
        _logger = logger;
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
            List<Domain.Entities.Barangay> barangays;

            if (request.MunicipalityId.HasValue)
            {
                var mun = await _context.Municipalities.AsNoTracking().FirstOrDefaultAsync(m => m.Id == request.MunicipalityId, cancellationToken);
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
                var prov = await _context.Provinces.AsNoTracking().FirstOrDefaultAsync(p => p.Id == request.ProvinceId, cancellationToken);
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
                var reg = await _context.Regions.AsNoTracking().FirstOrDefaultAsync(r => r.Id == request.RegionId, cancellationToken);
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
                return new ApiResponse<WirelessBroadbandReportResult>("Please provide a Municipality, Province, or Region ID.");
            }

            // 2. Prepare Data
            // Fetch Providers for dynamic headers
            var providers = await _context.Providers
                .AsNoTracking()
                .Where(p => p.ServiceProviders.Any(sp => sp.Service.Type == ServiceTypeEnum.Wireless))
                .OrderBy(p => p.Name)
                .ToListAsync(cancellationToken);

            // Optimization: Fetch all data for the target area in bulk
            
            // Fetch all WirelessValidations for these barangays
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
                    ProviderName = wv.ServiceProvider != null && wv.ServiceProvider.Provider != null ? wv.ServiceProvider.Provider.Name : "",
                    GenerationName = wv.Technology != null && wv.Technology.Generation != null ? wv.Technology.Generation.Name : ""
                })
                .ToListAsync(cancellationToken);

            // Fetch all Cellulars within Target Boundaries
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
                        ProviderName = c.ServiceProvider != null && c.ServiceProvider.Provider != null ? c.ServiceProvider.Provider.Name : "",
                        GenerationName = c.Technology != null && c.Technology.Generation != null ? c.Technology.Generation.Name : "",
                        Location = c.CellSector!.Cell!.Location
                    })
                    .ToListAsync(cancellationToken);
            }

            // Fetch Speedtests within Target Boundaries
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

            var reportData = new List<BarangayWirelessEntryDto>();

            foreach (var barangay in barangays)
            {
                var entry = new BarangayWirelessEntryDto
                {
                    MunicipalityName = barangay.Municipality.Name,
                    BarangayName = barangay.Name
                };

                var cellulars = new List<CellularInfo>();

                // 1. From Spatial Cellulars
                if (barangay.Boundaries != null)
                {
                    var inBarangay = allCellulars
                        .Where(c => c.Location != null && barangay.Boundaries.Contains(c.Location))
                        .Select(c => new CellularInfo { ProviderName = c.ProviderName, GenerationName = c.GenerationName });
                    
                    cellulars.AddRange(inBarangay);
                }

                // 2. From WirelessValidation
                var validations = allValidations
                    .Where(v => v.BarangayId == barangay.Id)
                    .Select(v => new CellularInfo { ProviderName = v.ProviderName, GenerationName = v.GenerationName });
                
                cellulars.AddRange(validations);

                // Distinct
                cellulars = cellulars.DistinctBy(c => new { c.ProviderName, c.GenerationName }).ToList();

                // 3. Speedtests
                var barangaySpeedtests = new List<SpeedtestInfo>();
                if (barangay.Boundaries != null)
                {
                    barangaySpeedtests = allSpeedtests
                        .Where(s => s.Location != null && barangay.Boundaries.Contains(s.Location))
                        .ToList();
                }

                foreach (var provider in providers)
                {
                    var providerCellulars = cellulars.Where(c => c.ProviderName.Contains(provider.Name, StringComparison.OrdinalIgnoreCase));
                    var providerSpeedtests = barangaySpeedtests.Where(s => s.ProviderName.Contains(provider.Name, StringComparison.OrdinalIgnoreCase)).ToList();
                    
                    entry.ProviderStats[provider.Name] = MapProviderStats(providerCellulars, providerSpeedtests);
                }

                reportData.Add(entry);
            }

            // 3. Generate Report based on Format
            byte[] fileContent;
            string extension;

            if (request.Format == ReportFormat.Excel)
            {
                ExcelPackage.License.SetNonCommercialPersonal("NTC QoS");
                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("Wireless Broadband");

                // Collect all unique provider/generation pairs for the whole target area to determine columns
                var allAreaData = new List<CellularInfo>();
                if (targetBoundaries != null)
                {
                    allAreaData.AddRange(allCellulars.Select(c => new CellularInfo { ProviderName = c.ProviderName, GenerationName = c.GenerationName }));
                    allAreaData.AddRange(allSpeedtests.Select(s => new CellularInfo { ProviderName = s.ProviderName, GenerationName = s.GenerationName }));
                }
                allAreaData.AddRange(allValidations.Select(v => new CellularInfo { ProviderName = v.ProviderName, GenerationName = v.GenerationName }));

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
                var providerGenerationsMap = new Dictionary<string, List<string>>();

                foreach (var provider in providers)
                {
                    var generations = GetPresentGenerations(allAreaData, provider.Name);
                    providerGenerationsMap[provider.Name] = generations;

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
                // Collect all unique provider/generation pairs for the whole target area to determine columns
                var allAreaData = new List<CellularInfo>();
                if (targetBoundaries != null)
                {
                    allAreaData.AddRange(allCellulars.Select(c => new CellularInfo { ProviderName = c.ProviderName, GenerationName = c.GenerationName }));
                    allAreaData.AddRange(allSpeedtests.Select(s => new CellularInfo { ProviderName = s.ProviderName, GenerationName = s.GenerationName }));
                }
                allAreaData.AddRange(allValidations.Select(v => new CellularInfo { ProviderName = v.ProviderName, GenerationName = v.GenerationName }));

                var providerGenerationsMap = new Dictionary<string, List<string>>();
                foreach (var provider in providers)
                {
                    var generations = GetPresentGenerations(allAreaData, provider.Name);
                    providerGenerationsMap[provider.Name] = generations;
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

    private ProviderWirelessStatsDto MapProviderStats(IEnumerable<CellularInfo> data, List<SpeedtestInfo> speedtests)
    {
        var stats = new ProviderWirelessStatsDto();
        var generations = data.Select(d => d.GenerationName).ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Add generations found in speedtests
        foreach (var st in speedtests)
        {
            if (!string.IsNullOrEmpty(st.GenerationName))
            {
                generations.Add(st.GenerationName);
            }
        }

        stats.Has2G = generations.Contains("2G");
        stats.Has3G = generations.Contains("3G");
        stats.Has4G = generations.Contains("4G") || generations.Contains("LTE");
        stats.Has5G = generations.Contains("5G");

        // Populate TechUsed from Speedtests
        var speedtestGenerations = speedtests
            .Where(s => !string.IsNullOrEmpty(s.GenerationName))
            .Select(s => s.GenerationName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => g)
            .ToList();

        if (speedtestGenerations.Any())
        {
            stats.TechUsed = string.Join(", ", speedtestGenerations);
        }
        else
        {
            stats.TechUsed = "";
        }

        // Calculate Average Speed
        if (speedtests.Any())
        {
            stats.UploadSpeedMbps = (double)speedtests.Average(s => s.UploadSpeed);
            stats.DownloadSpeedMbps = (double)speedtests.Average(s => s.DownloadSpeed);
        }

        return stats;
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
