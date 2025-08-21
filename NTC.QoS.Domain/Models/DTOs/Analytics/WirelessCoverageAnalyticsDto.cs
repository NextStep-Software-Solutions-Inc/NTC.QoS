namespace NTC.QoS.Domain.Models.DTOs.Analytics;

// 1. MAIN RESPONSE DTO
public class WirelessCoverageAnalyticsDto
{
    public int TotalCellTowers { get; set; }
    public Dictionary<string, int> TechnologyDistribution { get; set; } = new();
    public List<MunicipalityWirelessCoverageDto> CoverageByMunicipality { get; set; } = new();
    public SignalQualityAnalysisDto? SignalQualityAnalysis { get; set; }
    public List<ProviderCoverageDto> ProviderCoverage { get; set; } = new();

    // Additional summary metrics
    public WirelessCoverageSummaryDto? Summary { get; set; }
    public List<CoverageGapDto> CoverageGaps { get; set; } = new();
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
    public List<string> AvailableTechnologies { get; set; } = new();
    public List<string> ServiceProviders { get; set; } = new();

    // Signal quality metrics
    public double AverageSignalStrength { get; set; }
    public string SignalQualityRating { get; set; } = string.Empty; // Excellent, Good, Fair, Poor 

    // Technology breakdown
    public TechnologyBreakdownDto? TechnologyBreakdown { get; set; }

    // Coverage assessment
    public string CoverageStatus { get; set; } // Well-Covered, Partially-Covered, Poor-Coverage, No-Coverage
    public List<string> CoverageIssues { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();

    // Geographic info
    public CoordinateDto? EstimatedCenter { get; set; }
    public double EstimatedAreaKm2 { get; set; }
}

// 4. TECHNOLOGY BREAKDOWN DTO
public class TechnologyBreakdownDto
{
    public int Count2G { get; set; }
    public int Count3G { get; set; }
    public int Count4G { get; set; }
    public int Count5G { get; set; }
    public int CountLTE { get; set; }
    public int CountOther { get; set; }

    // Calculated properties
    public string PrimaryTechnology { get; set; }
    public bool HasModernCoverage { get; set; } // 4G/5G/LTE present
    public double ModernCoverageRatio { get; set; } // % of towers with 4G/5G/LTE
}

// 5. SIGNAL QUALITY ANALYSIS DTO
public class SignalQualityAnalysisDto
{
    public SignalQualityStatsDto? OverallStats { get; set; }
    public List<SignalQualityByTechnologyDto> QualityByTechnology { get; set; } = new();
    public List<SignalQualityByProviderDto> QualityByProvider { get; set; } = new();
    public List<SignalQualityZoneDto> QualityZones { get; set; } = new();
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
    public string Technology { get; set; }
    public int TowerCount { get; set; }
    public double AverageSignalStrength { get; set; }
    public string QualityRating { get; set; }
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
    public List<string> TechnologiesOffered { get; set; } = new();
}

// 9. SIGNAL QUALITY ZONE DTO
public class SignalQualityZoneDto
{
    public string ZoneName { get; set; }
    public string QualityLevel { get; set; } // Excellent, Good, Fair, Poor
    public List<string> MunicipalitiesInZone { get; set; } = new();
    public int TowerCount { get; set; }
    public double AverageSignalStrength { get; set; }
    public string PrimaryIssue { get; set; }
    public List<string> RecommendedActions { get; set; } = new();
}

// 10. PROVIDER COVERAGE DTO
public class ProviderCoverageDto
{
    public Guid ProviderId { get; set; }
    public string ProviderName { get; set; }
    public string ProviderColor { get; set; }
    public int TotalTowers { get; set; }

    // Coverage metrics
    public int MunicipalitiesServed { get; set; }
    public double EstimatedCoverageAreaKm2 { get; set; }
    public double MarketShareByTowers { get; set; }
    public double MarketShareByCoverage { get; set; }

    // Technology deployment
    public TechnologyBreakdownDto TechnologyDeployment { get; set; }
    public List<string> PrimaryTechnologies { get; set; } = new();

    // Network quality
    public double AverageSignalStrength { get; set; }
    public string NetworkQualityRating { get; set; }
    public double CMTSComplianceRate { get; set; }

    // Geographic presence
    public List<MunicipalityProviderPresenceDto> MunicipalityPresence { get; set; } = new();

    // Competitive analysis
    public List<string> CompetitiveAdvantages { get; set; } = new();
    public List<string> CoverageGaps { get; set; } = new();
    public string DeploymentStrategy { get; set; }
}

// 11. MUNICIPALITY PROVIDER PRESENCE DTO
public class MunicipalityProviderPresenceDto
{
    public Guid? MunicipalityId { get; set; }
    public string MunicipalityName { get; set; }
    public int TowerCount { get; set; }
    public double EstimatedCoveragePercent { get; set; }
    public List<string> TechnologiesOffered { get; set; } = new();
    public double AverageSignalStrength { get; set; }
    public bool IsDominantProvider { get; set; }
    public string MarketPosition { get; set; } // Leader, Competitor, Minor
}

// 12. COVERAGE GAP DTO
public class CoverageGapDto
{
    public string GapType { get; set; } // No-Coverage, Poor-Signal, Technology-Gap, Single-Provider
    public string Severity { get; set; } // Critical, High, Medium, Low
    public List<string> AffectedMunicipalities { get; set; } = new();
    public string Description { get; set; }
    public double EstimatedAffectedPopulation { get; set; }
    public double EstimatedAffectedAreaKm2 { get; set; }
    public List<string> RecommendedSolutions { get; set; } = new();
    public string Priority { get; set; } // Immediate, Short-term, Medium-term, Long-term
    public double EstimatedInvestmentRequired { get; set; }
}
