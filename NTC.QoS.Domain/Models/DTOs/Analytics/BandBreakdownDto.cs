namespace NTC.QoS.Domain.Models.DTOs.Analytics;

/// <summary>
/// Band frequency distribution breakdown for connectivity analysis
/// Shows deployment counts across different frequency bands
/// </summary>
public class BandBreakdownDto
{
    /// <summary>
    /// Count of 700 MHz (Band 28) deployments
    /// </summary>
    public int Count700MHz { get; set; }
    
    /// <summary>
    /// Count of 850 MHz (Band 5) deployments
    /// </summary>
    public int Count850MHz { get; set; }
    
    /// <summary>
    /// Count of 900 MHz (Band 8) deployments
    /// </summary>
    public int Count900MHz { get; set; }
    
    /// <summary>
    /// Count of 1800 MHz (Band 3) deployments
    /// </summary>
    public int Count1800MHz { get; set; }
    
    /// <summary>
    /// Count of 2100 MHz (Band 1) deployments
    /// </summary>
    public int Count2100MHz { get; set; }
    
    /// <summary>
    /// Count of 2300 MHz (Band 40) deployments
    /// </summary>
    public int Count2300MHz { get; set; }
    
    /// <summary>
    /// Count of 2500 MHz (Band 41) deployments
    /// </summary>
    public int Count2500MHz { get; set; }
    
    /// <summary>
    /// Count of 2600 MHz (Band 38/42) deployments
    /// </summary>
    public int Count2600MHz { get; set; }
    
    /// <summary>
    /// Count of 3500 MHz (Band 78) deployments
    /// </summary>
    public int Count3500MHz { get; set; }
    
    /// <summary>
    /// List of available band names
    /// </summary>
    public List<string> AvailableBands { get; set; } = [];
    
    /// <summary>
    /// Dominant band based on deployment count
    /// </summary>
    public string? DominantBand { get; set; }
    
    /// <summary>
    /// Total band deployment count
    /// </summary>
    public int TotalBandCount => Count700MHz + Count850MHz + Count900MHz + Count1800MHz + Count2100MHz + Count2300MHz + Count2500MHz + Count2600MHz + Count3500MHz;
    
    /// <summary>
    /// Whether high frequency bands (2300MHz+) are present
    /// </summary>
    public bool HasHighFrequencyBands => Count2300MHz > 0 || Count2500MHz > 0 || Count2600MHz > 0 || Count3500MHz > 0;
    
    /// <summary>
    /// Whether low frequency bands (sub-1GHz) are present
    /// </summary>
    public bool HasLowFrequencyBands => Count700MHz > 0 || Count850MHz > 0 || Count900MHz > 0;
    
    /// <summary>
    /// Percentage of high frequency deployments
    /// </summary>
    public double HighFrequencyRatio => TotalBandCount > 0 
        ? Math.Round((double)(Count2300MHz + Count2500MHz + Count2600MHz + Count3500MHz) / TotalBandCount * 100, 2) 
        : 0;
}
