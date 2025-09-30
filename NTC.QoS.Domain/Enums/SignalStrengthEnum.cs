namespace NTC.QoS.Domain.Enums;

/// <summary>
/// Signal strength classifications based on RSSI values in dBm
/// Aligned with speedtest data: Good (40-70), Average (70-80), Weak (80-90), Very Weak (90-100), NBC (100+)
/// Note: Values are absolute (positive) for easier comparison, but represent negative dBm values
/// </summary>
public enum SignalStrengthEnum
{
    /// <summary>
    /// No Broadband Coverage: > 100 dBm (or RSSI > 100)
    /// </summary>
    NoBroadbandCoverage = 1,   // > 100 dBm
    
    /// <summary>
    /// Very Weak: 90-100 dBm
    /// </summary>
    VeryWeak = 2,              // 90-100 dBm
    
    /// <summary>
    /// Weak: 80-90 dBm  
    /// </summary>
    Weak = 3,                  // 80-90 dBm
    
    /// <summary>
    /// Average: 70-80 dBm
    /// </summary>
    Average = 4,               // 70-80 dBm
    
    /// <summary>
    /// Good: 40-70 dBm
    /// </summary>
    Good = 5                   // 40-70 dBm
}