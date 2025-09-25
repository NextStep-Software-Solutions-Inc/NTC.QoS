using Microsoft.Extensions.Logging;
using System.Linq;

namespace NTC.QoS.Infrastructure.Helper;

/// <summary>
/// Helper class for technology mapping and resolution with alternative technology fallbacks
/// </summary>
public static class TechnologyMappingHelper
{
    /// <summary>
    /// Attempts to resolve a technology ID from the cache with fallback options for alternative technologies
    /// </summary>
    /// <param name="technology">The original technology name</param>
    /// <param name="technologyCache">The technology cache dictionary</param>
    /// <param name="logger">Logger for tracking resolution attempts</param>
    /// <param name="contextInfo">Additional context information for logging (e.g., row number, record identifier)</param>
    /// <returns>The resolved technology ID, or Guid.Empty if not found</returns>
    public static Guid ResolveTechnologyId(string technology, Dictionary<string, Guid> technologyCache, ILogger logger, string contextInfo = "")
    {
        if (string.IsNullOrWhiteSpace(technology))
        {
            logger.LogWarning("Empty or null technology provided{Context}", GetContextString(contextInfo));
            return Guid.Empty;
        }

        var originalTechnology = technology;
        var technologyCacheKey = CacheKeyGenerator.GenerateTechnologyCacheKey(technology);
        
        logger.LogDebug("Attempting to resolve technology '{Technology}' with cache key '{CacheKey}'{Context}", 
            technology, technologyCacheKey, GetContextString(contextInfo));
        
        // Try exact match first
        if (technologyCache.TryGetValue(technologyCacheKey, out var technologyId))
        {
            logger.LogDebug("Exact match found for technology '{Technology}'{Context}", 
                technology, GetContextString(contextInfo));
            return technologyId;
        }

        logger.LogDebug("Exact match not found for technology '{Technology}', trying flexible lookup{Context}", 
            technology, GetContextString(contextInfo));

        // Try flexible lookup for TDD/FDD technologies by checking for keywords
        if (technology.Contains("TDD", StringComparison.OrdinalIgnoreCase))
        {
            var tddTechnology = technologyCache.FirstOrDefault(kvp =>
                kvp.Key.Contains("tdd", StringComparison.OrdinalIgnoreCase));
            if (tddTechnology.Key != null)
            {
                technologyId = tddTechnology.Value;
                logger.LogInformation("Technology '{Technology}' not found, using TDD technology '{FoundTechnology}'{Context}",
                    technology, tddTechnology.Key, GetContextString(contextInfo));
                return technologyId;
            }
        }
        else if (technology.Contains("FDD", StringComparison.OrdinalIgnoreCase))
        {
            var fddTechnology = technologyCache.FirstOrDefault(kvp =>
                kvp.Key.Contains("fdd", StringComparison.OrdinalIgnoreCase));
            if (fddTechnology.Key != null)
            {
                technologyId = fddTechnology.Value;
                logger.LogInformation("Technology '{Technology}' not found, using FDD technology '{FoundTechnology}'{Context}",
                    technology, fddTechnology.Key, GetContextString(contextInfo));
                return technologyId;
            }
        }

        logger.LogDebug("Flexible TDD/FDD lookup failed for technology '{Technology}', trying alternative mappings{Context}", 
            technology, GetContextString(contextInfo));

        // Try alternative technology mappings (expanded for more flexibility)
        string? alternativeTechnology = technology.ToUpperInvariant() switch
        {
            "GSM" => "EDGE",
            "EDGE" => "GSM", // Reverse mapping for flexibility
            "WCDMA" => "HSPAP",
            "HSPAP" => "WCDMA", // Reverse mapping for HSPAP to WCDMA
            "HSPA+" => "HSPAP", // Common alternative notation
            "HSPA" => "HSPAP", // Alternative notation
            "UMTS" => "WCDMA", // UMTS is another name for WCDMA
            "FDDLTE" => "LTE",
            "TDDLTE" => "LTE",
            "LTE-FDD" => "LTE", // Hyphenated variant
            "LTE-TDD" => "LTE", // Hyphenated variant
            "LTE" => TryGetSpecificLTEVariant(technologyCache), // Try to find specific LTE variant if generic LTE fails
            "NR" => "5G", // 5G NR alternative
            "5G" => "NR", // Reverse mapping
            _ => null
        };

        if (alternativeTechnology != null)
        {
            logger.LogDebug("Found alternative technology mapping: '{Technology}' -> '{AlternativeTechnology}'{Context}", 
                technology, alternativeTechnology, GetContextString(contextInfo));
            
            var alternativeCacheKey = CacheKeyGenerator.GenerateTechnologyCacheKey(alternativeTechnology);
            if (technologyCache.TryGetValue(alternativeCacheKey, out technologyId))
            {
                logger.LogInformation("Technology '{Technology}' not found, using alternative '{Alternative}'{Context}",
                    technology, alternativeTechnology, GetContextString(contextInfo));
                return technologyId;
            }
            else
            {
                logger.LogWarning("Technology '{Technology}' and alternative '{Alternative}' not found in database{Context}. " +
                    "Available technologies: {AvailableTechnologies}",
                    technology, alternativeTechnology, GetContextString(contextInfo), string.Join(", ", technologyCache.Keys.OrderBy(k => k)));
            }
        }
        else
        {
            logger.LogWarning("Technology '{Technology}' not found in database and no alternative mapping available{Context}. " +
                "Available technologies: {AvailableTechnologies}",
                technology, GetContextString(contextInfo), string.Join(", ", technologyCache.Keys.OrderBy(k => k)));
        }

        logger.LogError("Failed to resolve technology '{Technology}' after trying all alternatives{Context}", 
            originalTechnology, GetContextString(contextInfo));
        return Guid.Empty;
    }

