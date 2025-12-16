using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Models.DTOs.Scheduler;

namespace NTC.QoS.Domain.Interfaces;

/// <summary>
/// Service for managing connectivity assessment schedulers and their Hangfire jobs
/// </summary>
public interface ISchedulerService
{
    /// <summary>
    /// Creates or updates a recurring Hangfire job for the scheduler
    /// </summary>
    Task RegisterSchedulerAsync(ConnectivityAssessmentScheduler scheduler);

    /// <summary>
    /// Removes the Hangfire recurring job for the scheduler
    /// </summary>
    Task UnregisterSchedulerAsync(string hangfireJobId);

    /// <summary>
    /// Pauses the scheduler (removes the recurring job)
    /// </summary>
    Task PauseSchedulerAsync(string hangfireJobId);

    /// <summary>
    /// Resumes the scheduler (re-registers the recurring job)
    /// </summary>
    Task ResumeSchedulerAsync(ConnectivityAssessmentScheduler scheduler);

    /// <summary>
    /// Generates a cron expression based on schedule configuration
    /// </summary>
    string GenerateCronExpression(ScheduleConfigurationDto config, Domain.Enums.ScheduleFrequencyEnum frequency);

    /// <summary>
    /// Parses configuration JSON and extracts ScheduleConfigurationDto
    /// </summary>
    ScheduleConfigurationDto? ParseConfiguration(string? configJson);

    /// <summary>
    /// Calculates the next execution time based on cron expression
    /// </summary>
    DateTime? GetNextExecutionTime(string cronExpression, string timeZone);

    /// <summary>
    /// Validates if the timezone ID is valid on this system
    /// </summary>
    bool ValidateTimeZone(string timeZone);
}
