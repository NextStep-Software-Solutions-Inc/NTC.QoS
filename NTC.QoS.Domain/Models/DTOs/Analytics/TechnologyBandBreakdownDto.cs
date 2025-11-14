using System.Text.Json.Serialization;

namespace NTC.QoS.Domain.Models.DTOs.Analytics;

/// <summary>
/// Technology-specific band availability breakdown with technology summary
/// Dictionary where key is technology standard name (e.g., "FDD-LTE", "NR", "HSPAP") 
/// and value is list of unique band names available for that technology (e.g., "Band 1", "Band 41")
/// Also includes technology generation summary and statistics
/// </summary>
[JsonConverter(typeof(TechnologyBandBreakdownDtoJsonConverter))]
public class TechnologyBandBreakdownDto : Dictionary<string, List<string>>
{
    // ===== Technology/Band Lists =====
    
    /// <summary>
    /// List of available technology standards (e.g., "FDD-LTE", "TDD-LTE", "NR", "HSPAP", "EDGE")
    /// </summary>
    [JsonPropertyName("availableTechnologies")]
    public List<string> AvailableTechnologies => this.Where(kvp => kvp.Value.Any()).Select(kvp => kvp.Key).OrderBy(t => t).ToList();
    
    /// <summary>
    /// List of available generation names (e.g., "2G", "3G", "4G", "5G")
    /// Derived from technologies present
    /// </summary>
    [JsonPropertyName("availableGenerations")]
    public List<string> AvailableGenerations { get; set; } = [];
    
    /// <summary>
    /// All unique bands across all technologies
    /// </summary>
    [JsonPropertyName("allBands")]
    public List<string> AllBands => this.Values.SelectMany(v => v).Distinct().OrderBy(b => b).ToList();
    
    // ===== Generation Counts (Deployment Density) =====
    
    /// <summary>
    /// Count of 2G technology deployments (total infrastructure)
    /// </summary>
    [JsonPropertyName("count2G")]
    public int Count2G { get; set; }
    
    /// <summary>
    /// Count of 3G technology deployments (total infrastructure)
    /// </summary>
    [JsonPropertyName("count3G")]
    public int Count3G { get; set; }
    
    /// <summary>
    /// Count of 4G/LTE technology deployments (total infrastructure)
    /// </summary>
    [JsonPropertyName("count4G")]
    public int Count4G { get; set; }
    
    /// <summary>
    /// Count of 5G technology deployments (total infrastructure)
    /// </summary>
    [JsonPropertyName("count5G")]
    public int Count5G { get; set; }
    
    // ===== Unique Technology Availability (Yes/No Presence) =====
    
    /// <summary>
    /// Whether 2G technology is available in this area (true/false)
    /// Used for availability reports and tables
    /// </summary>
    [JsonPropertyName("has2G")]
    public bool Has2G => Count2G > 0;
    
    /// <summary>
    /// Whether 3G technology is available in this area (true/false)
    /// Used for availability reports and tables
    /// </summary>
    [JsonPropertyName("has3G")]
    public bool Has3G => Count3G > 0;
    
    /// <summary>
    /// Whether 4G/LTE technology is available in this area (true/false)
    /// Used for availability reports and tables
    /// </summary>
    [JsonPropertyName("has4G")]
    public bool Has4G => Count4G > 0;
    
    /// <summary>
    /// Whether 5G technology is available in this area (true/false)
    /// Used for availability reports and tables
    /// </summary>
    [JsonPropertyName("has5G")]
    public bool Has5G => Count5G > 0;
    
    // ===== Dominant/Primary Technology =====
    
    /// <summary>
    /// Dominant generation based on deployment count (e.g., "5G", "4G", "3G", "2G")
    /// </summary>
    [JsonPropertyName("dominantGeneration")]
    public string? DominantGeneration { get; set; }
    
    /// <summary>
    /// Primary technology standard based on deployment count (e.g., "FDD-LTE", "NR")
    /// </summary>
    [JsonPropertyName("primaryTechnology")]
    public string PrimaryTechnology { get; set; } = string.Empty;
    
    // ===== Modern/Legacy Analysis =====
    
    /// <summary>
    /// Whether modern technologies (4G/5G) are present
    /// </summary>
    [JsonPropertyName("hasModernTechnology")]
    public bool HasModernTechnology => Count4G > 0 || Count5G > 0;
    
    /// <summary>
    /// Whether legacy technologies (2G/3G only) dominate
    /// </summary>
    [JsonPropertyName("isLegacyDominated")]
    public bool IsLegacyDominated => (Count2G + Count3G) > (Count4G + Count5G) && (Count4G + Count5G) > 0;
    
    /// <summary>
    /// Whether modern coverage (4G/5G/LTE) is present
    /// </summary>
    [JsonPropertyName("hasModernCoverage")]
    public bool HasModernCoverage { get; set; }
    
    /// <summary>
    /// Percentage of deployments with modern technology (4G/5G)
    /// </summary>
    [JsonPropertyName("modernCoverageRatio")]
    public double ModernCoverageRatio { get; set; }
    
    // ===== Total Counts =====
    
    /// <summary>
    /// Total technology deployment count across all generations
    /// </summary>
    [JsonPropertyName("totalTechnologyCount")]
    public int TotalTechnologyCount { get; set; }
    
    /// <summary>
    /// Total number of unique bands across all technologies
    /// </summary>
    [JsonPropertyName("totalUniqueBands")]
    public int TotalUniqueBands => AllBands.Count;
    
    /// <summary>
    /// Whether there are any bands available
    /// </summary>
    [JsonPropertyName("hasBands")]
    public bool HasBands => this.Any() && this.Values.Any(v => v.Any());
}
