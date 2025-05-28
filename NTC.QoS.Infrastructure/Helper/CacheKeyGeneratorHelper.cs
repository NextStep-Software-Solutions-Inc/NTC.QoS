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
        /// Generates a standardized cache key for Generation Bands.
        /// Format: (Generation, Band) tuple.
        /// </summary>
        /// <param name="generation">The generation name (e.g., "4G").</param>
        /// <param name="band">The band name (e.g., "Band 3").</param>
        /// <returns>A tuple representing the generation band cache key.</returns>
        public static (string Generation, string Band) GenerateGenerationBandCacheKey(string generation, string band)
        {
            // This reflects your existing logic for this key.
            // You might add .Trim() or .ToLowerInvariant() here if future consistency demands it,
            // but for now, matching the current tuple formation is key.
            return (generation, band);
        }

        /// <summary>
        /// Generates a standardized cache key for Cells based on ServiceProviderId, TowerType, and Location.
        /// Format: "Cell-ServiceProviderId_TowerType_Longitude_Latitude" (case-sensitive as per current usage).
        /// </summary>
        /// <param name="serviceProviderId">The GUID of the service provider.</param>
        /// <param name="towerType">The tower type (e.g., "Macro").</param>
        /// <param name="location">The NetTopologySuite Point object for the cell's location.</param>
        /// <returns>The standardized cache key for the cell.</returns>
        public static string GenerateCellCacheKey(Guid serviceProviderId, string towerType, Point location)
        {
            // This directly reflects your existing logic for this key.
            // Note: Your current code uses c.Location.Coordinate.X and c.Location.Coordinate.Y.
            // Ensure `location.Coordinate.X` and `location.Coordinate.Y` yield the same precision/format
            // as what's stored in the database for `c.Location.Coordinate.X` and `c.Location.Coordinate.Y`.
            return $"Cell-{serviceProviderId}_{towerType}_{location.Coordinate.X}_{location.Coordinate.Y}";
        }

        public static string GenerateNetworkCacheKey(string networkName)
        {
            return networkName.ToLowerInvariant();
        }
    }
}