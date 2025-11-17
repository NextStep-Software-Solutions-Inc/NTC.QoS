namespace NTC.QoS.Domain.Enums;

/// <summary>
/// Status enumeration for connectivity assessment background jobs
/// </summary>
public enum ConnectivityAssessmentJobStatusEnum
{
    /// <summary>
    /// Job has been queued but not yet started
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Job is currently being processed
    /// </summary>
    Processing = 1,

    /// <summary>
    /// Job has completed successfully
    /// </summary>
    Completed = 2,

    /// <summary>
    /// Job has failed due to an error
    /// </summary>
    Failed = 3,

    /// <summary>
    /// Job has been cancelled by user or system
    /// </summary>
    Cancelled = 4
}