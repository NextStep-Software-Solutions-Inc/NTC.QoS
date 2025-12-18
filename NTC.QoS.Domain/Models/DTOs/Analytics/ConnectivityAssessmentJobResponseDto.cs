using NTC.QoS.Domain.Enums;

namespace NTC.QoS.Domain.Models.DTOs.Analytics;

/// <summary>
/// Response DTO for connectivity assessment job creation
/// </summary>
public class ConnectivityAssessmentJobResponseDto
{
    /// <summary>
    /// Unique identifier for the job (Hangfire Job ID)
    /// </summary>
    public string JobId { get; set; } = string.Empty;

    /// <summary>
    /// Current status of the job
    /// </summary>
    public ConnectivityAssessmentJobStatusEnum Status { get; set; }

    /// <summary>
    /// Status message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Estimated processing duration in minutes
    /// </summary>
    public int EstimatedDurationMinutes { get; set; }

    /// <summary>
    /// Estimated total number of locations that will be processed
    /// This is an estimate based on hierarchical settings and may differ from actual count
    /// </summary>
    public int EstimatedTotalLocations { get; set; }

    /// <summary>
    /// URL to check job status
    /// </summary>
    public string StatusUrl => $"/api/analytics/connectivity-assessment/job/{JobId}/status";
}