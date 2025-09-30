using NTC.QoS.Domain.Enums;

namespace NTC.QoS.Domain.Helper;

/// <summary>
/// Helper class for signal strength classification and processing
/// Provides utilities for working with RSSI values and SignalStrengthEnum
/// Aligned with NTC Philippines signal strength classifications
/// </summary>
public static class SignalStrengthHelper
{
    /// <summary>
    /// Classifies RSSI value into SignalStrengthEnum based on predefined ranges
    /// Uses NTC Philippines classifications:
    /// - Good: 40-70 dBm
    /// - Average: 70-80 dBm  
    /// - Weak: 80-90 dBm
    /// - Very Weak: 90-100 dBm
    /// - No Broadband Coverage: 100+ dBm
    /// </summary>
    /// <param name="rssi">RSSI value in dBm (positive values representing absolute signal strength)</param>
    /// <returns>Corresponding SignalStrengthEnum value</returns>
    public static SignalStrengthEnum ClassifyRSSI(decimal rssi)
    {
        return rssi switch
        {
            <= 70 => SignalStrengthEnum.Good,                    // Good (40-70 dBm)
            <= 80 => SignalStrengthEnum.Average,                 // Average (70-80 dBm)
            <= 90 => SignalStrengthEnum.Weak,                    // Weak (80-90 dBm)
            <= 100 => SignalStrengthEnum.VeryWeak,               // Very Weak (90-100 dBm)
            _ => SignalStrengthEnum.NoBroadbandCoverage          // No Broadband Coverage (100+ dBm)
        };
    }

    /// <summary>
    /// Parses signal quality string into SignalStrengthEnum
    /// Supports various string formats commonly used in speedtest data
    /// </summary>
    /// <param name="quality">Signal quality string (e.g., "good", "average", "weak")</param>
    /// <returns>Corresponding SignalStrengthEnum value, defaults to Average if unknown</returns>
    public static SignalStrengthEnum ParseSignalQualityString(string quality)
    {
        if (string.IsNullOrWhiteSpace(quality))
            return SignalStrengthEnum.Average;

        return quality.ToLower().Trim() switch
        {
            "good" => SignalStrengthEnum.Good,
            "average" => SignalStrengthEnum.Average,
            "weak" => SignalStrengthEnum.Weak,
            "very weak" or "veryweak" => SignalStrengthEnum.VeryWeak,
            "nbc" or "no broadband coverage" => SignalStrengthEnum.NoBroadbandCoverage,
            _ => SignalStrengthEnum.Average // Default to average if unknown
        };
    }

    /// <summary>
    /// Gets a human-readable description of the signal strength enum value
    /// </summary>
    /// <param name="signalStrength">SignalStrengthEnum value</param>
    /// <returns>Human-readable string description</returns>
    public static string GetDescription(SignalStrengthEnum signalStrength)
    {
        return signalStrength switch
        {
            SignalStrengthEnum.Good => "Good",
            SignalStrengthEnum.Average => "Average",
            SignalStrengthEnum.Weak => "Weak",
            SignalStrengthEnum.VeryWeak => "Very Weak",
            SignalStrengthEnum.NoBroadbandCoverage => "No Broadband Coverage",
            _ => "Unknown"
        };
    }

    /// <summary>
    /// Gets a human-readable description based on average signal strength score (1-5)
    /// </summary>
    /// <param name="averageSignalStrength">Average signal strength score (1-5)</param>
    /// <returns>Human-readable string description</returns>
    public static string GetDescriptionFromScore(double averageSignalStrength)
    {
        return averageSignalStrength switch
        {
            >= (double)SignalStrengthEnum.Good => "Good",
            >= (double)SignalStrengthEnum.Average => "Average",
            >= (double)SignalStrengthEnum.Weak => "Weak", 
            >= (double)SignalStrengthEnum.VeryWeak => "Very Weak",
            >= (double)SignalStrengthEnum.NoBroadbandCoverage => "No Broadband Coverage",
            _ => "Unknown"
        };
    }

    /// <summary>
    /// Calculates average signal strength based on RSSI values and signal quality strings
    /// Returns a score from 1-5 based on SignalStrengthEnum values
    /// </summary>
    /// <param name="rssiValues">List of RSSI values in dBm</param>
    /// <param name="signalQualities">List of signal quality strings</param>
    /// <returns>Average signal strength score (1-5), 0 if no data provided</returns>
    public static double CalculateAverageSignalStrength(IEnumerable<decimal> rssiValues, IEnumerable<string> signalQualities)
    {
        var rssiList = rssiValues?.ToList() ?? [];
        var qualityList = signalQualities?.ToList() ?? [];

        if (rssiList.Count == 0 && qualityList.Count == 0)
            return 0;

        var signalScores = new List<double>();

        // Convert RSSI values to scores using SignalStrengthEnum
        foreach (var rssi in rssiList)
        {
            var signalStrength = ClassifyRSSI(rssi);
            signalScores.Add((double)signalStrength);
        }

        // Convert signal quality strings to scores
        foreach (var quality in qualityList)
        {
            var signalStrength = ParseSignalQualityString(quality);
            signalScores.Add((double)signalStrength);
        }

        return signalScores.Count > 0 ? Math.Round(signalScores.Average(), 2) : 0;
    }

    /// <summary>
    /// Determines if the signal strength meets minimum requirements for broadband
    /// Based on NTC Philippines standards
    /// </summary>
    /// <param name="signalStrength">Signal strength enum value</param>
    /// <returns>True if signal strength is Average or better</returns>
    public static bool MeetsMinimumRequirement(SignalStrengthEnum signalStrength)
    {
        return signalStrength >= SignalStrengthEnum.Average;
    }

    /// <summary>
    /// Determines if the average signal strength score meets minimum requirements
    /// </summary>
    /// <param name="averageSignalStrength">Average signal strength score (1-5)</param>
    /// <returns>True if score is 4 or higher (Average or better)</returns>
    public static bool MeetsMinimumRequirement(double averageSignalStrength)
    {
        return averageSignalStrength >= (double)SignalStrengthEnum.Average;
    }

    /// <summary>
    /// Gets the RSSI range description for a given signal strength classification
    /// </summary>
    /// <param name="signalStrength">Signal strength enum value</param>
    /// <returns>RSSI range description string</returns>
    public static string GetRSSIRangeDescription(SignalStrengthEnum signalStrength)
    {
        return signalStrength switch
        {
            SignalStrengthEnum.Good => "40-70 dBm",
            SignalStrengthEnum.Average => "70-80 dBm",
            SignalStrengthEnum.Weak => "80-90 dBm",
            SignalStrengthEnum.VeryWeak => "90-100 dBm",
            SignalStrengthEnum.NoBroadbandCoverage => "100+ dBm",
            _ => "Unknown range"
        };
    }

    /// <summary>
    /// Validates if an RSSI value is within acceptable range for mobile communications
    /// </summary>
    /// <param name="rssi">RSSI value to validate</param>
    /// <returns>True if RSSI is within normal range (0-120 dBm)</returns>
    public static bool IsValidRSSI(decimal rssi)
    {
        return rssi >= 0 && rssi <= 120; // Typical range for mobile RSSI measurements
    }
}