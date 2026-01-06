namespace NTC.QoS.API.Models;

/// <summary>
/// Response model for seed job status
/// </summary>
public class SeedJobStatusResponse
{
    /// <summary>
    /// The Hangfire job ID
    /// </summary>
    public string JobId { get; set; } = string.Empty;

    /// <summary>
    /// Current state of the job (Enqueued, Processing, Succeeded, Failed, Deleted, Scheduled)
    /// </summary>
    public string State { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable reason for the current state
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// When the job was created
    /// </summary>
    public DateTime? CreatedAt { get; set; }

    /// <summary>
    /// When the job started processing
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// When the job finished (succeeded or failed)
    /// </summary>
    public DateTime? FinishedAt { get; set; }

    /// <summary>
    /// Exception details if the job failed
    /// </summary>
    public string? ExceptionMessage { get; set; }

    /// <summary>
    /// The seeding parameters for this job
    /// </summary>
    public SeedJobParameters? Parameters { get; set; }
}

/// <summary>
/// Seed job parameters for display
/// </summary>
public class SeedJobParameters
{
    public bool RegionEntity { get; set; }
    public bool ProvinceEntity { get; set; }
    public bool MunicipalityEntity { get; set; }
    public bool BarangayEntity { get; set; }
    public bool ForceReseed { get; set; }
}
