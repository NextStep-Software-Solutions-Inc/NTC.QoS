using System.Text.RegularExpressions;

namespace NTC.QoS.Infrastructure.Helper;

/// <summary>
/// Helper class for parsing RSRP values from wireless validation technology/band columns
/// </summary>
public static partial class RSRPParser
{
    #region RSRP Threshold Constants

    // No Broadband Coverage (NBC) Thresholds
    /// <summary>
    /// RSRP threshold for No Broadband Coverage in 2G/3G technologies (GSM, WCDMA)
    /// </summary>
    public const decimal NBC_THRESHOLD_2G_3G = -105m;

    /// <summary>
    /// RSRP threshold for No Broadband Coverage in 4G/5G technologies (LTE, NR)
    /// </summary>
    public const decimal NBC_THRESHOLD_4G_5G = -120m;

    /// <summary>
    /// Standard RSRP threshold for No Broadband Coverage - signals at -100 dBm or lower
    /// </summary>
    public const decimal NBC_THRESHOLD = -100m;

    // Marginal Coverage Thresholds (Technology-specific for IsMarginal boolean property)
    /// <summary>
    /// RSRP threshold for Marginal coverage in 2G/3G technologies (GSM, WCDMA)
    /// </summary>
    public const decimal MARGINAL_THRESHOLD_2G_3G = -90m;

    /// <summary>
    /// RSRP threshold for Marginal coverage in 4G/5G technologies (LTE, NR)
    /// </summary>
    public const decimal MARGINAL_THRESHOLD_4G_5G = -100m;

    /// <summary>
    /// Default marginal threshold for unknown technologies
    /// </summary>
    public const decimal MARGINAL_THRESHOLD_DEFAULT = -85m;

    // Signal Quality Thresholds (Standardized for quality descriptions)
    /// <summary>
    /// Good signal threshold - signals better than -70 dBm
    /// </summary>
    public const decimal GOOD_THRESHOLD = -70m;

    /// <summary>
    /// Average signal threshold - signals between -70 and -80 dBm
    /// </summary>
    public const decimal AVERAGE_THRESHOLD = -80m;

    /// <summary>
    /// Weak signal threshold - signals between -80 and -90 dBm
    /// </summary>
    public const decimal WEAK_THRESHOLD = -90m;

    /// <summary>
    /// Very Weak signal threshold - signals between -90 and -100 dBm
    /// </summary>
    public const decimal VERY_WEAK_THRESHOLD = -100m;

    // RSRP Validation Range
    /// <summary>
    /// Minimum valid RSRP value (most negative)
    /// </summary>
    public const decimal MIN_VALID_RSRP = -150m;

    /// <summary>
    /// Maximum valid RSRP value (typically 0 dBm)
    /// </summary>
    public const decimal MAX_VALID_RSRP = 0m;

    #endregion

    /// <summary>
    /// Parses wireless validation data from various string formats found in Excel cells
    /// </summary>
    /// <param name="cellValue">The cell value containing wireless information</param>
    /// <param name="technology">The technology (GSM, WCDMA, FDDLTE, TDDLTE, NR) for threshold determination</param>
    /// <returns>Tuple containing (shouldCreate, rsrpValue, isMarginal)</returns>
    public static (bool ShouldCreate, decimal? RSRPValue, bool IsMarginal) ParseWirelessData(string cellValue, string technology)
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
            case "weak":
            case "very weak":
                return (true, null, true); // Create entity, no RSRP value, set marginal = true

