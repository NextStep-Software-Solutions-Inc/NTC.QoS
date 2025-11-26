using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.Analytics;

namespace NTC.QoS.Infrastructure.BackgroundTasks;

/// <summary>
/// Wrapper for scheduled connectivity assessment jobs that validates date constraints
/// before executing the actual job. This prevents jobs from running outside their 
/// configured start/end date windows.
/// </summary>
public interface IScheduledConnectivityAssessmentJob
{
    /// <summary>
    /// Executes a scheduled connectivity assessment with date validation
    /// </summary>
    Task ExecuteScheduledAsync(Guid schedulerId, ConnectivityAssessmentJobRequestDto jobRequest, DateTime? startDate, DateTime? endDate, string timeZone, CancellationToken cancellationToken);
}

public class ScheduledConnectivityAssessmentJob : IScheduledConnectivityAssessmentJob
{
    private readonly IProcessConnectivityAssessmentJob _processJob;
    private readonly IConnectivityAssessmentSchedulerRepository _schedulerRepository;
    private readonly ILogger<ScheduledConnectivityAssessmentJob> _logger;

    public ScheduledConnectivityAssessmentJob(
        IProcessConnectivityAssessmentJob processJob,
        IConnectivityAssessmentSchedulerRepository schedulerRepository,
        ILogger<ScheduledConnectivityAssessmentJob> logger)
    {
        _processJob = processJob;
        _schedulerRepository = schedulerRepository;
        _logger = logger;
    }

    /// <summary>
    /// Validates date constraints and executes the job if within valid time window
    /// </summary>
    public async Task ExecuteScheduledAsync(
        Guid schedulerId, 
        ConnectivityAssessmentJobRequestDto jobRequest, 
        DateTime? startDate, 
        DateTime? endDate, 
        string timeZone,
        CancellationToken cancellationToken)
    {
        // Get current time in the scheduler's timezone
        TimeZoneInfo tz;
        try
        {
            tz = TimeZoneInfo.FindSystemTimeZoneById(timeZone);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Invalid timezone '{TimeZone}' for scheduler {SchedulerId}. Using UTC.", timeZone, schedulerId);
            tz = TimeZoneInfo.Utc;
        }

        var nowUtc = DateTime.UtcNow;
        var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, tz);

        _logger.LogInformation(
            "Scheduler {SchedulerId}: Checking execution window. Current time: {CurrentTime} ({TimeZone}), Start: {Start}, End: {End}",
            schedulerId, nowLocal, timeZone, startDate, endDate);

        // Check if current time is before start date
        if (startDate.HasValue)
        {
            // Convert start date to timezone-aware comparison
            var startDateUtc = startDate.Value.Kind == DateTimeKind.Utc 
                ? startDate.Value 
                : TimeZoneInfo.ConvertTimeToUtc(startDate.Value, tz);

            if (nowUtc < startDateUtc)
            {
                _logger.LogWarning(
                    "Scheduler {SchedulerId}: Skipping execution - current time {CurrentTime} is before start date {StartDate}",
                    schedulerId, nowLocal, TimeZoneInfo.ConvertTimeFromUtc(startDateUtc, tz));
                
                await UpdateSchedulerSkipCount(schedulerId, "Before start date");
                return;
            }
        }

        // Check if current time is after end date
        if (endDate.HasValue)
        {
            // Convert end date to timezone-aware comparison
            var endDateUtc = endDate.Value.Kind == DateTimeKind.Utc 
                ? endDate.Value 
                : TimeZoneInfo.ConvertTimeToUtc(endDate.Value, tz);

            if (nowUtc > endDateUtc)
            {
                _logger.LogWarning(
                    "Scheduler {SchedulerId}: Skipping execution - current time {CurrentTime} is after end date {EndDate}. " +
                    "Consider disabling this scheduler as it has reached its end date.",
                    schedulerId, nowLocal, TimeZoneInfo.ConvertTimeFromUtc(endDateUtc, tz));
                
                await UpdateSchedulerSkipCount(schedulerId, "After end date");
                
                // Optionally pause the scheduler since it's past end date
                var scheduler = await _schedulerRepository.Query()
                    .Where(s => s.Id == schedulerId)
                    .FirstOrDefaultAsync();
                    
                if (scheduler != null && scheduler.Status == Domain.Enums.SchedulerStatusEnum.Active)
                {
                    _logger.LogInformation("Scheduler {SchedulerId}: Auto-pausing scheduler as end date has been reached", schedulerId);
                    scheduler.Status = Domain.Enums.SchedulerStatusEnum.Paused;
                    scheduler.LastError = $"Automatically paused - end date {endDate} reached";
                    _schedulerRepository.Update(scheduler);
                    await _schedulerRepository.SaveChangesAsync();
                }
                
                return;
            }
        }

        // Date constraints are valid - execute the job
        _logger.LogInformation(
            "Scheduler {SchedulerId}: Executing job {JobId} - within valid time window",
            schedulerId, jobRequest.JobId);

        try
        {
            // Update scheduler execution tracking
            var scheduler = await _schedulerRepository.Query()
                .Where(s => s.Id == schedulerId)
                .FirstOrDefaultAsync();
                
            if (scheduler != null)
            {
                scheduler.LastExecutionTime = nowUtc;
                scheduler.ExecutionCount++;
                _schedulerRepository.Update(scheduler);
                await _schedulerRepository.SaveChangesAsync();
            }

            // Execute the actual connectivity assessment job
            await _processJob.ExecuteAsync(jobRequest, cancellationToken);

            _logger.LogInformation(
                "Scheduler {SchedulerId}: Job {JobId} completed successfully",
                schedulerId, jobRequest.JobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Scheduler {SchedulerId}: Job {JobId} failed with error: {Error}",
                schedulerId, jobRequest.JobId, ex.Message);

            // Update scheduler failure tracking
            var scheduler = await _schedulerRepository.Query()
                .Where(s => s.Id == schedulerId)
                .FirstOrDefaultAsync();
                
            if (scheduler != null)
            {
                scheduler.FailureCount++;
                scheduler.LastError = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC: {ex.Message}";
                _schedulerRepository.Update(scheduler);
                await _schedulerRepository.SaveChangesAsync();
            }

            throw; // Re-throw to let Hangfire handle retry logic
        }
    }

    private async Task UpdateSchedulerSkipCount(Guid schedulerId, string reason)
    {
        try
        {
            var scheduler = await _schedulerRepository.Query()
                .Where(s => s.Id == schedulerId)
                .FirstOrDefaultAsync();
                
            if (scheduler != null)
            {
                scheduler.LastError = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC: Skipped - {reason}";
                _schedulerRepository.Update(scheduler);
                await _schedulerRepository.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update scheduler skip count for {SchedulerId}", schedulerId);
        }
    }
}
