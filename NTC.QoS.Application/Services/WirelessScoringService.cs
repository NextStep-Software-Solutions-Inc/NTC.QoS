using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Models.DTOs.Analytics;
using NTC.QoS.Domain.Models.DTOs.Reports;

namespace NTC.QoS.Application.Services;

/// <summary>
/// Shared service for wireless connectivity scoring based on CMTS criteria.
/// Ensures consistent scoring methodology across reports and assessments.
/// </summary>
public interface IWirelessScoringService
{
    /// <summary>
    /// Calculate Internet Access Score (1-3 points) - CMTS Criterion 2
    /// Based on speedtest data: ≥25 Mbps = 3, Available but &lt;25 Mbps = 2, Minimal/None = 1
    /// </summary>
    int CalculateInternetAccessScore(SpeedStatsDto? speedStats);

    /// <summary>
    /// Calculate Technology Type Score for wireless services (1-3 points) - CMTS Criterion 3
    /// Based on highest available generation: 5G/4G = 3, 3G/2G = 2, None = 1
    /// </summary>
    int CalculateTechnologyTypeScore(TechnologyBandBreakdownDto? techBreakdown);

    /// <summary>
    /// Determine service status from wireless connectivity scores
    /// 6 points = Served, 4-5 = Underserved, 2-3 = Unserved
    /// </summary>
    ServiceStatusEnum DetermineServiceStatus(int totalScore);

    /// <summary>
    /// Get highest technology generation from technology breakdown
    /// Returns: "5G", "4G/LTE", "3G", "2G", or "None"
    /// </summary>
    string GetHighestTechnology(TechnologyBandBreakdownDto? techBreakdown);
}

public class WirelessScoringService : IWirelessScoringService
{
    private const double MinimumSpeedThreshold = 25.0; // Mbps for CMTS "Served" criteria

    /// <summary>
    /// Calculate Internet Access Score (1-3 points) - CMTS Criterion 2
    /// Aligned with GenerateConnectivityAssessmentHandler.CalculateInternetAccessScore
    /// </summary>
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

        var avgDownloadSpeed = (double)(speedStats.AverageDownloadSpeedMbps ?? 0);
        var medianDownloadSpeed = (double)(speedStats.MedianDownloadSpeedMbps ?? 0);

        // Check for unreliable connection (high variance between average and median)
        var speedVariance = Math.Abs(avgDownloadSpeed - medianDownloadSpeed);
        var isUnreliable = speedVariance > (medianDownloadSpeed * 0.5); // >50% variance

        // SERVED (3 points): Speeds ≥25 Mbps and reliable
        if (avgDownloadSpeed >= MinimumSpeedThreshold && !isUnreliable)
        {
            return 3; // Served - Meets national broadband standards
        }

        // UNDERSERVED (2 points): Internet available but slow, unreliable, or <25 Mbps
        if (avgDownloadSpeed >= MinimumSpeedThreshold && isUnreliable)
        {
            return 2; // Underserved - Meets speed but unreliable
        }

        // Internet is available but below 25 Mbps threshold
        if (avgDownloadSpeed >= 10 && avgDownloadSpeed < MinimumSpeedThreshold)
        {
            return 2; // Underserved - Available but slow
        }

        // UNSERVED (1 point): No internet access or minimal (<10 Mbps)
        return 1; // Unserved - Minimal or no broadband access
    }

    /// <summary>
    /// Calculate Technology Type Score for wireless services (1-3 points) - CMTS Criterion 3
    /// Simplified for wireless-only assessment (no fiber evaluation)
    /// Aligned with GenerateConnectivityAssessmentHandler.CalculateTechnologyTypeScore
    /// </summary>
    public int CalculateTechnologyTypeScore(TechnologyBandBreakdownDto? techBreakdown)
    {
        if (techBreakdown == null || techBreakdown.TotalTechnologyCount == 0)
        {
            return 1; // Unserved - No broadband technology
        }

        // SERVED (3 points): 5G or modern 4G/LTE infrastructure
        if (techBreakdown.Count5G > 0)
        {
            return 3; // Served - 5G available
        }

        // Modern 4G/LTE without legacy domination
        if (techBreakdown.Count4G > 0 && !techBreakdown.IsLegacyDominated)
        {
            return 3; // Served - Modern 4G/LTE infrastructure
        }

        // UNDERSERVED (2 points): Legacy technologies or mixed modern/legacy
        // Has modern tech but legacy dominated
        if (techBreakdown.HasModernTechnology && techBreakdown.IsLegacyDominated)
        {
            return 2; // Underserved - Mixed modern/legacy, legacy dominant
        }

        // Only legacy technologies (3G/2G)
        if (techBreakdown.Count3G > 0 || techBreakdown.Count2G > 0)
        {
            return 2; // Underserved - Only legacy technology
        }

        // UNSERVED (1 point): No significant broadband technology
        return 1; // Unserved - No broadband technology
    }

    /// <summary>
    /// Determine service status from wireless connectivity scores
    /// Based on 2 criteria only: Internet Access + Technology Type (max 6 points)
    /// </summary>
    public ServiceStatusEnum DetermineServiceStatus(int totalScore)
    {
        return totalScore switch
        {
            6 => ServiceStatusEnum.Served,        // Both criteria at max
            >= 4 => ServiceStatusEnum.Underserved, // At least one criterion good
            _ => ServiceStatusEnum.Unserved        // Both criteria poor
        };
    }

    /// <summary>
    /// Get highest technology generation from technology breakdown
    /// </summary>
    public string GetHighestTechnology(TechnologyBandBreakdownDto? techBreakdown)
    {
        if (techBreakdown == null || techBreakdown.TotalTechnologyCount == 0)
        {
            return "None";
        }

        if (techBreakdown.Count5G > 0) return "5G";
        if (techBreakdown.Count4G > 0) return "4G/LTE";
        if (techBreakdown.Count3G > 0) return "3G";
        if (techBreakdown.Count2G > 0) return "2G";

        return "None";
    }
}
