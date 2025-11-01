namespace NTC.QoS.Domain.Models.DTOs.Analytics;

// 1. MAIN RESPONSE DTO
public class WirelessCoverageAnalyticsDto
{
    public int TotalCellTowers { get; set; }
    public Dictionary<string, int> TechnologyDistribution { get; set; } = [];
    public List<MunicipalityWirelessCoverageDto> CoverageByMunicipality { get; set; } = [];
    public SignalQualityAnalysisDto? SignalQualityAnalysis { get; set; }
    public List<ProviderCoverageDto> ProviderCoverage { get; set; } = [];

    // Additional summary metrics
    public WirelessCoverageSummaryDto? Summary { get; set; }
    public List<CoverageGapDto> CoverageGaps { get; set; } = [];
}

// 2. SUMMARY DTO
public class WirelessCoverageSummaryDto
{
    public double TotalCoverageAreaKm2 { get; set; }
    public double EstimatedPopulationCovered { get; set; }
    public int MunicipalitiesWithCoverage { get; set; }
    public int MunicipalitiesWithoutCoverage { get; set; }
    public string DominantTechnology { get; set; } = string.Empty;
    public double AverageSignalStrength { get; set; }
    public int ProviderCount { get; set; }
}

// 3. MUNICIPALITY WIRELESS COVERAGE DTO
public class MunicipalityWirelessCoverageDto
{
    public Guid? MunicipalityId { get; set; }
    public string MunicipalityName { get; set; } = string.Empty;
    public int CellTowerCount { get; set; }
    public double EstimatedCoveragePercent { get; set; }
    public List<string> AvailableTechnologies { get; set; } = [];
    public List<string> ServiceProviders { get; set; } = [];

    // Signal quality metrics
    public double AverageSignalStrength { get; set; }
    public string SignalQualityRating { get; set; } = string.Empty; // Excellent, Good, Fair, Poor 

    // Technology breakdown
    public TechnologyBreakdownDto? TechnologyBreakdown { get; set; }

    // Coverage assessment
    public string CoverageStatus { get; set; } = string.Empty; // Well-Covered, Partially-Covered, Poor-Coverage, No-Coverage
    public List<string> CoverageIssues { get; set; } = [];
    public List<string> Recommendations { get; set; } = [];

    // Geographic info
    public CoordinateDto? EstimatedCenter { get; set; }
    public double EstimatedAreaKm2 { get; set; }
}

// 4. TECHNOLOGY BREAKDOWN DTO
public class TechnologyBreakdownDto
{
    /// <summary>
    /// Count of 2G technology deployments
    /// </summary>
    public int Count2G { get; set; }
    
    /// <summary>
    /// Count of 3G technology deployments  
    /// </summary>
    public int Count3G { get; set; }
    
    /// <summary>
    /// Count of 4G/LTE technology deployments
    /// </summary>
    public int Count4G { get; set; }
    
    /// <summary>
    /// Count of 5G technology deployments
    /// </summary>
    public int Count5G { get; set; }
    
    /// <summary>
    /// List of available technology standards (e.g., GSM, WCDMA, LTE, NR)
    /// </summary>
    public List<string> AvailableTechnologies { get; set; } = [];
    
    /// <summary>
    /// List of available generation names
    /// </summary>
    public List<string> AvailableGenerations { get; set; } = [];
    
    /// <summary>
    /// Dominant generation based on deployment count
    /// </summary>
    public string? DominantGeneration { get; set; }

    // Calculated properties
    public string PrimaryTechnology { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether modern technologies (4G/5G) are present
    /// </summary>
    public bool HasModernTechnology => Count4G > 0 || Count5G > 0;
    
    /// <summary>
    /// Whether legacy technologies (2G/3G only) dominate
    /// </summary>
    public bool IsLegacyDominated => (Count2G + Count3G) > (Count4G + Count5G) && (Count4G + Count5G) > 0;
    
    /// <summary>
    /// Total technology deployment count
    /// </summary>
    public int TotalTechnologyCount => Count2G + Count3G + Count4G + Count5G;
    
    public bool HasModernCoverage { get; set; } // 4G/5G/LTE present
    public double ModernCoverageRatio { get; set; } // % of towers with 4G/5G/LTE
}

// 5. SIGNAL QUALITY ANALYSIS DTO
public class SignalQualityAnalysisDto
{
    public SignalQualityStatsDto? OverallStats { get; set; }
    public List<SignalQualityByTechnologyDto> QualityByTechnology { get; set; } = [];
    public List<SignalQualityByProviderDto> QualityByProvider { get; set; } = [];
    public List<SignalQualityZoneDto> QualityZones { get; set; } = [];
}

// 6. SIGNAL QUALITY STATS DTO
public class SignalQualityStatsDto
{
    public double AverageSignalStrength { get; set; }
    public double MinSignalStrength { get; set; }
    public double MaxSignalStrength { get; set; }
    public double MedianSignalStrength { get; set; }

