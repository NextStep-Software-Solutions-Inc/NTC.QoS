using NTC.QoS.Domain.Enums;

namespace NTC.QoS.Domain.Interfaces;

/// <summary>
/// Interface for background connectivity assessment job processing
/// </summary>
public interface IProcessConnectivityAssessmentJob
{
    /// <summary>
    /// Execute connectivity assessment generation as a background job
    /// </summary>
    /// <param name="jobRequest">Job request containing assessment parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task ExecuteAsync(ConnectivityAssessmentJobRequest jobRequest, CancellationToken cancellationToken);
}

/// <summary>
/// Request model for background connectivity assessment job
/// </summary>
public class ConnectivityAssessmentJobRequest
{
    public string JobId { get; set; } = string.Empty;
    public List<Guid> LocationIds { get; set; } = [];
    public LocationTypeEnum LocationType { get; set; }
    public bool ForceRecalculate { get; set; } = false;
    public bool GenerateHierarchicalAssessments { get; set; } = false;
    public int HierarchicalDepth { get; set; } = 3;
    public string UserId { get; set; } = string.Empty;
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Job status tracking
/// </summary>
public class ConnectivityAssessmentJobStatus
{
    public string JobId { get; set; } = string.Empty;
    public ConnectivityAssessmentJobStatusEnum Status { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime LastUpdated { get; set; }
    public int? TotalLocations { get; set; }
    public int? ProcessedLocations { get; set; }
    public double? ProgressPercentage => ProcessedLocations.HasValue && TotalLocations.HasValue && TotalLocations > 0
        ? Math.Round((double)ProcessedLocations.Value / TotalLocations.Value * 100, 2)
        : null;
}