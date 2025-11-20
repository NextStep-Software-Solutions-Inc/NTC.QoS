using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Infrastructure.Persistence;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;

namespace NTC.QoS.Application.Features.Reports.Queries.GetWirelessBroadbandReport;

public class GetWirelessBroadbandReportHandler : IRequestHandler<GetWirelessBroadbandReportQuery, ApiResponse<byte[]>>
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

    public async Task<ApiResponse<byte[]>> Handle(
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
                return new ApiResponse<byte[]>("Municipality not found.");
            }

            // 2. Get Barangays
            var barangays = await _context.Barangays
                .AsNoTracking()
                .Where(b => b.MunicipalityId == request.MunicipalityId)
                .OrderBy(b => b.Name)
                .ToListAsync(cancellationToken);

            // 3. Prepare Data
            var reportData = new List<BarangayWirelessEntryDto>();

            foreach (var barangay in barangays)
            {
                var entry = new BarangayWirelessEntryDto
                {
                    BarangayName = barangay.Name
                };

                if (barangay.Boundaries != null)
                {
                    var cellulars = await _context.Cellulars
                        .AsNoTracking()
                        .Include(c => c.ServiceProvider)
                            .ThenInclude(sp => sp!.Provider)
                        .Include(c => c.Technology)
                            .ThenInclude(t => t!.Generation)
                        .Include(c => c.CellSector)
                            .ThenInclude(cs => cs!.Cell)
                        .Where(c => c.CellSector != null
                                 && c.CellSector.Cell != null
                                 && c.CellSector.Cell.Location != null
                                 && barangay.Boundaries.Contains(c.CellSector.Cell.Location))
                        .Select(c => new CellularInfo
                        {
                            ProviderName = c.ServiceProvider != null && c.ServiceProvider.Provider != null ? c.ServiceProvider.Provider.Name : "",
                            GenerationName = c.Technology != null && c.Technology.Generation != null ? c.Technology.Generation.Name : ""
                        })
                        .Distinct()
                        .ToListAsync(cancellationToken);

                    entry.Globe = MapProviderStats(cellulars.Where(c => c.ProviderName.Contains("Globe", StringComparison.OrdinalIgnoreCase)));
                    entry.Smart = MapProviderStats(cellulars.Where(c => c.ProviderName.Contains("Smart", StringComparison.OrdinalIgnoreCase)));
                    entry.Dito = MapProviderStats(cellulars.Where(c => c.ProviderName.Contains("Dito", StringComparison.OrdinalIgnoreCase)));
                }
                reportData.Add(entry);
            }

            // 4. Generate Excel
            ExcelPackage.License.SetNonCommercialPersonal("NTC QoS");
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Wireless Broadband");

            // --- Headers ---
            // Row 1: Title
            worksheet.Cells["A1:T1"].Merge = true;
            worksheet.Cells["A1"].Value = "WIRELESS BROADBAND";
            worksheet.Cells["A1"].Style.Font.Bold = true;
            worksheet.Cells["A1"].Style.Font.Size = 14;
            worksheet.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            worksheet.Cells["A1"].Style.Fill.PatternType = ExcelFillStyle.Solid;
            worksheet.Cells["A1"].Style.Fill.BackgroundColor.SetColor(Color.Cyan);

            // Row 2: Provider Headers
            // Municipality & Barangay
            worksheet.Cells["A2:A4"].Merge = true;
            worksheet.Cells["A2"].Value = "Municipality";
            worksheet.Cells["B2:B4"].Merge = true;
            worksheet.Cells["B2"].Value = "Barangay";

            // Globe (C-H) - 6 cols
            worksheet.Cells["C2:H2"].Merge = true;
            worksheet.Cells["C2"].Value = "GLOBE COMMUNICATION, INC.";
            worksheet.Cells["C2"].Style.Fill.PatternType = ExcelFillStyle.Solid;
            worksheet.Cells["C2"].Style.Fill.BackgroundColor.SetColor(Color.CornflowerBlue);

            // Smart (I-O) - 7 cols
            worksheet.Cells["I2:O2"].Merge = true;
            worksheet.Cells["I2"].Value = "SMART COMMUNICATIONS, INC.";
            worksheet.Cells["I2"].Style.Fill.PatternType = ExcelFillStyle.Solid;
            worksheet.Cells["I2"].Style.Fill.BackgroundColor.SetColor(Color.LightGreen);

            // Dito (P-T) - 5 cols
            worksheet.Cells["P2:T2"].Merge = true;
            worksheet.Cells["P2"].Value = "DITO TELECOMMUNITY, INC.";
            worksheet.Cells["P2"].Style.Fill.PatternType = ExcelFillStyle.Solid;
            worksheet.Cells["P2"].Style.Fill.BackgroundColor.SetColor(Color.IndianRed);

            // Row 3: Sub-headers
            // Globe Row 3
            worksheet.Cells["C3"].Value = "2G";
            worksheet.Cells["D3"].Value = "4G";
            worksheet.Cells["E3"].Value = "5G";
            worksheet.Cells["F3"].Value = "Tech used";
            worksheet.Cells["G3:H3"].Merge = true;
            worksheet.Cells["G3"].Value = "Speedtest (x~) (Mbps)";

            // Smart Row 3
            worksheet.Cells["I3"].Value = "2G";
            worksheet.Cells["J3"].Value = "3G";
            worksheet.Cells["K3"].Value = "4G";
            worksheet.Cells["L3"].Value = "5G";
            worksheet.Cells["M3"].Value = "Tech used";
            worksheet.Cells["N3:O3"].Merge = true;
            worksheet.Cells["N3"].Value = "Speedtest (x~) (Mbps)";

            // Dito Row 3
            worksheet.Cells["P3"].Value = "4G";
            worksheet.Cells["Q3"].Value = "5G";
            worksheet.Cells["R3"].Value = "Tech used";
            worksheet.Cells["S3:T3"].Merge = true;
            worksheet.Cells["S3"].Value = "Speedtest (x~) (Mbps)";

            // Row 4: Sub-sub headers for Speedtest
            worksheet.Cells["G4"].Value = "Upload";
            worksheet.Cells["H4"].Value = "Download";
            worksheet.Cells["N4"].Value = "Upload";
            worksheet.Cells["O4"].Value = "Download";
            worksheet.Cells["S4"].Value = "Upload";
            worksheet.Cells["T4"].Value = "Download";

            // Merge the non-speedtest columns from Row 3 to Row 4
            worksheet.Cells["C3:C4"].Merge = true; // Globe 2G
            worksheet.Cells["D3:D4"].Merge = true; // Globe 4G
            worksheet.Cells["E3:E4"].Merge = true; // Globe 5G
            worksheet.Cells["F3:F4"].Merge = true; // Globe Tech

            worksheet.Cells["I3:I4"].Merge = true; // Smart 2G
            worksheet.Cells["J3:J4"].Merge = true; // Smart 3G
            worksheet.Cells["K3:K4"].Merge = true; // Smart 4G
            worksheet.Cells["L3:L4"].Merge = true; // Smart 5G
            worksheet.Cells["M3:M4"].Merge = true; // Smart Tech

            worksheet.Cells["P3:P4"].Merge = true; // Dito 4G
            worksheet.Cells["Q3:Q4"].Merge = true; // Dito 5G
            worksheet.Cells["R3:R4"].Merge = true; // Dito Tech

            // Center align headers
            worksheet.Cells["A2:T4"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            worksheet.Cells["A2:T4"].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            worksheet.Cells["A2:T4"].Style.Font.Bold = true;
            worksheet.Cells["A2:T4"].Style.Border.Top.Style = ExcelBorderStyle.Thin;
            worksheet.Cells["A2:T4"].Style.Border.Left.Style = ExcelBorderStyle.Thin;
            worksheet.Cells["A2:T4"].Style.Border.Right.Style = ExcelBorderStyle.Thin;
            worksheet.Cells["A2:T4"].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

            // --- Data ---
            int row = 5;
            // Merge Municipality column for all barangays
            if (reportData.Count > 0)
            {
                worksheet.Cells[row, 1, row + reportData.Count - 1, 1].Merge = true;
                worksheet.Cells[row, 1].Value = municipality.Name;
                worksheet.Cells[row, 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                worksheet.Cells[row, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                // Rotate text 90 degrees like in the image? The image has "Alubijid" rotated.
                worksheet.Cells[row, 1].Style.TextRotation = 90;
                worksheet.Cells[row, 1].Style.Font.Bold = true;
                worksheet.Cells[row, 1].Style.Font.Size = 14;
            }

            foreach (var item in reportData)
            {
                worksheet.Cells[row, 2].Value = item.BarangayName;

                // Globe
                if (item.Globe.Has2G) worksheet.Cells[row, 3].Value = "?";
                if (item.Globe.Has4G) worksheet.Cells[row, 4].Value = "?";
                if (item.Globe.Has5G) worksheet.Cells[row, 5].Value = "?";
                worksheet.Cells[row, 6].Value = item.Globe.TechUsed;
                // Speedtest blank

                // Smart
                if (item.Smart.Has2G) worksheet.Cells[row, 9].Value = "?";
                if (item.Smart.Has3G) worksheet.Cells[row, 10].Value = "?";
                if (item.Smart.Has4G) worksheet.Cells[row, 11].Value = "?";
                if (item.Smart.Has5G) worksheet.Cells[row, 12].Value = "?";
                worksheet.Cells[row, 13].Value = item.Smart.TechUsed;
                // Speedtest blank

                // Dito
                if (item.Dito.Has4G) worksheet.Cells[row, 16].Value = "?";
                if (item.Dito.Has5G) worksheet.Cells[row, 17].Value = "?";
                worksheet.Cells[row, 18].Value = item.Dito.TechUsed;
                // Speedtest blank

                row++;
            }

            // Borders for data
            var dataRange = worksheet.Cells[5, 1, row - 1, 20];
            dataRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            dataRange.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            dataRange.Style.Border.Right.Style = ExcelBorderStyle.Thin;
            dataRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            dataRange.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            worksheet.Cells.AutoFitColumns();

            return new ApiResponse<byte[]>(package.GetAsByteArray());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating wireless broadband report for municipality {MunicipalityId}", request.MunicipalityId);
            return new ApiResponse<byte[]>("An error occurred while generating the report.");
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

        return stats;
    }

    private class CellularInfo
    {
        public string ProviderName { get; set; } = string.Empty;
        public string GenerationName { get; set; } = string.Empty;
    }
}