    /// <summary>
    /// Checks if a technology ID was successfully resolved (not Guid.Empty)
    /// </summary>
    /// <param name="technologyId">The technology ID to check</param>
    /// <returns>True if the technology ID is valid (not empty), false otherwise</returns>
    public static bool IsTechnologyResolved(Guid technologyId)
    {
        return technologyId != Guid.Empty;
    }

    /// <summary>
    /// Tries to find a specific LTE variant (FDD or TDD) when generic LTE lookup fails
    /// </summary>
    /// <param name="technologyCache">The technology cache dictionary</param>
    /// <returns>The first available LTE variant, or null if none found</returns>
    private static string? TryGetSpecificLTEVariant(Dictionary<string, Guid> technologyCache)
    {
        // Try common LTE variants in order of preference
        var lteVariants = new[] { "FDDLTE", "TDDLTE", "LTE-FDD", "LTE-TDD" };
        
        foreach (var variant in lteVariants)
        {
            var variantKey = CacheKeyGenerator.GenerateTechnologyCacheKey(variant);
            if (technologyCache.ContainsKey(variantKey))
            {
                return variant;
            }
        }
        
        return null;
    }

    /// <summary>
    /// Gets a comprehensive list of all alternative technology mappings for documentation purposes
    /// </summary>
    /// <returns>A dictionary of original technology to alternative technology mappings</returns>
    public static Dictionary<string, string> GetAlternativeTechnologyMappings()
    {
        return new Dictionary<string, string>
        {
            { "GSM", "EDGE" },
            { "EDGE", "GSM" },
            { "WCDMA", "HSPAP" },
            { "HSPAP", "WCDMA" },
            { "HSPA+", "HSPAP" },
            { "HSPA", "HSPAP" },
            { "UMTS", "WCDMA" },
            { "FDDLTE", "LTE" },
            { "TDDLTE", "LTE" },
            { "LTE-FDD", "LTE" },
            { "LTE-TDD", "LTE" },
            { "NR", "5G" },
            { "5G", "NR" }
        };
    }

    /// <summary>
    /// Validates that a band exists in the cache
    /// </summary>
    /// <param name="band">The band name</param>
    /// <param name="bandCache">The band cache dictionary</param>
    /// <param name="logger">Logger for tracking validation</param>
    /// <param name="contextInfo">Additional context information for logging</param>
    /// <returns>The band ID if found, or Guid.Empty if not found</returns>
    public static Guid ResolveBandId(string band, Dictionary<string, Guid> bandCache, ILogger logger, string contextInfo = "")
    {
        var bandCacheKey = CacheKeyGenerator.GenerateBandCacheKey(band);
        if (bandCache.TryGetValue(bandCacheKey, out var bandId))
        {
            return bandId;
        }

        logger.LogWarning("Band '{Band}' not found in database{Context}. " +
            "Available bands: {AvailableBands}",
            band, GetContextString(contextInfo), string.Join(", ", bandCache.Keys));

        return Guid.Empty;
    }

    /// <summary>
    /// Helper method to format context information for logging
    /// </summary>
    /// <param name="contextInfo">The context information</param>
    /// <returns>Formatted context string for logging</returns>
    private static string GetContextString(string contextInfo)
    {
        return string.IsNullOrWhiteSpace(contextInfo) ? "" : $" for {contextInfo}";
    }

    /// <summary>
    /// Logs diagnostic information about available technologies and their mappings
    /// </summary>
    /// <param name="technologyCache">The technology cache dictionary</param>
    /// <param name="logger">Logger for output</param>
    public static void LogTechnologyDiagnostics(Dictionary<string, Guid> technologyCache, ILogger logger)
    {
        logger.LogInformation("=== Technology Mapping Diagnostics ===");
        logger.LogInformation("Available technologies in database ({Count} total):", technologyCache.Count);
        
        foreach (var tech in technologyCache.OrderBy(kvp => kvp.Key))
        {
            logger.LogInformation("  - '{Technology}' (ID: {TechnologyId})", tech.Key, tech.Value);
        }
        
        logger.LogInformation("Alternative technology mappings supported:");
        var mappings = GetAlternativeTechnologyMappings();
        foreach (var mapping in mappings.OrderBy(kvp => kvp.Key))
        {
            var sourceExists = technologyCache.ContainsKey(CacheKeyGenerator.GenerateTechnologyCacheKey(mapping.Key));
            var targetExists = technologyCache.ContainsKey(CacheKeyGenerator.GenerateTechnologyCacheKey(mapping.Value));
            var status = (sourceExists, targetExists) switch
            {
                (true, true) => "? Both available",
                (true, false) => "? Source available, target missing",
                (false, true) => "? Source missing, target available",
                (false, false) => "? Both missing"
            };
            
            logger.LogInformation("  - '{Source}' -> '{Target}' ({Status})", 
                mapping.Key, mapping.Value, status);
        }
        
        logger.LogInformation("=====================================");
    }
}