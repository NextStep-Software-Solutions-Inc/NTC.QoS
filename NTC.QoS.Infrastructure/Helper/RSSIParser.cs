using System.Text.RegularExpressions;

namespace NTC.QoS.Infrastructure.Helper;

/// <summary>
/// Helper class for parsing RSSI values from wireless validation technology/band columns
/// </summary>
public static class RSSIParser
{
    /// <summary>
    /// Parses wireless validation data from various string formats found in Excel cells
    /// </summary>
    /// <param name="cellValue">The cell value containing wireless information</param>
    /// <param name="technology">The technology (GSM, WCDMA, FDDLTE, TDDLTE, NR) for threshold determination</param>
    /// <returns>Tuple containing (shouldCreate, rssiValue, isMarginal)</returns>
    public static (bool ShouldCreate, decimal? RSSIValue, bool IsMarginal) ParseWirelessData(string cellValue, string technology)
    {
        if (string.IsNullOrWhiteSpace(cellValue))
            return (false, null, false);

        var trimmedValue = cellValue.Trim().ToLowerInvariant();

        // Handle boolean-like values
        switch (trimmedValue)
        {
            case "false":
            case "0":
            case "no":
            case "n":
            case "":
            case "n/a":
            case "na":
            case "none":
            case "null":
            case "empty":
            case "-":
                return (false, null, false); // Don't create entity

            case "marginal":
            case "margin":
            case "marg":
            case "m":
                return (true, null, true); // Create entity, no RSSI value, set marginal = true

            case "true":
            case "1":
            case "yes":
            case "y":
            case "good":
            case "ok":
                return (true, null, false); // Create entity, no RSSI value, set marginal = false
        }

        // Try to parse as RSSI value
        if (TryParseRSSIValue(cellValue, out decimal rssiValue))
        {
            // Check for NBC (No Broadband Coverage) thresholds
            if (IsNoBroadbandCoverage(rssiValue, technology))
            {
                return (false, null, false); // Don't create entity for NBC
            }

            // Determine if marginal based on technology-specific thresholds
            bool isMarginal = IsMarginalByTechnology(rssiValue, technology);
            return (true, rssiValue, isMarginal);
        }

        // If we can't parse it, don't create the entity
        return (false, null, false);
    }

    /// <summary>
    /// Tries to parse RSSI value from various common formats
    /// </summary>
    private static bool TryParseRSSIValue(string value, out decimal rssi)
    {
        rssi = 0;

        // Pattern 1: "RSSI: -85 dBm" or "RSSI: -85"
        var rssiPattern = new Regex(@"RSSI\s*:\s*(-?\d+(?:\.\d+)?)", RegexOptions.IgnoreCase);
        var match = rssiPattern.Match(value);
        if (match.Success && decimal.TryParse(match.Groups[1].Value, out rssi))
        {
            return ValidateRSSIRange(rssi);
        }

        // Pattern 2: "Good: -70" or "Fair: -80" or "Poor: -90"
        var qualityPattern = new Regex(@"(?:Good|Fair|Poor|Excellent|Bad)\s*:\s*(-?\d+(?:\.\d+)?)", RegexOptions.IgnoreCase);
        match = qualityPattern.Match(value);
        if (match.Success && decimal.TryParse(match.Groups[1].Value, out rssi))
        {
            return ValidateRSSIRange(rssi);
        }

        // Pattern 3: Just the number "-85" or "-85.5"
        if (decimal.TryParse(value, out rssi))
        {
            return ValidateRSSIRange(rssi);
        }

        // Pattern 4: "Signal: -75 dBm" or similar signal indicators
        var signalPattern = new Regex(@"(?:Signal|Power|Level)\s*:\s*(-?\d+(?:\.\d+)?)", RegexOptions.IgnoreCase);
        match = signalPattern.Match(value);
        if (match.Success && decimal.TryParse(match.Groups[1].Value, out rssi))
        {
            return ValidateRSSIRange(rssi);
        }

        // Pattern 5: Extract any negative number from the string
        var negativeNumberPattern = new Regex(@"(-\d+(?:\.\d+)?)");
        match = negativeNumberPattern.Match(value);
        if (match.Success && decimal.TryParse(match.Groups[1].Value, out rssi))
        {
            return ValidateRSSIRange(rssi);
        }

        return false;
    }

