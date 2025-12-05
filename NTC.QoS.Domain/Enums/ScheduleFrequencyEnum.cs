namespace NTC.QoS.Domain.Enums;

/// <summary>
/// Defines the frequency options for scheduled jobs
/// </summary>
public enum ScheduleFrequencyEnum
{
    /// <summary>
    /// Run once at a specific time
    /// </summary>
    Once = 0,

    /// <summary>
    /// Run every hour
    /// </summary>
    Hourly = 1,

    /// <summary>
    /// Run daily at a specific time
    /// </summary>
    Daily = 2,

    /// <summary>
    /// Run weekly on specific days
    /// </summary>
    Weekly = 3,

    /// <summary>
    /// Run monthly on a specific day
    /// </summary>
    Monthly = 4,

    /// <summary>
    /// Custom cron expression
    /// </summary>
    Custom = 5
}
