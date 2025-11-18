using System.Text.Json.Serialization;

namespace NTC.QoS.Domain.Models.DTOs.Analytics;

/// <summary>
/// Detailed band breakdown for a specific technology showing cellular, validation, and combined bands
/// </summary>
public class TechnologyBandDetailDto
{
    /// <summary>
    /// Bands detected from cellular infrastructure (cell towers)
    /// </summary>
    [JsonPropertyName("cellularBands")]
    public List<string> CellularBands { get; set; } = [];

    /// <summary>
    /// Bands detected from wireless validation measurements
    /// </summary>
    [JsonPropertyName("validationBands")]
    public List<string> ValidationBands { get; set; } = [];

    /// <summary>
    /// All unique bands from both cellular and validation sources
    /// </summary>
    [JsonPropertyName("allBands")]
    public List<string> AllBands { get; set; } = [];
}

/// <summary>
/// Complete technology band breakdown by location
/// Dictionary where key is technology standard (e.g., "FDD-LTE", "NR")
/// and value contains detailed band information from different sources
/// </summary>
[JsonConverter(typeof(DetailedTechnologyBandBreakdownDtoJsonConverter))]
public class DetailedTechnologyBandBreakdownDto : Dictionary<string, TechnologyBandDetailDto>
{
    /// <summary>
    /// List of available technology standards (e.g., "FDD-LTE", "TDD-LTE", "NR", "HSPAP", "EDGE")
    /// </summary>
    [JsonPropertyName("availableTechnologies")]
    public List<string> AvailableTechnologies { get; set; } = [];
    
    /// <summary>
    /// List of available generation names (e.g., "2G", "3G", "4G", "5G")
    /// </summary>
    [JsonPropertyName("availableGenerations")]
    public List<string> AvailableGenerations { get; set; } = [];
    
    /// <summary>
    /// All unique bands across all technologies
    /// </summary>
    [JsonPropertyName("allBands")]
    public List<string> AllBands { get; set; } = [];
    
    /// <summary>
    /// Count of unique bands available in 2G technology
    /// </summary>
    [JsonPropertyName("count2G")]
    public int Count2G { get; set; }
    
    /// <summary>
    /// Count of unique bands available in 3G technology
    /// </summary>
    [JsonPropertyName("count3G")]
    public int Count3G { get; set; }
    
    /// <summary>
    /// Count of unique bands available in 4G/LTE technology
    /// </summary>
    [JsonPropertyName("count4G")]
    public int Count4G { get; set; }
    
    /// <summary>
    /// Count of unique bands available in 5G technology
    /// </summary>
    [JsonPropertyName("count5G")]
    public int Count5G { get; set; }
    
    /// <summary>
    /// Whether 2G technology is available in this area (true/false)
    /// </summary>
    [JsonPropertyName("has2G")]
    public bool Has2G => Count2G > 0;
    
    /// <summary>
    /// Whether 3G technology is available in this area (true/false)
    /// </summary>
    [JsonPropertyName("has3G")]
    public bool Has3G => Count3G > 0;
    
    /// <summary>
    /// Whether 4G/LTE technology is available in this area (true/false)
    /// </summary>
    [JsonPropertyName("has4G")]
    public bool Has4G => Count4G > 0;
    
    /// <summary>
    /// Whether 5G technology is available in this area (true/false)
    /// </summary>
    [JsonPropertyName("has5G")]
    public bool Has5G => Count5G > 0;
    
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
    public bool HasBands => this.Any() && this.Values.Any(v => v.AllBands.Any());
}
