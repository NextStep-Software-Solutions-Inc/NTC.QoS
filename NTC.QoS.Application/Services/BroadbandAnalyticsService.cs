using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.Analytics;
using NTC.QoS.Infrastructure.Helper;
using System.Collections.Generic;

namespace NTC.QoS.Application.Services;

/// <summary>
/// Service for calculating broadband analytics metrics.
/// Extracts shared logic from GetPointBroadbandInfoHandler and GetAreaBroadbandInfoHandler.
/// </summary>
public class BroadbandAnalyticsService
{
    private readonly IMunicipalityRepository _municipalityRepository;
    private readonly ILogger<BroadbandAnalyticsService> _logger;

    // NOTE: Reflection caches removed to simplify code and reduce complexity.

    public BroadbandAnalyticsService(
        IMunicipalityRepository municipalityRepository,
        ILogger<BroadbandAnalyticsService> logger)
    {
        _municipalityRepository = municipalityRepository;
        _logger = logger;
    }

    // Reflection helper removed; projections now use strongly-typed DTOs.

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
            if (speedtestData != null && speedtestData.Any())
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
            if (serviceType == ServiceTypeEnum.Wireless && cellularBandwidthData != null && cellularBandwidthData.Any())
            {
                var providerBandwidths = cellularBandwidthData
                    .Where(c => c.ServiceProviderId == providerId && c.ServiceType == ServiceTypeEnum.Wireless)
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
    public SignalStrengthDto? CalculateSignalStrength<T>(List<T> cellularCoverageData) where T : class
    {
        if (!cellularCoverageData.Any())
        {
            return null;
        }

        try
        {
            var strengthProperty = typeof(T).GetProperty("Strength");
            if (strengthProperty == null)
            {
                return null;
            }

            var strengths = cellularCoverageData
                .Select(c => strengthProperty.GetValue(c))
                .OfType<decimal>()
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
        if (!municipalities.Any())
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
    /// Uses both interface constraints for type safety and fallback reflection for anonymous types.
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
        var municipalityGroups = new Dictionary<(Guid? Id, string Name), (int CellCount, int NAPCount, bool IsCovered)>();

        // Initialize all covered municipalities with zero assets
        foreach (var coveredMunicipality in coveredMunicipalities)
        {
            var key = (coveredMunicipality.Id, coveredMunicipality.Name);
            municipalityGroups[key] = (0, 0, true);
        }

        // Count cells by municipality
        foreach (var location in cellLocations)
        {
            if (location?.IsValid == true && locationMunicipalityMap.TryGetValue(location, out var municipalityInfo))
            {
                var key = (municipalityInfo.Id, municipalityInfo.Name);
                if (municipalityGroups.TryGetValue(key, out var value))
                {
                    municipalityGroups[key] = (value.CellCount + 1, value.NAPCount, value.IsCovered);
                }
                else
                {
                    // Asset found in municipality not covered by query geometry (edge case)
                    municipalityGroups[key] = (1, 0, false);
                }
            }
        }

        // Count NAPs by municipality
        foreach (var location in napLocations)
        {
            if (location?.IsValid == true && locationMunicipalityMap.TryGetValue(location, out var municipalityInfo))
            {
                var key = (municipalityInfo.Id, municipalityInfo.Name);
                if (municipalityGroups.TryGetValue(key, out var value))
                {
                    municipalityGroups[key] = (value.CellCount, value.NAPCount + 1, value.IsCovered);
                }
                else
                {
                    // Asset found in municipality not covered by query geometry (edge case)
                    municipalityGroups[key] = (0, 1, false);
                }
            }
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

    /// <summary>
    /// Type-safe method to extract Location property from objects.
    /// Handles both interface implementations and anonymous types with optimized caching.
    /// </summary>
    // GetLocationFromItem removed: handlers now project concrete DTOs including `Location`.
}