            case "true":
            case "1":
            case "yes":
            case "y":
            case "good":
            case "ok":
            case "average":
                return (true, null, false); // Create entity, no RSRP value, set marginal = false
        }

        // Try to parse as RSRP value
        if (TryParseRSRPValue(cellValue, out decimal rsrpValue))
        {
            // Check for NBC (No Broadband Coverage) thresholds using technology-specific logic
            if (IsNoBroadbandCoverage(rsrpValue, technology))
            {
                return (false, null, false); // Don't create entity for NBC
            }

            // Determine if marginal based on technology-specific thresholds (main basis for boolean property)
            bool isMarginal = IsMarginalByTechnology(rsrpValue, technology);
            return (true, rsrpValue, isMarginal);
        }

        // If we can't parse it, don't create the entity
        return (false, null, false);
    }

    /// <summary>
    /// Tries to parse RSRP value from various common formats
    /// </summary>
    private static bool TryParseRSRPValue(string value, out decimal rsrp)
    {
        // Pattern 1: "RSRP: -85 dBm" or "RSRP: -85"
        var match = RSRPRegex().Match(value);
        if (match.Success && decimal.TryParse(match.Groups[1].Value, out rsrp))
        {
            return ValidateRSRPRange(rsrp);
        }

        // Pattern 2: "Good: -70" or "Fair: -80" or "Poor: -90"
        match = QualityRegex().Match(value);
        if (match.Success && decimal.TryParse(match.Groups[1].Value, out rsrp))
        {
            return ValidateRSRPRange(rsrp);
        }

        // Pattern 3: Just the number "-85" or "-85.5"
        if (decimal.TryParse(value, out rsrp))
        {
            return ValidateRSRPRange(rsrp);
        }

        // Pattern 4: "Signal: -75 dBm" or similar signal indicators
        match = SignalRegex().Match(value);
        if (match.Success && decimal.TryParse(match.Groups[1].Value, out rsrp))
        {
            return ValidateRSRPRange(rsrp);
        }

        // Pattern 5: Extract any negative number from the string
        match = NegativeNumberRegex().Match(value);
        if (match.Success && decimal.TryParse(match.Groups[1].Value, out rsrp))
        {
            return ValidateRSRPRange(rsrp);
        }

        return false;
    }

    [GeneratedRegex(@"RSRP\s*:\s*(-?\d+(?:\.\d+)?)", RegexOptions.IgnoreCase)]
    private static partial Regex RSRPRegex();

    [GeneratedRegex(@"(?:Good|Fair|Poor|Excellent|Bad|Average|Weak)\s*:\s*(-?\d+(?:\.\d+)?)", RegexOptions.IgnoreCase)]
    private static partial Regex QualityRegex();

    [GeneratedRegex(@"(?:Signal|Power|Level)\s*:\s*(-?\d+(?:\.\d+)?)", RegexOptions.IgnoreCase)]
    private static partial Regex SignalRegex();

    [GeneratedRegex(@"(-\d+(?:\.\d+)?)")]
    private static partial Regex NegativeNumberRegex();

    /// <summary>
    /// Validates if RSRP value is within reasonable range
    /// </summary>
    private static bool ValidateRSRPRange(decimal rsrp)
    {
        // Validate it's a reasonable RSRP value using defined constants
        return rsrp >= MIN_VALID_RSRP && rsrp <= MAX_VALID_RSRP;
    }

    /// <summary>
    /// Determines if signal has No Broadband Coverage (NBC) based on technology-specific thresholds
    /// </summary>
    /// <param name="rsrp">The RSRP value in dBm</param>
    /// <param name="technology">The technology standard</param>
    /// <returns>True if signal is considered NBC (should not create entity)</returns>
    public static bool IsNoBroadbandCoverage(decimal rsrp, string technology)
    {
        return technology?.ToUpperInvariant() switch
        {
            "GSM" or "WCDMA" => rsrp <= NBC_THRESHOLD_2G_3G,        // 2G/3G: NBC at -105 dBm or lower
            "FDDLTE" or "TDDLTE" or "LTE" or "NR" => rsrp <= NBC_THRESHOLD_4G_5G,  // 4G/5G: NBC at -120 dBm or lower
            _ => false // Unknown technology, don't filter out
        };
    }

    /// <summary>
    /// Determines if signal has No Broadband Coverage (NBC) based on the standard threshold
    /// </summary>
    /// <param name="rsrp">The RSRP value in dBm</param>
    /// <returns>True if signal is considered NBC</returns>
    public static bool IsNoBroadbandCoverage(decimal rsrp)
    {
        return rsrp <= NBC_THRESHOLD; // NBC at -100 dBm or lower
    }

    /// <summary>
    /// Determines if a signal is marginal based on technology-specific thresholds (main basis for boolean property)
    /// </summary>
    /// <param name="rsrp">The RSRP value in dBm</param>
    /// <param name="technology">The technology standard</param>
    /// <returns>True if the signal is considered marginal</returns>
    public static bool IsMarginalByTechnology(decimal rsrp, string technology)
    {
        return technology?.ToUpperInvariant() switch
        {
            "GSM" or "WCDMA" => rsrp <= MARGINAL_THRESHOLD_2G_3G,         // 2G/3G: Marginal at -90 dBm or lower
            "FDDLTE" or "TDDLTE" or "LTE" or "NR" => rsrp <= MARGINAL_THRESHOLD_4G_5G,  // 4G/5G: Marginal at -100 dBm or lower
            _ => rsrp <= MARGINAL_THRESHOLD_DEFAULT // Default threshold for unknown technologies
        };
    }

    /// <summary>
    /// Determines if a signal is marginal using standardized thresholds
    /// </summary>
    /// <param name="rsrp">The RSRP value in dBm</param>
    /// <returns>True if the signal is considered marginal</returns>
    public static bool IsMarginal(decimal rsrp)
    {
        return rsrp <= AVERAGE_THRESHOLD; // Weak (-80 to -90) and Very Weak (-90 to -100) are considered marginal
    }

    /// <summary>
    /// Gets a human-readable signal quality description based on RSRP using standardized ranges
    /// </summary>
    /// <param name="rsrp">The RSRP value in dBm</param>
    /// <param name="technology">The technology standard (optional, for backward compatibility)</param>
    /// <returns>Signal quality description</returns>
    public static string GetSignalQualityDescription(decimal rsrp, string technology = "")
    {
        // Use standardized quality ranges for descriptions
        if (rsrp <= NBC_THRESHOLD)
            return "No Broadband Coverage (NBC)";

        return rsrp switch
        {
            > GOOD_THRESHOLD => "Good",           // Better than -70 dBm
            > AVERAGE_THRESHOLD => "Average",     // -70 to -80 dBm
            > WEAK_THRESHOLD => "Weak",           // -80 to -90 dBm
            > VERY_WEAK_THRESHOLD => "Very Weak", // -90 to -100 dBm
            _ => "No Broadband Coverage (NBC)"    // -100 dBm or lower
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