using Cronos;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.Analytics;
using NTC.QoS.Domain.Models.DTOs.Scheduler;
using NTC.QoS.Infrastructure.BackgroundTasks;

namespace NTC.QoS.Infrastructure.Services;

/// <summary>
/// Service for managing connectivity assessment schedulers and their Hangfire jobs
/// NOTE: Scheduler calls ProcessConnectivityAssessmentJob directly (not StartConnectivityAssessmentJobHandler)
/// This is intentional - the scheduler is for automated recurring jobs, while StartConnectivityAssessmentJobHandler
/// is for one-time user-initiated jobs through the API that need immediate job ID response.
/// </summary>
public class SchedulerService : ISchedulerService
{
    private readonly IBackgroundJobService _backgroundJobService;
    private readonly ILogger<SchedulerService> _logger;

    public SchedulerService(
        IBackgroundJobService backgroundJobService,
        ILogger<SchedulerService> logger)
    {
        _backgroundJobService = backgroundJobService;
        _logger = logger;
    }

    /// <summary>
    /// Creates or updates a recurring Hangfire job for the scheduler
    /// Uses ScheduledConnectivityAssessmentJob wrapper to validate date constraints before execution
    /// </summary>
    public Task RegisterSchedulerAsync(ConnectivityAssessmentScheduler scheduler)
    {
        var locationIds = JsonConvert.DeserializeObject<List<Guid>>(scheduler.LocationIdsJson) ?? new List<Guid>();
        
        // Parse job execution configuration
        var config = string.IsNullOrEmpty(scheduler.ConfigurationJson)
            ? new JobExecutionConfigurationDto()
            : JsonConvert.DeserializeObject<JobExecutionConfigurationDto>(scheduler.ConfigurationJson) ?? new JobExecutionConfigurationDto();

        // Create the job request DTO with typed location properties
        var jobRequest = new ConnectivityAssessmentJobRequestDto
        {
            JobId = Guid.NewGuid().ToString("N"), // Generate a unique job ID for scheduled job
            UserId = scheduler.CreatedBy,
            ActualTotalLocations = locationIds.Count, // Initial count, will be recalculated by job
            GenerateHierarchicalAssessments = config.GenerateHierarchicalAssessments,
            HierarchicalDepth = config.HierarchicalDepth,
            MaxHierarchicalLocations = config.MaxHierarchicalLocations, // null = no limit, value = apply limit
            ForceRecalculate = config.ForceRecalculate
        };

        // Populate typed location ID properties based on location type
        switch (scheduler.LocationType)
        {
            case LocationTypeEnum.Regional:
                jobRequest.RegionIds = locationIds;
                break;
            case LocationTypeEnum.Provincial:
                jobRequest.ProvinceIds = locationIds;
                break;
            case LocationTypeEnum.Municipality:
                jobRequest.MunicipalityIds = locationIds;
                break;
            case LocationTypeEnum.Barangay:
                jobRequest.BarangayIds = locationIds;
                break;
            default:
                throw new InvalidOperationException($"Unsupported location type: {scheduler.LocationType}");
        }

        // Also populate deprecated properties for backward compatibility
        #pragma warning disable CS0618 // Type or member is obsolete
        jobRequest.LocationType = scheduler.LocationType;
        jobRequest.LocationIds = locationIds;
        #pragma warning restore CS0618

        // Validate end date timing - warn if end date would cut off a scheduled run
        if (scheduler.EndDate.HasValue)
        {
            var nextRun = GetNextExecutionTime(scheduler.CronExpression, scheduler.TimeZone);
            if (nextRun.HasValue)
            {
                var endDateUtc = scheduler.EndDate.Value.Kind == DateTimeKind.Utc 
                    ? scheduler.EndDate.Value 
                    : DateTime.SpecifyKind(scheduler.EndDate.Value, DateTimeKind.Utc);

                if (nextRun.Value > endDateUtc)
                {
                    _logger.LogWarning(
                        "Scheduler '{Name}' (ID: {Id}): End date {EndDate} occurs before next scheduled run {NextRun}. " +
                        "This scheduler may never execute. Consider extending end date to at least {SuggestedEnd}",
                        scheduler.Name, scheduler.Id, endDateUtc, nextRun.Value, 
                        nextRun.Value.AddHours(1)); // Suggest 1 hour buffer after scheduled run
                }
            }
        }

        // Register the recurring job with Hangfire using the wrapper
        // The wrapper validates StartDate/EndDate before executing the actual job
        // IMPORTANT: Pass timezone to Hangfire so it interprets the cron expression correctly
        _backgroundJobService.AddOrUpdateRecurringJob<IScheduledConnectivityAssessmentJob>(
            scheduler.HangfireJobId,
            job => job.ExecuteScheduledAsync(
                scheduler.Id,
                jobRequest, 
                scheduler.StartDate, 
                scheduler.EndDate,
                scheduler.TimeZone,
                CancellationToken.None),
            scheduler.CronExpression,
            scheduler.TimeZone  // Pass timezone so Hangfire uses correct time zone for cron execution
        );

        _logger.LogInformation(
            "Registered Hangfire recurring job '{JobId}' with cron '{Cron}' - " +
            "Hierarchical: {Hierarchical}, Depth: {Depth}, StartDate: {StartDate}, EndDate: {EndDate}",
            scheduler.HangfireJobId, scheduler.CronExpression, 
            config.GenerateHierarchicalAssessments, config.HierarchicalDepth,
            scheduler.StartDate, scheduler.EndDate);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Removes the Hangfire recurring job for the scheduler
    /// </summary>
    public Task UnregisterSchedulerAsync(string hangfireJobId)
    {
        _backgroundJobService.RemoveRecurringJob(hangfireJobId);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Pauses the scheduler (removes the recurring job)
    /// </summary>
    public Task PauseSchedulerAsync(string hangfireJobId)
    {
        return UnregisterSchedulerAsync(hangfireJobId);
    }

    /// <summary>
    /// Resumes the scheduler (re-registers the recurring job)
    /// </summary>
    public Task ResumeSchedulerAsync(ConnectivityAssessmentScheduler scheduler)
    {
        return RegisterSchedulerAsync(scheduler);
    }

    /// <summary>
    /// Generates a cron expression based on schedule configuration
    /// Uses Cron format: minute hour day-of-month month day-of-week
    /// </summary>
    public string GenerateCronExpression(ScheduleConfigurationDto config, ScheduleFrequencyEnum frequency)
    {
        return frequency switch
        {
            ScheduleFrequencyEnum.Hourly => $"{config.MinuteOfHour ?? 0} * * * *",
            
            ScheduleFrequencyEnum.Daily => $"{config.MinuteOfDay ?? 0} {config.HourOfDay ?? 0} * * *",
            
            ScheduleFrequencyEnum.Weekly => GenerateWeeklyCron(config),
            
            ScheduleFrequencyEnum.Monthly => $"{config.MonthlyMinute ?? 0} {config.MonthlyHour ?? 0} {config.DayOfMonth ?? 1} * *",
            
            ScheduleFrequencyEnum.Custom => config.CustomCronExpression ?? "0 0 * * *", // Default to daily at midnight
            
            _ => "0 0 * * *" // Default to daily at midnight
        };
    }

    /// <summary>
    /// Generates a weekly cron expression
    /// </summary>
    private string GenerateWeeklyCron(ScheduleConfigurationDto config)
    {
        var minute = config.WeeklyMinute ?? 0;
        var hour = config.WeeklyHour ?? 0;
        
        if (config.DaysOfWeek == null || config.DaysOfWeek.Count == 0)
        {
            // Default to Monday if no days specified
            return $"{minute} {hour} * * 1";
        }

        // Join days with commas (0=Sunday, 1=Monday, etc.)
        var days = string.Join(",", config.DaysOfWeek.OrderBy(d => d));
        return $"{minute} {hour} * * {days}";
    }

    /// <summary>
    /// Parses configuration JSON and extracts ScheduleConfigurationDto
    /// </summary>
    public ScheduleConfigurationDto? ParseConfiguration(string? configJson)
    {
        if (string.IsNullOrWhiteSpace(configJson))
        {
            return null;
        }

        try
        {
            return JsonConvert.DeserializeObject<ScheduleConfigurationDto>(configJson);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Calculates the next execution time based on cron expression
    /// </summary>
    public DateTime? GetNextExecutionTime(string cronExpression, string timeZone)
    {
        try
        {
            var cron = Cronos.CronExpression.Parse(cronExpression);
            var tz = TimeZoneInfo.FindSystemTimeZoneById(timeZone);
            
            // Cronos requires UTC DateTime for GetNextOccurrence
            var nowUtc = DateTime.UtcNow;
            
            // GetNextOccurrence will handle timezone conversion internally
            var next = cron.GetNextOccurrence(nowUtc, tz);
            
            if (next.HasValue)
            {
                _logger.LogDebug("Next execution for cron '{Cron}' in timezone '{TimeZone}': {NextRun} UTC",
                    cronExpression, timeZone, next.Value);
            }
            
            return next;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to calculate next execution time for cron '{Cron}' in timezone '{TimeZone}'",
                cronExpression, timeZone);
            return null;
        }
    }

    /// <summary>
    /// Validates if the timezone ID is valid on this system
    /// </summary>
    public bool ValidateTimeZone(string timeZone)
    {
        try
        {
            TimeZoneInfo.FindSystemTimeZoneById(timeZone);
            return true;
        }
        catch (TimeZoneNotFoundException)
        {
            _logger.LogWarning("Timezone '{TimeZone}' not found. Available timezones: {Timezones}",
                timeZone, string.Join(", ", TimeZoneInfo.GetSystemTimeZones().Take(5).Select(tz => tz.Id)));
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating timezone '{TimeZone}'", timeZone);
            return false;
        }
    }
}