    // Quality distribution based on CMTS criteria
    public int ExcellentCount { get; set; }    // -44 to -70 dBm
    public int GoodCount { get; set; }         // -71 to -85 dBm  
    public int FairCount { get; set; }         // -86 to -99 dBm
    public int PoorCount { get; set; }         // -100 dBm and below

    // Percentage distributions
    public double ExcellentPercent { get; set; }
    public double GoodPercent { get; set; }
    public double FairPercent { get; set; }
    public double PoorPercent { get; set; }

    // CMTS compliance
    public int CMTSCompliantCount { get; set; } // Signal strength >= -89 dBm
    public double CMTSComplianceRate { get; set; }
}

// 7. SIGNAL QUALITY BY TECHNOLOGY DTO
public class SignalQualityByTechnologyDto
{
    public string Technology { get; set; } = string.Empty;
    public int TowerCount { get; set; }
    public double AverageSignalStrength { get; set; }
    public string QualityRating { get; set; } = string.Empty;
    public double CMTSComplianceRate { get; set; }
}

// 8. SIGNAL QUALITY BY PROVIDER DTO  
public class SignalQualityByProviderDto
{
    public Guid ProviderId { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public int TowerCount { get; set; }
    public double AverageSignalStrength { get; set; }
    public string QualityRating { get; set; } = string.Empty;
    public double CMTSComplianceRate { get; set; }
    public List<string> TechnologiesOffered { get; set; } = [];
}

// 9. SIGNAL QUALITY ZONE DTO
public class SignalQualityZoneDto
{
    public string ZoneName { get; set; } = string.Empty;
    public string QualityLevel { get; set; } = string.Empty; // Excellent, Good, Fair, Poor
    public List<string> MunicipalitiesInZone { get; set; } = [];
    public int TowerCount { get; set; }
    public double AverageSignalStrength { get; set; }
    public string PrimaryIssue { get; set; } = string.Empty;
    public List<string> RecommendedActions { get; set; } = [];
}

// 10. PROVIDER COVERAGE DTO
public class ProviderCoverageDto
{
    public Guid ProviderId { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public string ProviderColor { get; set; } = string.Empty;
    public int TotalTowers { get; set; }

    // Coverage metrics
    public int MunicipalitiesServed { get; set; }
    public double EstimatedCoverageAreaKm2 { get; set; }
    public double MarketShareByTowers { get; set; }
    public double MarketShareByCoverage { get; set; }

    // Technology deployment
    public TechnologyBreakdownDto? TechnologyDeployment { get; set; }
    public List<string> PrimaryTechnologies { get; set; } = [];

    // Network quality
    public double AverageSignalStrength { get; set; }
    public string NetworkQualityRating { get; set; } = string.Empty;
    public double CMTSComplianceRate { get; set; }

    // Geographic presence
    public List<MunicipalityProviderPresenceDto> MunicipalityPresence { get; set; } = [];

    // Competitive analysis
    public List<string> CompetitiveAdvantages { get; set; } = [];
    public List<string> CoverageGaps { get; set; } = [];
    public string DeploymentStrategy { get; set; } = string.Empty;
}

// 11. MUNICIPALITY PROVIDER PRESENCE DTO
public class MunicipalityProviderPresenceDto
{
    public Guid? MunicipalityId { get; set; }
    public string MunicipalityName { get; set; } = string.Empty;
    public int TowerCount { get; set; }
    public double EstimatedCoveragePercent { get; set; }
    public List<string> TechnologiesOffered { get; set; } = [];
    public double AverageSignalStrength { get; set; }
    public bool IsDominantProvider { get; set; }
    public string MarketPosition { get; set; } = string.Empty; // Leader, Competitor, Minor
}

// 12. COVERAGE GAP DTO
public class CoverageGapDto
{
    public string GapType { get; set; } = string.Empty; // No-Coverage, Poor-Signal, Technology-Gap, Single-Provider
    public string Severity { get; set; } = string.Empty; // Critical, High, Medium, Low
    public List<string> AffectedMunicipalities { get; set; } = [];
    public string Description { get; set; } = string.Empty;
    public double EstimatedAffectedPopulation { get; set; }
    public double EstimatedAffectedAreaKm2 { get; set; }
    public List<string> RecommendedSolutions { get; set; } = [];
    public string Priority { get; set; } = string.Empty; // Immediate, Short-term, Medium-term, Long-term
    public double EstimatedInvestmentRequired { get; set; }
}