    /// <summary>
    /// Validates if RSSI value is within reasonable range
    /// </summary>
    private static bool ValidateRSSIRange(decimal rssi)
    {
        // Validate it's a reasonable RSSI value (typically between -150 and 0)
        return rssi >= -150 && rssi <= 0;
    }

    /// <summary>
    /// Determines if signal has No Broadband Coverage (NBC) based on technology-specific thresholds
    /// </summary>
    /// <param name="rssi">The RSSI value in dBm</param>
    /// <param name="technology">The technology standard</param>
    /// <returns>True if signal is considered NBC (should not create entity)</returns>
    public static bool IsNoBroadbandCoverage(decimal rssi, string technology)
    {
        return technology?.ToUpperInvariant() switch
        {
            "GSM" or "WCDMA" => rssi <= -105,        // 2G/3G: NBC at -105 dBm or lower
            "FDDLTE" or "TDDLTE" or "LTE" or "NR" => rssi <= -120,  // 4G/5G: NBC at -120 dBm or lower
            _ => false // Unknown technology, don't filter out
        };
    }

    /// <summary>
    /// Determines if a signal is marginal based on technology-specific thresholds
    /// </summary>
    /// <param name="rssi">The RSSI value in dBm</param>
    /// <param name="technology">The technology standard</param>
    /// <returns>True if the signal is considered marginal</returns>
    public static bool IsMarginalByTechnology(decimal rssi, string technology)
    {
        return technology?.ToUpperInvariant() switch
        {
            "GSM" or "WCDMA" => rssi <= -90,         // 2G/3G: Marginal at -90 dBm or lower
            "FDDLTE" or "TDDLTE" or "LTE" or "NR" => rssi <= -100,  // 4G/5G: Marginal at -100 dBm or lower
            _ => rssi < -85 // Default threshold for unknown technologies
        };
    }

    /// <summary>
    /// Gets a human-readable signal quality description based on RSSI and technology
    /// </summary>
    /// <param name="rssi">The RSSI value in dBm</param>
    /// <param name="technology">The technology standard</param>
    /// <returns>Signal quality description</returns>
    public static string GetSignalQualityDescription(decimal rssi, string technology)
    {
        if (IsNoBroadbandCoverage(rssi, technology))
            return "No Broadband Coverage (NBC)";

        if (IsMarginalByTechnology(rssi, technology))
            return "Marginal";

        return technology?.ToUpperInvariant() switch
        {
            "GSM" or "WCDMA" => rssi switch
            {
                >= -70 => "Excellent",
                >= -80 => "Good",
                >= -90 => "Fair",
                _ => "Marginal"
            },
            "FDDLTE" or "TDDLTE" or "LTE" or "NR" => rssi switch
            {
                >= -80 => "Excellent", 
                >= -90 => "Good",
                >= -100 => "Fair",
                _ => "Marginal"
            },
            _ => rssi switch
            {
                >= -70 => "Excellent",
                >= -80 => "Good",
                >= -85 => "Fair",
                _ => "Poor"
            }
        };
    }

    /// <summary>
    /// Validates if a technology/band column has meaningful data that should be processed
    /// </summary>
    /// <param name="cellValue">The cell value to validate</param>
    /// <param name="technology">The technology for context</param>
    /// <returns>True if the cell contains processable wireless validation data</returns>
    public static bool HasValidWirelessData(string cellValue, string technology = "")
    {
        var (shouldCreate, _, _) = ParseWirelessData(cellValue, technology);
        return shouldCreate;
    }

    /// <summary>
    /// Gets generation from technology standard for threshold determination
    /// </summary>
    /// <param name="technology">Technology standard</param>
    /// <returns>Generation designation</returns>
    public static string GetGenerationFromTechnology(string technology)
    {
        return technology?.ToUpperInvariant() switch
        {
            "GSM" => "2G",
            "WCDMA" => "3G",
            "FDDLTE" or "TDDLTE" or "LTE" => "4G",
            "NR" => "5G",
            _ => "Unknown"
        };
    }
}