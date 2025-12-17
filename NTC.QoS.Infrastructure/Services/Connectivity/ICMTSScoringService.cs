using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Models.DTOs.Analytics;

namespace NTC.QoS.Infrastructure.Services.Connectivity;

/// <summary>
/// Service for calculating CMTS (Commission on Mapping and Technology Standards) connectivity scores.
/// Pure business logic with no external dependencies - fully testable and reusable across:
/// - Connectivity Assessments
/// - Wired Broadband Reports
/// - Wireless Broadband Reports
/// - Connectivity Gap Reports
/// 
/// CMTS Scoring Rubric (0-15 points total):
/// 1. Fiber Coverage (0-3): Service provider diversity and fiber infrastructure presence
/// 2. Internet Access (0-3): Internet availability, speed, and reliability
/// 3. Technology Type (0-3): Broadband technology infrastructure presence and quality
/// 4. Quality of Service (0-3): Latency, signal strength, and service reliability
/// 5. Coverage Availability (0-3): Population/household coverage percentage
/// 
/// Final Status: Served (11-15), Underserved (6-10), Unserved (≤5)
/// </summary>
public interface ICMTSScoringService
{
    /// <summary>
    /// Calculate all CMTS scores in one call
    /// </summary>
    CMTSScoresDto CalculateAllScores(
        InfrastructureStatsDto infrastructure,
        SpeedStatsDto? speedStats,
        double areaKm2,
        int? population,
        int? householdCount);

    /// <summary>
    /// Calculate Fiber Coverage Score (0-3 points) - CMTS Criterion 1
    /// </summary>
    int CalculateFiberCoverageScore(
        InfrastructureStatsDto infrastructure,
        double areaKm2,
        int? population,
        PopulationContextDto context);

    /// <summary>
    /// Calculate Internet Access Score (0-3 points) - CMTS Criterion 2
    /// </summary>
    int CalculateInternetAccessScore(SpeedStatsDto? speedStats);

    /// <summary>
    /// Calculate Technology Type Score (0-3 points) - CMTS Criterion 3
    /// </summary>
    int CalculateTechnologyTypeScore(InfrastructureStatsDto infrastructure);

    /// <summary>
    /// Calculate Quality of Service Score (0-3 points) - CMTS Criterion 4
    /// </summary>
    int CalculateQualityOfServiceScore(
        SpeedStatsDto? speedStats,
        InfrastructureStatsDto infrastructure);

    /// <summary>
    /// Calculate Coverage Availability Score (0-3 points) - CMTS Criterion 5
    /// </summary>
    int CalculateCoverageAvailabilityScore(
        InfrastructureStatsDto infrastructure,
        double areaKm2,
        int? population,
        PopulationContextDto context);

    /// <summary>
    /// Calculate population density context for scoring
    /// </summary>
    PopulationContextDto CalculatePopulationContext(int? population, double areaKm2);

    /// <summary>
    /// Determine overall connectivity status from total score
    /// </summary>
    ConnectivityStatusEnum DetermineConnectivityStatus(int totalScore);

    /// <summary>
    /// Determine if connectivity is reliable based on infrastructure and speed data
    /// </summary>
    bool DetermineReliableConnectivity(
        InfrastructureStatsDto infrastructure,
        SpeedStatsDto? speedStats);

    /// <summary>
    /// Identifies if a provider is a major telecommunications company in the Philippines
    /// Major telcos: Globe, Smart, DITO
    /// </summary>
    bool IsMajorTelcoProvider(string providerName);
}

/// <summary>
/// DTO for population density context calculations
/// </summary>
public class PopulationContextDto
{
    public double PopulationDensity { get; set; }
    public bool IsUrbanArea { get; set; }
    public bool IsHighDensityArea { get; set; }
}
