using Microsoft.Extensions.Logging;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Models.DTOs.Analytics;

namespace NTC.QoS.Infrastructure.Services.Connectivity;

/// <summary>
/// Implementation of CMTS (Commission on Mapping and Technology Standards) scoring service.
/// Pure business logic with no external dependencies - fully testable and reusable.
/// 
/// IMPORTANT: All methods are stateless and deterministic:
/// - Same inputs always produce same outputs
/// - No database access
/// - No external service calls
/// - Thread-safe
/// 
/// This ensures scoring consistency across:
/// - Connectivity Assessments (Handler & Background Job)
/// - Wired Broadband Reports
/// - Wireless Broadband Reports
/// - Connectivity Gap Reports
/// </summary>
public class CMTSScoringService : ICMTSScoringService
{
    private readonly ILogger<CMTSScoringService> _logger;

    public CMTSScoringService(ILogger<CMTSScoringService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public CMTSScoresDto CalculateAllScores(
        InfrastructureStatsDto infrastructure,
        SpeedStatsDto? speedStats,
        double areaKm2,
        int? population,
        int? householdCount)
    {
        _logger.LogDebug("Calculating CMTS scores for area {AreaKm2}km², population {Population}", 
            areaKm2, population);

        var scores = new CMTSScoresDto();

        // Calculate population density context once
        var populationContext = CalculatePopulationContext(population, areaKm2);

        // Calculate each CMTS criterion using dedicated methods
        scores.FiberCoverage = CalculateFiberCoverageScore(infrastructure, areaKm2, population, populationContext);
        scores.InternetAccess = CalculateInternetAccessScore(speedStats);
        scores.TechnologyType = CalculateTechnologyTypeScore(infrastructure);
        scores.QualityOfService = CalculateQualityOfServiceScore(speedStats, infrastructure);
        scores.CoverageAvailability = CalculateCoverageAvailabilityScore(infrastructure, areaKm2, population, populationContext);

        var totalScore = scores.FiberCoverage + scores.InternetAccess + scores.TechnologyType + 
                        scores.QualityOfService + scores.CoverageAvailability;

        _logger.LogDebug("CMTS scores calculated: Total={TotalScore}, Fiber={Fiber}, Internet={Internet}, " +
                        "Tech={Tech}, QoS={QoS}, Coverage={Coverage}",
            totalScore, scores.FiberCoverage, scores.InternetAccess, scores.TechnologyType,
            scores.QualityOfService, scores.CoverageAvailability);

        return scores;
    }

    /// <inheritdoc />
    public PopulationContextDto CalculatePopulationContext(int? population, double areaKm2)
    {
        var populationDensity = population.HasValue && areaKm2 > 0 ? population.Value / areaKm2 : 0;
        var isHighDensityArea = populationDensity > 500; // >500 people per km² = high density
        var isUrbanArea = populationDensity > 1000; // >1000 people per km² = urban

        return new PopulationContextDto
        {
            PopulationDensity = populationDensity,
            IsUrbanArea = isUrbanArea,
            IsHighDensityArea = isHighDensityArea
        };
    }

    /// <inheritdoc />
    public int CalculateFiberCoverageScore(
        InfrastructureStatsDto infrastructure,
        double areaKm2,
        int? population,
        PopulationContextDto context)
    {
        // Unserved: No fiber infrastructure at all
        if (infrastructure.FiberNAPCount == 0)
        {
            return 1; // Unserved - No installed or present FTTH facilities
        }

        // Get providers with fiber presence
        var fiberProviders = infrastructure.ProviderBreakdown
            .Where(p => p.FiberNAPCount > 0)
            .ToList();

        if (fiberProviders.Count == 0)
        {
            return 1; // No fiber providers
        }

        // Identify major telco providers (Globe, Smart, DITO)
        var majorTelcoProviders = fiberProviders
            .Where(p => IsMajorTelcoProvider(p.ProviderName))
            .ToList();

        var localVasProviders = fiberProviders
            .Where(p => !IsMajorTelcoProvider(p.ProviderName))
            .ToList();

        // Count distinct major telcos with fiber presence
        var majorTelcoCount = majorTelcoProviders.Count;

        // SERVED (3 points): All major service providers have fiber infrastructure
        // Interpretation: At least 2-3 major telcos present (Globe, Smart, DITO)
        if (majorTelcoCount >= 2)
        {
            return 3; // Served - Fiber facilities for all/most major service providers present
        }

        // UNDERSERVED (2 points): Few major telcos OR only local/VAS providers
        // Case 1: Only 1 major telco with fiber
        if (majorTelcoCount == 1)
        {
            return 2; // Underserved - Few major telcos with fiber
        }

        // Case 2: No major telcos, but local/VAS providers have fiber
        if (majorTelcoCount == 0 && localVasProviders.Any())
        {
            return 2; // Underserved - Only local or VAS providers with fiber
        }

        // Fallback: Has fiber but doesn't fit clear categories
        return 1; // Unserved - Minimal fiber presence
    }

    /// <inheritdoc />
    public int CalculateInternetAccessScore(SpeedStatsDto? speedStats)
    {
        // Unserved: No internet access or only minimal access
        if (speedStats == null || speedStats.SpeedTestCount == 0)
        {
            return 1; // No data - no broadband infrastructure
        }

        // Insufficient data to make reliable assessment
        if (speedStats.SpeedTestCount < 3)
        {
            return 1; // Too few tests to assess reliability
        }

        var avgDownloadSpeed = speedStats.AverageDownloadSpeedMbps ?? 0;
        var medianDownloadSpeed = speedStats.MedianDownloadSpeedMbps ?? 0;

        // Calculate speed consistency (variability indicator)
        // Large difference between average and median indicates unreliability
        var speedVariability = avgDownloadSpeed > 0
            ? Math.Abs((avgDownloadSpeed - medianDownloadSpeed) / avgDownloadSpeed)
            : 0;
        var isUnreliable = speedVariability > 0.3m; // >30% variance = unreliable

        // SERVED (3 points): Stable internet meeting/exceeding national broadband standards
        // Criteria: ≥25 Mbps AND reliable (low variability)
        if (avgDownloadSpeed >= 25 && !isUnreliable)
        {
            return 3; // Served - Stable internet meeting or exceeding standards (25+ Mbps)
        }

        // UNDERSERVED (2 points): Internet available but slow, unreliable, or below standards
        // Case 1: Speed ≥25 Mbps but unreliable
        if (avgDownloadSpeed >= 25 && isUnreliable)
        {
            return 2; // Underserved - Available but unreliable
        }

        // Case 2: Speed between 10-25 Mbps (below standards but functional)
        if (avgDownloadSpeed >= 10 && avgDownloadSpeed < 25)
        {
            return 2; // Underserved - Available but below national broadband standards
        }

        // UNSERVED (1 point): No internet access or only minimal access
        // Speed <10 Mbps = minimal/no practical broadband access
        return 1; // Unserved - No or minimal internet access (<10 Mbps)
    }

    /// <inheritdoc />
    public int CalculateTechnologyTypeScore(InfrastructureStatsDto infrastructure)
    {
        var hasFiberInfra = infrastructure.FiberNAPCount > 0;
        var hasCellTowerInfra = infrastructure.CellTowerCount > 0;
        var hasWiFiInfra = infrastructure.WiFiHotspotCount > 0;

        // SERVED (3 points): Fiber optic, LTE/5G, or high-speed DSL infrastructure in place and active

        // Case 1: Fiber optic infrastructure present (meets rubric directly)
        if (hasFiberInfra)
        {
            return 3; // Served - Fiber optic infrastructure present
        }

        // Case 2: Modern wireless technology (LTE/5G) active
        // Enhanced technology scoring using actual generation data
        if (infrastructure.TechnologyBandBreakdown != null && infrastructure.TechnologyBandBreakdown.TotalTechnologyCount > 0)
        {
            var tech = infrastructure.TechnologyBandBreakdown;

            // 5G infrastructure present and active
            if (tech.Count5G > 0)
            {
                return 3; // Served - 5G infrastructure in place and active
            }

            // 4G/LTE infrastructure present and dominant (not just occasional)
            if (tech.Count4G > 0 && !tech.IsLegacyDominated)
            {
                return 3; // Served - LTE/4G infrastructure in place and active
            }

            // UNDERSERVED (2 points): Legacy technologies (2G/3G, dial-up, outdated satellite)

            // Has modern tech but legacy dominates
            if (tech.HasModernTechnology && tech.IsLegacyDominated)
            {
                return 2; // Underserved - Mixed modern and legacy, but legacy dominates
            }

            // Only legacy technologies (2G/3G)
            if (tech.Count3G > 0 || tech.Count2G > 0)
            {
                return 2; // Underserved - Only legacy technologies (2G/3G) available
            }

            // No identifiable technology generation
            return 1; // Unserved - No identifiable broadband technology
        }

        // FALLBACK: No technology breakdown data available, use infrastructure presence

        // Has some modern infrastructure (cell towers or WiFi)
        if (hasCellTowerInfra || hasWiFiInfra)
        {
            // Multiple infrastructure types suggest better service
            var infraTypes = (hasCellTowerInfra ? 1 : 0) + (hasWiFiInfra ? 1 : 0);

            if (infraTypes >= 2)
            {
                return 3; // Served - Multiple infrastructure types present
            }

            return 2; // Underserved - Limited infrastructure diversity
        }

        // UNSERVED (1 point): No installed broadband technology
        return 1; // Unserved - No broadband infrastructure detected
    }

    /// <inheritdoc />
    public int CalculateQualityOfServiceScore(
        SpeedStatsDto? speedStats,
        InfrastructureStatsDto infrastructure)
    {
        // Insufficient data to assess quality
        if (speedStats == null || speedStats.SpeedTestCount < 3)
        {
            return 1; // Unserved - Cannot assess quality without sufficient data
        }

        var latency = speedStats.AverageLatencyMs ?? decimal.MaxValue;
        var signalStrength = speedStats.AverageSignalStrength;
        var hasConsistentSpeeds = speedStats.MeetsMinimumSpeedRequirement;

        // Map signal strength (1-5 scale) to dBm ranges per rubric
        // 5 = Good (-44 to -70 dBm), 4 = Average (-70 to -85 dBm), 
        // 3 = Weak (-85 to -90 dBm), 2 = Very Weak (-90 to -100 dBm), 
        // 1 = No Coverage (-100+ dBm)

        // UNSERVED (1 point): Poor to nonexistent quality
        // Criteria: Latency >100ms OR signal ≤ 2 (worse than -90 dBm) OR very poor speeds
        if (latency > 100 || signalStrength <= 2.0 || speedStats.AverageDownloadSpeedMbps < 5)
        {
            return 1; // Unserved - Poor quality, unusable for essential tasks
        }

        // SERVED (3 points): High quality service
        // Criteria: Latency <50ms AND good signal (≥4 = -44 to -89 dBm)
        var hasLowLatency = latency < 50;
        var hasGoodSignal = signalStrength >= 4.0; // Average to Good signal (-44 to -89 dBm)

        if (hasLowLatency && hasGoodSignal && hasConsistentSpeeds)
        {
            return 3; // Served - High quality, suitable for online learning, remote work, streaming
        }

        // Additional quality check for excellent 5G performance
        if (infrastructure.TechnologyBandBreakdown?.Count5G > 0 &&
            latency <= 20 &&
            speedStats.AverageDownloadSpeedMbps >= 100 &&
            signalStrength >= 4.5)
        {
            return 3; // Served - Exceptional 5G quality
        }

        // UNDERSERVED (2 points): Fair quality with issues
        // Criteria: Latency 50-100ms OR moderate signal (-90 to -99 dBm) OR inconsistent speeds
        // This catches the "middle ground" cases
        var hasModerateLatency = latency >= 50 && latency <= 100;
        var hasModerateSignal = signalStrength >= 2.0 && signalStrength < 4.0; // -90 to -99 dBm range

        if (hasModerateLatency || hasModerateSignal || !hasConsistentSpeeds)
        {
            return 2; // Underserved - Fair quality but suffers from congestion or poor signal
        }

        // Edge case: Has good latency OR good signal but not both
        if (hasLowLatency || hasGoodSignal)
        {
            return 2; // Underserved - Some quality indicators are good but not comprehensive
        }

        // Fallback
        return 1; // Unserved - Quality issues detected
    }

    /// <inheritdoc />
    public int CalculateCoverageAvailabilityScore(
        InfrastructureStatsDto infrastructure,
        double areaKm2,
        int? population,
        PopulationContextDto context)
    {
        if (infrastructure.TotalAssets == 0)
        {
            return 1; // Unserved - No coverage (no infrastructure)
        }

        double coverageScore;

        if (population.HasValue && population > 0)
        {
            // Population-based coverage calculation (aligns with CMTS rubric)
            var assetsPerCapita = (double)infrastructure.TotalAssets / population.Value;
            var providersPerThousand = (infrastructure.ServiceProviderCount * 1000.0) / population.Value;

            // Calculate estimated coverage percentage based on infrastructure density
            double estimatedCoveragePercent;

            if (context.IsUrbanArea)
            {
                // Urban: High infrastructure density expected  
                estimatedCoveragePercent = Math.Min(100, (assetsPerCapita * 10000) + (providersPerThousand * 20));
            }
            else if (context.IsHighDensityArea)
            {
                // Semi-urban: Moderate infrastructure density
                estimatedCoveragePercent = Math.Min(100, (assetsPerCapita * 15000) + (providersPerThousand * 25));
            }
            else
            {
                // Rural: Lower per-capita requirements but geographic coverage matters
                var geographicDensity = areaKm2 > 0 ? infrastructure.TotalAssets / areaKm2 : 0;
                var populationCoverage = Math.Min(100, (assetsPerCapita * 20000) + (providersPerThousand * 30));
                var geographicCoverage = Math.Min(100, geographicDensity * 100);
                estimatedCoveragePercent = Math.Max(populationCoverage, geographicCoverage);
            }

            // Apply CMTS rubric thresholds strictly based on coverage percentage
            // SERVED (3 points): 100% of households/population have access
            if (estimatedCoveragePercent >= 100)
            {
                coverageScore = 3; // Served - 100% coverage per CMTS rubric
            }
            // UNDERSERVED (2 points): 40% to 99% of the population has access
            else if (estimatedCoveragePercent >= 40)
            {
                coverageScore = 2; // Underserved - 40-99% coverage per CMTS rubric
            }
            // UNSERVED (1 point): Less than 40% have access; infrastructure is sparse
            else
            {
                coverageScore = 1; // Unserved - <40% coverage per CMTS rubric
            }
        }
        else
        {
            // Fallback to geographic density when population data unavailable
            var assetDensity = areaKm2 > 0 ? infrastructure.TotalAssets / areaKm2 : 0;
            var hasMultipleProviders = infrastructure.ServiceProviderCount > 1;
            var hasModernTech = infrastructure.TechnologyBandBreakdown?.HasModernTechnology == true;

            // Estimate coverage category based on infrastructure presence
            // High density with good provider diversity suggests comprehensive coverage (100%)
            if (hasMultipleProviders && hasModernTech && assetDensity >= 1.0)
            {
                coverageScore = 3; // Served - Comprehensive coverage estimated
            }
            // Moderate infrastructure suggests partial coverage (40-99%)
            else if (hasMultipleProviders || hasModernTech || assetDensity >= 0.5)
            {
                coverageScore = 2; // Underserved - Partial coverage estimated
            }
            // Sparse infrastructure suggests minimal coverage (<40%)
            else
            {
                coverageScore = 1; // Unserved - Limited/sparse coverage estimated
            }
        }

        return (int)Math.Round(coverageScore);
    }

    /// <inheritdoc />
    public ConnectivityStatusEnum DetermineConnectivityStatus(int totalScore)
    {
        return totalScore switch
        {
            >= 11 => ConnectivityStatusEnum.Served,    // 11-15 points
            >= 6 => ConnectivityStatusEnum.Underserved, // 6-10 points  
            _ => ConnectivityStatusEnum.Unserved        // ≤5 points
        };
    }

    /// <inheritdoc />
    public bool DetermineReliableConnectivity(
        InfrastructureStatsDto infrastructure,
        SpeedStatsDto? speedStats)
    {
        var hasInfrastructure = infrastructure.TotalAssets > 0;
        var hasMultipleProviders = infrastructure.ServiceProviderCount > 1;
        var hasGoodSpeeds = speedStats?.MeetsMinimumSpeedRequirement == true;

        return hasInfrastructure && (hasMultipleProviders || hasGoodSpeeds);
    }

    /// <inheritdoc />
    public bool IsMajorTelcoProvider(string providerName)
    {
        if (string.IsNullOrWhiteSpace(providerName))
            return false;

        // Major telco providers in the Philippines
        var majorTelcos = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Globe", "Smart", "DITO",
            // Include common variations and subsidiaries
            "Globe Telecom", "Smart Communications", "DITO Telecommunity",
            "PLDT", "Globe Broadband", "Smart Broadband"
        };

        return majorTelcos.Any(telco => providerName.Contains(telco, StringComparison.OrdinalIgnoreCase));
    }
}
