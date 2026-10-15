using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.Analytics;
using NTC.QoS.Infrastructure.Helper;

namespace NTC.QoS.Application.Services;

/// <summary>
/// Service for calculating broadband analytics metrics.
/// Extracts shared logic from GetPointBroadbandInfoHandler and GetAreaBroadbandInfoHandler.
/// </summary>
public class BroadbandAnalyticsService
{
    private readonly IMunicipalityRepository _municipalityRepository;
    private readonly ILogger<BroadbandAnalyticsService> _logger;
    public BroadbandAnalyticsService(
        IMunicipalityRepository municipalityRepository,
        ILogger<BroadbandAnalyticsService> logger)
    {
        _municipalityRepository = municipalityRepository;
        _logger = logger;
    }

    /// <summary>
    /// Calculates maximum speed for a service provider based on cellular bandwidth data, speedtest data, and service type.
    /// Prioritizes actual speedtest measurements over theoretical bandwidth.
    /// </summary>
    public decimal? CalculateMaxSpeedForProvider(Guid providerId, ServiceTypeEnum serviceType, List<CellularBandwidthProjectionDto> cellularBandwidthData, List<SpeedtestProjectionDto> speedtestData)
    {
        try
        {
            decimal? maxSpeed = null;

            // First, check for actual speedtest measurements for this provider
            if (speedtestData?.Any() == true)
            {
                var providerSpeeds = speedtestData
                    .Where(s => s.ServiceProviderId == providerId && s.ServiceType == serviceType)
                    .Select(s => s.DownloadSpeed)
                    .ToList();

                if (providerSpeeds.Any())
                {
                    maxSpeed = providerSpeeds.Max();
                }
            }

            // For wireless/cellular, also check bandwidth data and use the higher value
            if ((serviceType == ServiceTypeEnum.Wireless || serviceType == ServiceTypeEnum.Cellular) && cellularBandwidthData?.Any() == true)
            {
                var providerBandwidths = cellularBandwidthData
                    .Where(c => c.ServiceProviderId == providerId && (c.ServiceType == ServiceTypeEnum.Wireless || c.ServiceType == ServiceTypeEnum.Cellular))
                    .Select(c => c.Bandwidth)
                    .ToList();

                if (providerBandwidths.Any())
                {
                    var maxBandwidth = providerBandwidths.Max();
                    maxSpeed = maxSpeed.HasValue ? Math.Max(maxSpeed.Value, maxBandwidth) : maxBandwidth;
                }
            }

            return maxSpeed;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to calculate max speed for provider {ProviderId} with service type {ServiceType}", providerId, serviceType);
            return null;
        }
    }

