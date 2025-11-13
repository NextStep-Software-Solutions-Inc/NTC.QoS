namespace NTC.QoS.Domain.Models.DTOs.Analytics;

/// <summary>
/// Technology-specific band availability breakdown
/// Dictionary where key is technology standard name (e.g., "5G NR", "LTE", "UMTS", "GSM") 
/// and value is list of unique band names available for that technology (e.g., "Band 1", "Band 41")
/// </summary>
public class TechnologyBandBreakdownDto : Dictionary<string, List<string>>
{
    /// <summary>
    /// All unique bands across all technologies
    /// </summary>
    public List<string> AllBands => this.Values.SelectMany(v => v).Distinct().OrderBy(b => b).ToList();
    
    /// <summary>
    /// Total number of unique bands across all technologies
    /// </summary>
    public int TotalUniqueBands => AllBands.Count;
    
    /// <summary>
    /// Whether there are any bands available
    /// </summary>
    public bool HasBands => this.Any() && this.Values.Any(v => v.Any());
    
    /// <summary>
    /// Get all technology standards that have bands
    /// </summary>
    public List<string> TechnologiesWithBands => this.Where(kvp => kvp.Value.Any()).Select(kvp => kvp.Key).OrderBy(t => t).ToList();
}
