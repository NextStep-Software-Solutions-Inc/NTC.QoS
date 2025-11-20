using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Infrastructure.Persistence;

namespace NTC.QoS.Application.Features.Reports.Queries.GetWirelessBroadbandReport;

public class GetWirelessBroadbandReportHandler : IRequestHandler<GetWirelessBroadbandReportQuery, ApiResponse<WirelessBroadbandReportDto>>
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

    public async Task<ApiResponse<WirelessBroadbandReportDto>> Handle(
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
                return new ApiResponse<WirelessBroadbandReportDto>("Municipality not found.");
            }

            // 2. Get Barangays
            var barangays = await _context.Barangays
                .AsNoTracking()
                .Where(b => b.MunicipalityId == request.MunicipalityId)
                .OrderBy(b => b.Name)
                .ToListAsync(cancellationToken);

            var report = new WirelessBroadbandReportDto
            {
                MunicipalityName = municipality.Name
            };

            // 3. Process each Barangay
            foreach (var barangay in barangays)
            {
                var entry = new BarangayWirelessEntryDto
                {
                    BarangayName = barangay.Name
                };

                if (barangay.Boundaries != null)
                {
                    // Query Cellulars in this Barangay
                    // We need to join Cellular -> ServiceProvider -> Provider and Cellular -> Technology -> Generation
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

                    // Process Globe
                    var globeData = cellulars.Where(c => c.ProviderName.Contains("Globe", StringComparison.OrdinalIgnoreCase)).ToList();
                    entry.Globe = MapProviderStats(globeData);

                    // Process Smart
                    var smartData = cellulars.Where(c => c.ProviderName.Contains("Smart", StringComparison.OrdinalIgnoreCase)).ToList();
                    entry.Smart = MapProviderStats(smartData);

                    // Process Dito
                    var ditoData = cellulars.Where(c => c.ProviderName.Contains("Dito", StringComparison.OrdinalIgnoreCase)).ToList();
                    entry.Dito = MapProviderStats(ditoData);
                }

                report.Barangays.Add(entry);
            }

            return new ApiResponse<WirelessBroadbandReportDto>(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating wireless broadband report for municipality {MunicipalityId}", request.MunicipalityId);
            return new ApiResponse<WirelessBroadbandReportDto>("An error occurred while generating the report.");
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

        // Speedtest data is left blank as requested
        stats.UploadSpeedMbps = null;
        stats.DownloadSpeedMbps = null;

        return stats;
    }

    private class CellularInfo
    {
        public string ProviderName { get; set; } = string.Empty;
        public string GenerationName { get; set; } = string.Empty;
    }
}
