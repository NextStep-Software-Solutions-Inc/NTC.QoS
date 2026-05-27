using NTC.QoS.Domain.Enums;
using NetTopologySuite.Geometries;

namespace NTC.QoS.Infrastructure.Helper
{
    public static class CacheKeyGenerator
    {
        /// <summary>
        /// Generates a standardized cache key for Service Providers.
        /// Format: "providername-servicetypeenum.wireless" (all lowercase).
        /// </summary>
        /// <param name="providerName">The name of the service provider (e.g., "ATT").</param>
        /// <param name="serviceType">The service type (expected to be ServiceTypeEnum.Wireless).</param>
        /// <returns>The standardized lowercase cache key.</returns>
        public static string GenerateServiceProviderCacheKey(string providerName, ServiceTypeEnum serviceType)
        {
            // This directly reflects your existing logic for this key.
            return $"{providerName}-{serviceType}".ToLowerInvariant();
        }

        /// <summary>
        /// Generates a standardized cache key for Technology.
        /// Format: Lowercase technology standard.
        /// This is the preferred method for caching individual Technology entities.
        /// </summary>
        /// <param name="technologyStandard">The technology standard (e.g., "LTE", "GSM", "WCDMA").</param>
        /// <returns>A standardized lowercase cache key for the technology.</returns>
        public static string GenerateTechnologyCacheKey(string technologyStandard)
        {
            return technologyStandard.ToLowerInvariant();
        }

        /// <summary>
        /// Generates a standardized cache key for Band.
        /// Format: Lowercase band name.
        /// This is the preferred method for caching individual Band entities.
        /// </summary>
        /// <param name="bandName">The band name (e.g., "Band 3", "Band 28").</param>
        /// <returns>A standardized lowercase cache key for the band.</returns>
        public static string GenerateBandCacheKey(string bandName)
        {
            return bandName.ToLowerInvariant();
        }

        /// <summary>
        /// Generates a standardized cache key for Cells based on ServiceProviderId, TowerType, and Location.
        /// Format: "Cell-ServiceProviderId_TowerType_Longitude_Latitude" (case-sensitive as per current usage).
        /// </summary>
        /// <param name="serviceProviderId">The GUID of the service provider.</param>
        /// <param name="towerType">The tower type (e.g., "Macro"). Null/empty values are normalized to "Unknown".</param>
        /// <param name="location">The NetTopologySuite Point object for the cell's location.</param>
        /// <returns>The standardized cache key for the cell.</returns>
        public static string GenerateCellCacheKey(Guid serviceProviderId, string towerType, Point location)
        {
            // Normalize TowerType to handle null and empty strings consistently
            var normalizedTowerType = string.IsNullOrWhiteSpace(towerType) ? "Unknown" : towerType;

            // This directly reflects your existing logic for this key.
            // Note: Your current code uses c.Location.Coordinate.X and c.Location.Coordinate.Y.
            // Ensure `location.Coordinate.X` and `location.Coordinate.Y` yield the same precision/format
            // as what's stored in the database for `c.Location.Coordinate.X` and `c.Location.Coordinate.Y`.
            return $"Cell-{serviceProviderId}_{normalizedTowerType}_{location.Coordinate.X}_{location.Coordinate.Y}";
        }

        /// <summary>
        /// Generates a standardized cache key for Generation technology only.
        /// Example: "4g", "5g", "lte"
        /// </summary>
        public static string GenerateGenerationCacheKey(string technologyStandard)
        {
            return technologyStandard.ToLowerInvariant();
        }

        /// <summary>
        /// Generates a standardized cache key for Networks based on ServiceProvider + Municipality + Technology + Band.
        /// Format: "Network-ServiceProviderId_MunicipalityName_TechnologyId_BandId"
        /// </summary>
        /// <param name="serviceProviderId">The GUID of the service provider.</param>
        /// <param name="municipalityName">The name of the municipality.</param>
        /// <param name="technologyId">The GUID of the technology.</param>
        /// <param name="bandId">The GUID of the band.</param>
        /// <returns>The standardized cache key for the network.</returns>
        public static string GenerateNetworkCacheKey(Guid serviceProviderId, string municipalityName, Guid technologyId, Guid bandId)
        {
            var normalizedMunicipalityName = municipalityName?.Trim().ToLowerInvariant() ?? string.Empty;
            return $"Network-{serviceProviderId}_{normalizedMunicipalityName}_{technologyId}_{bandId}";
        }

        /// <summary>
        /// Generates a simple cache key for Network based on name (for backward compatibility).
        /// Format: Lowercase network name.
        /// </summary>
        /// <param name="networkName">The name of the network.</param>
        /// <returns>A standardized lowercase cache key for the network.</returns>
        public static string GenerateSimpleNetworkCacheKey(string networkName)
        {
            return networkName.ToLowerInvariant();
        }

        public static string GenerateBarangayCacheKey(string barangayName, Guid municipalityId)
        {
            return $"{barangayName}_{municipalityId}".ToLowerInvariant();
        }

        /// <summary>
        /// Generates a standardized cache key for Municipality.
        /// Format: Lowercase municipality name.
        /// </summary>
        /// <param name="municipalityName">The municipality name (e.g., "Quezon City").</param>
        /// <returns>A standardized lowercase cache key for the municipality.</returns>
        public static string GenerateMunicipalityCacheKey(string municipalityName)
        {
            return municipalityName.ToLowerInvariant();
        }

        /// <summary>
        /// Generates a cache key for wireless validation records to check for duplicates.
        /// Format: "WV-ServiceProviderId_BarangayId_BandId_TechnologyId_NormalizedAddress"
        /// </summary>
        /// <param name="serviceProviderId">Service provider ID</param>
        /// <param name="barangayId">Barangay ID</param>
        /// <param name="bandId">Band ID</param>
        /// <param name="technologyId">Technology ID</param>
        /// <param name="address">Address string</param>
        /// <returns>A unique cache key for the wireless validation combination</returns>
        public static string GenerateWirelessValidationCacheKey(Guid serviceProviderId, Guid barangayId, Guid bandId, Guid technologyId, string address)
        {
            var normalizedAddress = address?.ToLowerInvariant().Trim() ?? string.Empty;
            return $"WV-{serviceProviderId}_{barangayId}_{bandId}_{technologyId}_{normalizedAddress}";
        }
    }
}