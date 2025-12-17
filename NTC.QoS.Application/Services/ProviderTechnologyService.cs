using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Models.DTOs.Analytics;
using NTC.QoS.Infrastructure.Persistence;

namespace NTC.QoS.Application.Services;

/// <summary>
/// Service for calculating per-provider technology breakdown.
/// This is specific to wireless broadband reports and not part of the general connectivity assessment.
/// </summary>
public interface IProviderTechnologyService
{
    /// <summary>
    /// Calculate technology breakdown for each provider within the specified boundary.
    /// Returns a dictionary mapping provider names to their technology breakdown.
    /// </summary>
    Task<Dictionary<string, TechnologyBandBreakdownDto>> CalculateProviderTechnologyBreakdownAsync(
        Geometry? boundary,
        Guid? barangayId,
        CancellationToken cancellationToken = default);
}

public class ProviderTechnologyService : IProviderTechnologyService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ProviderTechnologyService> _logger;

    public ProviderTechnologyService(
        ApplicationDbContext context,
        ILogger<ProviderTechnologyService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Calculate technology breakdown for each provider within the specified boundary.
    /// Combines data from Cellulars (spatial), WirelessValidations (barangay-specific), and Speedtests.
    /// </summary>
    public async Task<Dictionary<string, TechnologyBandBreakdownDto>> CalculateProviderTechnologyBreakdownAsync(
        Geometry? boundary,
        Guid? barangayId,
        CancellationToken cancellationToken = default)
    {
        var providerTechMap = new Dictionary<string, Dictionary<string, HashSet<string>>>();

        // 1. Get data from Cellulars (spatial query)
        if (boundary != null)
        {
            var cellularData = await _context.Cellulars
                .AsNoTracking()
                .Where(c => c.CellSector != null
                         && c.CellSector.Cell != null
                         && c.CellSector.Cell.Location != null
                         && boundary.Contains(c.CellSector.Cell.Location)
                         && c.ServiceProvider != null
                         && c.ServiceProvider.Provider != null
                         && c.Technology != null
                         && c.Technology.Generation != null)
                .Include(c => c.Technology)
                    .ThenInclude(t => t!.TechnologyBands)
                    .ThenInclude(tb => tb.Band)
                .Select(c => new
                {
                    ProviderName = c.ServiceProvider!.Provider!.Name,
                    GenerationName = c.Technology!.Generation!.Name,
                    TechnologyStandard = c.Technology.Standard,
                    Bands = c.Technology.TechnologyBands.Select(tb => tb.Band.Name).ToList()
                })
                .ToListAsync(cancellationToken);

            foreach (var item in cellularData)
            {
                foreach (var band in item.Bands)
                {
                    AddToProviderTechMap(providerTechMap, item.ProviderName, item.GenerationName, item.TechnologyStandard, band);
                }
                // Also add without band if no bands specified
                if (!item.Bands.Any())
                {
                    AddToProviderTechMap(providerTechMap, item.ProviderName, item.GenerationName, item.TechnologyStandard, "");
                }
            }
        }

        // 2. Get data from WirelessValidations (barangay-specific)
        if (barangayId.HasValue)
        {
            var validationData = await _context.WirelessValidations
                .AsNoTracking()
                .Where(wv => wv.BarangayId == barangayId.Value
                          && wv.ServiceProvider != null
                          && wv.ServiceProvider.Provider != null
                          && wv.Technology != null
                          && wv.Technology.Generation != null)
                .Include(wv => wv.Technology)
                    .ThenInclude(t => t!.TechnologyBands)
                    .ThenInclude(tb => tb.Band)
                .Select(wv => new
                {
                    ProviderName = wv.ServiceProvider!.Provider!.Name,
                    GenerationName = wv.Technology!.Generation!.Name,
                    TechnologyStandard = wv.Technology.Standard,
                    Bands = wv.Technology.TechnologyBands.Select(tb => tb.Band.Name).ToList()
                })
                .ToListAsync(cancellationToken);

            foreach (var item in validationData)
            {
                foreach (var band in item.Bands)
                {
                    AddToProviderTechMap(providerTechMap, item.ProviderName, item.GenerationName, item.TechnologyStandard, band);
                }
                // Also add without band if no bands specified
                if (!item.Bands.Any())
                {
                    AddToProviderTechMap(providerTechMap, item.ProviderName, item.GenerationName, item.TechnologyStandard, "");
                }
            }
        }

        // 3. Get data from Speedtests (spatial query, wireless only)
        if (boundary != null)
        {
            var speedtestData = await _context.Speedtests
                .AsNoTracking()
                .Where(s => s.Location != null
                         && boundary.Contains(s.Location)
                         && s.ServiceProvider.Service.Type == ServiceTypeEnum.Wireless
                         && s.Generation != null)
                .Select(s => new
                {
                    ProviderName = s.ServiceProvider.Provider.Name,
                    GenerationName = s.Generation!.Name
                })
                .ToListAsync(cancellationToken);

            foreach (var item in speedtestData)
            {
                // Speedtests don't have technology/band details, only generation
                AddToProviderTechMap(providerTechMap, item.ProviderName, item.GenerationName, "", "");
            }
        }

        // Convert to TechnologyBandBreakdownDto per provider
        var result = new Dictionary<string, TechnologyBandBreakdownDto>();

        foreach (var (providerName, techData) in providerTechMap)
        {
            var breakdown = new TechnologyBandBreakdownDto();

            // Build technology-band mapping
            foreach (var (tech, bands) in techData)
            {
                if (!string.IsNullOrEmpty(tech))
                {
                    breakdown[tech] = bands.Where(b => !string.IsNullOrEmpty(b)).OrderBy(b => b).ToList();
                }
            }

            // Calculate generation counts
            var generationCounts = new Dictionary<string, int>();
            foreach (var (tech, _) in techData)
            {
                if (string.IsNullOrEmpty(tech)) continue;

                var normalizedGen = NormalizeGenerationName(tech);
                if (!string.IsNullOrEmpty(normalizedGen))
                {
                    generationCounts[normalizedGen] = generationCounts.GetValueOrDefault(normalizedGen) + 1;
                }
            }

            breakdown.Count2G = generationCounts.GetValueOrDefault("2G");
            breakdown.Count3G = generationCounts.GetValueOrDefault("3G");
            breakdown.Count4G = generationCounts.GetValueOrDefault("4G");
            breakdown.Count5G = generationCounts.GetValueOrDefault("5G");

            // Calculate summary properties
            breakdown.TotalTechnologyCount = breakdown.Count2G + breakdown.Count3G + breakdown.Count4G + breakdown.Count5G;
            breakdown.AvailableGenerations = generationCounts.Keys.OrderBy(g => g).ToList();

            if (breakdown.TotalTechnologyCount > 0)
            {
                breakdown.ModernCoverageRatio = (double)(breakdown.Count4G + breakdown.Count5G) / breakdown.TotalTechnologyCount;
                breakdown.HasModernCoverage = breakdown.ModernCoverageRatio > 0;

                // Determine dominant generation
                var maxCount = generationCounts.Max(kvp => kvp.Value);
                breakdown.DominantGeneration = generationCounts.FirstOrDefault(kvp => kvp.Value == maxCount).Key;
            }

            result[providerName] = breakdown;
        }

        return result;
    }

    /// <summary>
    /// Helper method to add technology data to the provider tech map
    /// </summary>
    private static void AddToProviderTechMap(
        Dictionary<string, Dictionary<string, HashSet<string>>> providerTechMap,
        string providerName,
        string generationName,
        string technologyName,
        string bandName)
    {
        if (string.IsNullOrEmpty(providerName)) return;

        if (!providerTechMap.ContainsKey(providerName))
        {
            providerTechMap[providerName] = new Dictionary<string, HashSet<string>>();
        }

        // Use technology name if available, otherwise use generation as key
        var techKey = !string.IsNullOrEmpty(technologyName) ? technologyName : generationName;

        if (!providerTechMap[providerName].ContainsKey(techKey))
        {
            providerTechMap[providerName][techKey] = new HashSet<string>();
        }

        if (!string.IsNullOrEmpty(bandName))
        {
            providerTechMap[providerName][techKey].Add(bandName);
        }
    }

    /// <summary>
    /// Normalize generation names to standard format (2G, 3G, 4G, 5G)
    /// Aligned with connectivity assessment logic
    /// </summary>
    private static string NormalizeGenerationName(string generationName)
    {
        if (string.IsNullOrEmpty(generationName)) return "";

        var gen = generationName.ToUpperInvariant();

        // 5G patterns
        if (gen.Contains("5G") || gen.Contains("NR")) return "5G";

        // 4G/LTE patterns
        if (gen.Contains("4G") || gen.Contains("LTE")) return "4G";

        // 3G patterns
        if (gen.Contains("3G") || gen.Contains("UMTS") || gen.Contains("HSPA") || gen.Contains("WCDMA"))
            return "3G";

        // 2G patterns
        if (gen.Contains("2G") || gen.Contains("GSM") || gen.Contains("EDGE") || gen.Contains("GPRS"))
            return "2G";

        return "";
    }
}