    /// <summary>
    /// Calculates signal strength metrics from cellular coverage data.
    /// </summary>
    public SignalStrengthDto? CalculateSignalStrength(List<CellularCoverageProjectionDto> cellularCoverageData)
    {
        if (cellularCoverageData?.Any() != true)
        {
            return null;
        }

        try
        {
            var strengths = cellularCoverageData
                .Select(c => c.SignalStrength)
                .ToList();

            if (!strengths.Any())
            {
                return null;
            }

            var avgStrength = strengths.Average();
            var maxStrength = strengths.Max();
            var minStrength = strengths.Min();

            // Determine quality based on average strength (in dBm)
            // Signal strength is typically negative, so higher (less negative) is better
            var quality = avgStrength switch
            {
                >= -70 => "Excellent",
                >= -85 => "Good",
                >= -100 => "Fair",
                >= -110 => "Weak",
                _ => "Poor"
            };

            return new SignalStrengthDto
            {
                AverageStrengthDbm = Math.Round(avgStrength, 2),
                MaxStrengthDbm = maxStrength,
                MinStrengthDbm = minStrength,
                Quality = quality,
                MeasurementCount = strengths.Count
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to calculate signal strength from {Count} coverage data points", cellularCoverageData.Count);
            return null;
        }
    }

    /// <summary>
    /// Extracts location details from municipality information.
    /// </summary>
    public LocationDetailsDto? GetLocationDetails(List<MunicipalityAreaInfoDto> municipalities)
    {
        if (municipalities?.Any() != true)
        {
            return null;
        }

        try
        {
            // Get the primary municipality (one with most assets or first covered)
            var primaryMunicipality = municipalities
                .Where(m => m.HasAssets)
                .OrderByDescending(m => m.TotalAssets)
                .ThenBy(m => m.Name)
                .FirstOrDefault() ?? municipalities.FirstOrDefault();

            if (primaryMunicipality == null)
            {
                return null;
            }

            // Parse municipality name - typically might include province info
            // This is a basic implementation; you may need to enhance with actual barangay/province queries
            return new LocationDetailsDto
            {
                Municipality = primaryMunicipality.Name,
                // Note: Street, Barangay, Province, and Region would require additional queries
                // or reverse geocoding services to populate accurately
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract location details from {Count} municipalities", municipalities.Count);
            return null;
        }
    }

    /// <summary>
    /// Calculates municipality information for items that have location data.
    /// Accepts strongly-typed <see cref="Point"/> collections for both cell and NAP locations.
    /// </summary>
    public async Task<List<MunicipalityAreaInfoDto>> CalculateMunicipalityInfo(
        Geometry queryGeometry,
        List<Point> cellLocations,
        List<Point> napLocations,
        CancellationToken cancellationToken)
    {
        var allLocations = cellLocations.Concat(napLocations).ToList();

        // Find all municipalities covered by the query area (regardless of assets)
        var coveredMunicipalities = await GeoQueryHelper.FindMunicipalitiesWithinGeometryAsync(
            queryGeometry, _municipalityRepository, cancellationToken);

        _logger.LogInformation("Found {CoveredCount} municipalities within the query area", coveredMunicipalities.Count);

        // Find municipalities for asset locations (if any)
        var locationMunicipalityMap = new Dictionary<Point, (Guid? Id, string Name)>();
        if (allLocations.Count > 0)
        {
            locationMunicipalityMap = await GeoQueryHelper.FindMunicipalitiesByLocationsAsync(
                allLocations, _municipalityRepository, cancellationToken);
        }

        // Group by municipality and count assets
        var municipalityGroups = coveredMunicipalities
            .Select(m => new KeyValuePair<(Guid? Id, string Name), (int CellCount, int NAPCount, bool IsCovered)>(
                (m.Id, m.Name),
                (0, 0, true)))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        // Count cells by municipality
        var cellUpdates = cellLocations
            .Where(location => location?.IsValid == true && locationMunicipalityMap.ContainsKey(location))
            .Select(location =>
            {
                var municipalityInfo = locationMunicipalityMap[location];
                return (municipalityInfo.Id, municipalityInfo.Name);
            })
            .GroupBy(key => key)
            .Select(g => new { Key = g.Key, Count = g.Count() });

        foreach (var update in cellUpdates)
        {
            var key = update.Key;
            municipalityGroups[key] = municipalityGroups.TryGetValue(key, out var value)
                ? (value.CellCount + update.Count, value.NAPCount, value.IsCovered)
                : (update.Count, 0, false);
        }

        // Count NAPs by municipality
        var napUpdates = napLocations
            .Where(location => location?.IsValid == true && locationMunicipalityMap.ContainsKey(location))
            .Select(location =>
            {
                var municipalityInfo = locationMunicipalityMap[location];
                return (municipalityInfo.Id, municipalityInfo.Name);
            })
            .GroupBy(key => key)
            .Select(g => new { Key = g.Key, Count = g.Count() });

        foreach (var update in napUpdates)
        {
            var key = update.Key;
            municipalityGroups[key] = municipalityGroups.TryGetValue(key, out var value)
                ? (value.CellCount, value.NAPCount + update.Count, value.IsCovered)
                : (0, update.Count, false);
        }

        // Calculate total assets for percentage calculation
        var totalAssets = cellLocations.Count + napLocations.Count;

        var result = municipalityGroups
            .Select(kvp => new MunicipalityAreaInfoDto
            {
                Id = kvp.Key.Id,
                Name = kvp.Key.Name,
                CellCount = kvp.Value.CellCount,
                NAPCount = kvp.Value.NAPCount,
                IsCoveredByQuery = kvp.Value.IsCovered,
                AssetPercentage = totalAssets > 0
                    ? Math.Round(((double)(kvp.Value.CellCount + kvp.Value.NAPCount) / totalAssets) * 100, 2)
                    : 0
            })
            .OrderByDescending(m => m.HasAssets)        // Assets first
            .ThenByDescending(m => m.TotalAssets)       // Then by asset count
            .ThenByDescending(m => m.IsCoveredByQuery)  // Then covered ones
            .ThenBy(m => m.Name)                        // Finally by name
            .ToList();

        _logger.LogInformation("Municipality calculation completed. Found {MunicipalityCount} total municipalities ({WithAssets} with assets, {CoveredOnly} covered without assets)",
            result.Count,
            result.Count(m => m.HasAssets),
            result.Count(m => m.IsCoveredByQuery && !m.HasAssets));

        return result;
    }
}
