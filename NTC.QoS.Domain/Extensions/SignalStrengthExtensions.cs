using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;

namespace NTC.QoS.Domain.Extensions;

/// <summary>
/// Extension methods for signal strength operations
/// Provides fluent API for working with RSSI values and signal strength classifications
/// </summary>
public static class SignalStrengthExtensions
{
    /// <summary>
    /// Converts an RSSI value to SignalStrengthEnum
    /// </summary>
    /// <param name="rssi">RSSI value in dBm</param>
    /// <returns>Corresponding SignalStrengthEnum</returns>
    public static SignalStrengthEnum ToSignalStrength(this decimal rssi)
    {
        return SignalStrengthHelper.ClassifyRSSI(rssi);
    }

    /// <summary>
    /// Converts an RSSI value to SignalStrengthEnum (double overload)
    /// </summary>
    /// <param name="rssi">RSSI value in dBm</param>
    /// <returns>Corresponding SignalStrengthEnum</returns>
    public static SignalStrengthEnum ToSignalStrength(this double rssi)
    {
        return SignalStrengthHelper.ClassifyRSSI((decimal)rssi);
    }

    /// <summary>
    /// Gets a human-readable description of the signal strength
    /// </summary>
    /// <param name="signalStrength">SignalStrengthEnum value</param>
    /// <returns>Description string</returns>
    public static string GetDescription(this SignalStrengthEnum signalStrength)
    {
        return SignalStrengthHelper.GetDescription(signalStrength);
    }

    /// <summary>
    /// Gets the RSSI range for this signal strength classification
    /// </summary>
    /// <param name="signalStrength">SignalStrengthEnum value</param>
    /// <returns>RSSI range description</returns>
    public static string GetRSSIRange(this SignalStrengthEnum signalStrength)
    {
        return SignalStrengthHelper.GetRSSIRangeDescription(signalStrength);
    }

    /// <summary>
    /// Checks if the signal strength meets minimum broadband requirements
    /// </summary>
    /// <param name="signalStrength">SignalStrengthEnum value</param>
    /// <returns>True if meets minimum requirements</returns>
    public static bool MeetsMinimumRequirement(this SignalStrengthEnum signalStrength)
    {
        return SignalStrengthHelper.MeetsMinimumRequirement(signalStrength);
    }

    /// <summary>
    /// Parses a signal quality string to SignalStrengthEnum
    /// </summary>
    /// <param name="quality">Signal quality string</param>
    /// <returns>Corresponding SignalStrengthEnum</returns>
    public static SignalStrengthEnum ToSignalStrength(this string quality)
    {
        return SignalStrengthHelper.ParseSignalQualityString(quality);
    }

    /// <summary>
    /// Validates if RSSI value is within acceptable range
    /// </summary>
    /// <param name="rssi">RSSI value to validate</param>
    /// <returns>True if valid RSSI range</returns>
    public static bool IsValidRSSI(this decimal rssi)
    {
        return SignalStrengthHelper.IsValidRSSI(rssi);
    }

    /// <summary>
    /// Calculates average signal strength for a collection of RSSI values
    /// </summary>
    /// <param name="rssiValues">Collection of RSSI values</param>
    /// <returns>Average signal strength score (1-5)</returns>
    public static double CalculateAverageSignalStrength(this IEnumerable<decimal> rssiValues)
    {
        return SignalStrengthHelper.CalculateAverageSignalStrength(rssiValues, Enumerable.Empty<string>());
    }

    /// <summary>
    /// Calculates average signal strength for a collection of signal quality strings
    /// </summary>
    /// <param name="signalQualities">Collection of signal quality strings</param>
    /// <returns>Average signal strength score (1-5)</returns>
    public static double CalculateAverageSignalStrength(this IEnumerable<string> signalQualities)
    {
        return SignalStrengthHelper.CalculateAverageSignalStrength(Enumerable.Empty<decimal>(), signalQualities);
    }

    /// <summary>
    /// Gets description based on average signal strength score
    /// </summary>
    /// <param name="averageScore">Average signal strength score (1-5)</param>
    /// <returns>Description string</returns>
    public static string GetSignalStrengthDescription(this double averageScore)
    {
        return SignalStrengthHelper.GetDescriptionFromScore(averageScore);
    }
}