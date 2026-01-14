namespace NTC.QoS.Domain.Enums;

/// <summary>
/// Status of a background report generation job
/// </summary>
public enum ReportJobStatusEnum
{
    /// <summary>
    /// Job is queued and waiting to start
    /// </summary>
    Queued = 0,

    /// <summary>
    /// Job is currently processing
    /// </summary>
    Processing = 1,

    /// <summary>
    /// Job completed successfully
    /// </summary>
    Completed = 2,

    /// <summary>
    /// Job failed with an error
    /// </summary>
    Failed = 3
}
