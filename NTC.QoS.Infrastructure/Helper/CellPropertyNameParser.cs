namespace NTC.QoS.Infrastructure.Helper;

/// <summary>
/// Result structure for parsing band/technology column values
/// </summary>      
public class BandTechnologyParseResult
{
    public bool IsActive { get; set; }
    public decimal? DLCenterFreq { get; set; }
    public decimal? Bandwidth { get; set; }
}

/// <summary>
/// Helper class for parsing band, generation, and technology information from Excel property names
/// </summary>
public static class CellPropertyNameParser
{
    /// <summary>
    /// Extracts the band designation from a property name based on frequency patterns
    /// </summary>
    /// <param name="propertyName">The property name to parse</param>
    /// <returns>The band designation (e.g., "Band 28") or empty string if no match found</returns>
    public static string GetBandFromPropertyName(string propertyName)
    {
        if (string.IsNullOrEmpty(propertyName))
            return string.Empty;

        return propertyName switch
        {
            var name when name.Contains("700") => "Band 28",
            var name when name.Contains("850") => "Band 5",
            var name when name.Contains("900") => "Band 8",
            var name when name.Contains("1800") => "Band 3",
            var name when name.Contains("2100") => "Band 1",
            var name when name.Contains("2300") => "Band 40",
            var name when name.Contains("2500") => "Band 41",
            var name when name.Contains("2600") => "Band 38",
            var name when name.Contains("3400") => "Band 42",
            var name when name.Contains("3500") => "Band 78",
            _ => string.Empty
        };
    }

    /// <summary>
    /// Extracts the generation (2G, 3G, 4G, 5G) from a property name based on technology prefixes
    /// </summary>
    /// <param name="propertyName">The property name to parse</param>
    /// <returns>The generation designation (e.g., "2G", "3G", "4G", "5G") or empty string if no match found</returns>
    public static string GetGenerationFromPropertyName(string propertyName)
    {
        if (string.IsNullOrEmpty(propertyName))
            return string.Empty;

        return propertyName switch
        {
            var name when name.StartsWith("Gsm", StringComparison.OrdinalIgnoreCase) => "2G",
            var name when name.StartsWith("Wcdma", StringComparison.OrdinalIgnoreCase) => "3G",
            var name when name.StartsWith("FddLte", StringComparison.OrdinalIgnoreCase) || 
                          name.StartsWith("TddLte", StringComparison.OrdinalIgnoreCase) => "4G",
            var name when name.StartsWith("Nr", StringComparison.OrdinalIgnoreCase) => "5G",
            _ => string.Empty
        };
    }

    /// <summary>
    /// Extracts the technology standard from a property name based on technology prefixes
    /// </summary>
    /// <param name="propertyName">The property name to parse</param>
    /// <returns>The technology standard (e.g., "GSM", "WCDMA", "FDDLTE", "TDDLTE", "NR") or empty string if no match found</returns>
    public static string GetTechnologyFromPropertyName(string propertyName)
    {
        if (string.IsNullOrEmpty(propertyName))
            return string.Empty;

        return propertyName switch
        {
            var name when name.StartsWith("Gsm", StringComparison.OrdinalIgnoreCase) => "GSM",
            var name when name.StartsWith("Wcdma", StringComparison.OrdinalIgnoreCase) => "WCDMA",
            var name when name.StartsWith("FddLte", StringComparison.OrdinalIgnoreCase) => "FDDLTE", // Match database convention
            var name when name.StartsWith("TddLte", StringComparison.OrdinalIgnoreCase) => "TDDLTE", // Match database convention
            var name when name.StartsWith("Nr", StringComparison.OrdinalIgnoreCase) => "NR",
            _ => string.Empty
        };
    }

    /// <summary>
    /// Parses a property name and extracts all relevant cellular information
    /// </summary>
    /// <param name="propertyName">The property name to parse</param>
    /// <returns>A tuple containing (Band, Generation, Technology)</returns>
    public static (string Band, string Generation, string Technology) ParsePropertyName(string propertyName)
    {
        if (string.IsNullOrEmpty(propertyName))
            return (string.Empty, string.Empty, string.Empty);

        var band = GetBandFromPropertyName(propertyName);
        var generation = GetGenerationFromPropertyName(propertyName);
        var technology = GetTechnologyFromPropertyName(propertyName);

        return (band, generation, technology);
    }

    /// <summary>
    /// Validates if a property name contains valid cellular technology information
    /// </summary>
    /// <param name="propertyName">The property name to validate</param>
    /// <returns>True if the property name contains valid band and technology information</returns>
    public static bool IsValidCellularPropertyName(string propertyName)
    {
        if (string.IsNullOrEmpty(propertyName))
            return false;

        var (band, _, technology) = ParsePropertyName(propertyName);
        return !string.IsNullOrEmpty(band) && !string.IsNullOrEmpty(technology);
    }
}