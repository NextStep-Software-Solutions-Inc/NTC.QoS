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

        public static string GenerateNetworkCacheKey(string networkName)
        {
            return networkName.ToLowerInvariant();
        }

        /// <summary>
        /// Generates a standardized cache key for Generation technology only.
        /// Example: "4g", "5g", "lte"
        /// </summary>
        public static string GenerateGenerationCacheKey(string technologyStandard)
        {
            return technologyStandard.ToLowerInvariant();
        }
    }
}