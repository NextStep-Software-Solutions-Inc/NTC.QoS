using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NTC.QoS.Application.Features.Reports.Queries.GetWirelessBroadbandReport;
using NTC.QoS.Application.Features.Reports.WirelessBroadband.DTOs;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Report;
using NTC.QoS.Infrastructure.Persistence;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;

namespace NTC.QoS.Application.Features.Reports.WirelessBroadband.Commands;

public class GetWirelessBroadbandReportHandler : IRequestHandler<GetWirelessBroadbandReportQuery, ApiResponse<WirelessBroadbandReportResult>>
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<GetWirelessBroadbandReportHandler> _logger;

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
            // 1. Get Municipality
            var municipality = await _context.Municipalities
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == request.MunicipalityId, cancellationToken);

            if (municipality == null)
            {
                return new ApiResponse<WirelessBroadbandReportResult>("Municipality not found.");
            }

            // 2. Get Barangays
            var barangays = await _context.Barangays
                .AsNoTracking()
                .Where(b => b.MunicipalityId == request.MunicipalityId)
                .OrderBy(b => b.Name)
                .ToListAsync(cancellationToken);

            // 3. Prepare Data
            // Optimization: Fetch all data for the municipality in bulk instead of per-barangay queries
            
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

            // Fetch all Cellulars within Municipality Boundaries
            // Note: We project Location to client to perform Barangay containment check in memory
            var allCellulars = new List<CellularLocationInfo>();
            
            if (municipality.Boundaries != null)
            {
                allCellulars = await _context.Cellulars
                    .AsNoTracking()
                    .Where(c => c.CellSector != null
                             && c.CellSector.Cell != null
                             && c.CellSector.Cell.Location != null
                             && municipality.Boundaries.Contains(c.CellSector.Cell.Location))
                    .Select(c => new CellularLocationInfo
                    {
                        ProviderName = c.ServiceProvider != null && c.ServiceProvider.Provider != null ? c.ServiceProvider.Provider.Name : "",
                        GenerationName = c.Technology != null && c.Technology.Generation != null ? c.Technology.Generation.Name : "",
                        Location = c.CellSector!.Cell!.Location
                    })
                    .ToListAsync(cancellationToken);
            }

            var reportData = new List<BarangayWirelessEntryDto>();

            foreach (var barangay in barangays)
            {
                var entry = new BarangayWirelessEntryDto
                {
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

                entry.Globe = MapProviderStats(cellulars.Where(c => c.ProviderName.Contains("Globe", StringComparison.OrdinalIgnoreCase)));
                entry.Smart = MapProviderStats(cellulars.Where(c => c.ProviderName.Contains("Smart", StringComparison.OrdinalIgnoreCase)));
                entry.Dito = MapProviderStats(cellulars.Where(c => c.ProviderName.Contains("Dito", StringComparison.OrdinalIgnoreCase)));

                reportData.Add(entry);
            }

            // 4. Generate Excel
            ExcelPackage.License.SetNonCommercialPersonal("NTC QoS");
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Wireless Broadband");

            // Collect all unique provider/generation pairs for the whole municipality to determine columns
            var allMunicipalityData = new List<CellularInfo>();
            if (municipality.Boundaries != null)
            {
                allMunicipalityData.AddRange(allCellulars.Select(c => new CellularInfo { ProviderName = c.ProviderName, GenerationName = c.GenerationName }));
            }
            allMunicipalityData.AddRange(allValidations.Select(v => new CellularInfo { ProviderName = v.ProviderName, GenerationName = v.GenerationName }));

            var globeGenerations = GetPresentGenerations(allMunicipalityData, "Globe");
            var smartGenerations = GetPresentGenerations(allMunicipalityData, "Smart");
            var ditoGenerations = GetPresentGenerations(allMunicipalityData, "Dito");

            // --- Headers ---
            int col = 3; // Start at C

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

            DrawProviderSection("Globe", "GLOBE COMMUNICATION, INC.", globeGenerations, Color.CornflowerBlue);
            DrawProviderSection("Smart", "SMART COMMUNICATIONS, INC.", smartGenerations, Color.LightGreen);
            DrawProviderSection("Dito", "DITO TELECOMMUNITY, INC.", ditoGenerations, Color.IndianRed);

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
            // Merge Municipality column for all barangays
            if (reportData.Count > 0)
            {
                worksheet.Cells[row, 1, row + reportData.Count - 1, 1].Merge = true;
                worksheet.Cells[row, 1].Value = municipality.Name;
                worksheet.Cells[row, 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                worksheet.Cells[row, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheet.Cells[row, 1].Style.TextRotation = 90;
                worksheet.Cells[row, 1].Style.Font.Bold = true;
                worksheet.Cells[row, 1].Style.Font.Size = 14;
            }

            foreach (var item in reportData)
            {
                worksheet.Cells[row, 2].Value = item.BarangayName;
                int currentCol = 3;

                // Globe
                foreach (var gen in globeGenerations)
                {
                    if (HasGeneration(item.Globe, gen)) worksheet.Cells[row, currentCol].Value = "✓";
                    currentCol++;
                }
                worksheet.Cells[row, currentCol].Value = item.Globe.TechUsed;
                currentCol += 3; // Skip Tech + Speedtest

                // Smart
                foreach (var gen in smartGenerations)
                {
                    if (HasGeneration(item.Smart, gen)) worksheet.Cells[row, currentCol].Value = "✓";
                    currentCol++;
                }
                worksheet.Cells[row, currentCol].Value = item.Smart.TechUsed;
                currentCol += 3;

                // Dito
                foreach (var gen in ditoGenerations)
                {
                    if (HasGeneration(item.Dito, gen)) worksheet.Cells[row, currentCol].Value = "✓";
                    currentCol++;
                }
                worksheet.Cells[row, currentCol].Value = item.Dito.TechUsed;
                currentCol += 3;

                row++;
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

            var result = new WirelessBroadbandReportResult
            {
                FileContent = package.GetAsByteArray(),
                FileName = $"{municipality.Name}_WirelessBroadbandReport.xlsx"
            };

            return new ApiResponse<WirelessBroadbandReportResult>(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating wireless broadband report for municipality {MunicipalityId}", request.MunicipalityId);
            return new ApiResponse<WirelessBroadbandReportResult>("An error occurred while generating the report.");
        }
    }

    private ProviderWirelessStatsDto MapProviderStats(IEnumerable<CellularInfo> data)
    {
        var stats = new ProviderWirelessStatsDto();
        var generations = data.Select(d => d.GenerationName).ToHashSet(StringComparer.OrdinalIgnoreCase);

        stats.Has2G = generations.Contains("2G");
        stats.Has3G = generations.Contains("3G");
        stats.Has4G = generations.Contains("4G") || generations.Contains("LTE");
        stats.Has5G = generations.Contains("5G");

        // Determine "Tech Used" - simplistic logic: highest generation
        if (stats.Has5G) stats.TechUsed = "5G";
        else if (stats.Has4G) stats.TechUsed = "4G";
        else if (stats.Has3G) stats.TechUsed = "3G";
        else if (stats.Has2G) stats.TechUsed = "2G";
        else stats.TechUsed = "-";
        
        // User requested to leave Tech Used blank
        stats.TechUsed = "";

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
}
