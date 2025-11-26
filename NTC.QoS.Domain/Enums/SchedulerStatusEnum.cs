namespace NTC.QoS.Domain.Enums;

/// <summary>
/// Defines the status of a scheduled job
/// </summary>
public enum SchedulerStatusEnum
{
    /// <summary>
    /// Scheduler is active and running
    /// </summary>
    Active = 0,

    /// <summary>
    /// Scheduler is paused/disabled
    /// </summary>
    Paused = 1,

    /// <summary>
    /// Scheduler has completed (for one-time schedules)
    /// </summary>
    Completed = 2,

    /// <summary>
    /// Scheduler has failed
    /// </summary>
    Failed = 3
}
