using Microsoft.Extensions.Logging;
using NTC.QoS.Domain.Interfaces;

namespace NTC.QoS.Infrastructure.Services;

/// <summary>
/// Service for consolidating and deduplicating connectivity assessment job triggers.
/// 
/// PURPOSE:
/// Prevents multiple assessment jobs from being queued for the same region when
/// multiple files of the same type are processed in close succession.
/// 
/// BEHAVIOR:
/// - Tracks pending assessments per region with timestamps
/// - Skips triggering if an assessment for the same region was recently triggered
/// - Automatically clears stale pending assessments after consolidation window
/// - Thread-safe operation using ReaderWriterLockSlim
/// 
/// CONSOLIDATION WINDOW:
/// - Default: 30 seconds
/// - If multiple files trigger assessment within 30s for same region, only first is queued
/// - After consolidation window, file-based checks (AssessmentJobId) provide additional deduplication
/// 
/// USE CASE:
/// Region1 receives 3 Cell files uploaded in quick succession (5 seconds apart)
/// - File1 completes: Assessment queued immediately, marked as pending
/// - File2 completes (within window): Assessment request skipped
/// - File3 completes (within window): Assessment request skipped
/// - Result: Only 1 assessment job for Region1 instead of 3
/// </summary>
public interface IAssessmentConsolidationService
{
    /// <summary>
    /// Requests an assessment trigger for a file and region.
    /// Returns true if the assessment should be queued, false if consolidation skipped it.
    /// </summary>
    bool ShouldTriggerAssessment(Guid fileId, Guid? regionId);

    /// <summary>
    /// Records that an assessment was successfully triggered for the region.
    /// </summary>
    void MarkAssessmentTriggered(Guid? regionId);

    /// <summary>
    /// Gets the timestamp of the last triggered assessment for a region (if any).
    /// </summary>
    DateTime? GetLastAssessmentTrigger(Guid? regionId);

    /// <summary>
    /// Gets the delay duration for scheduling assessment jobs.
    /// Assessment jobs are scheduled to run after this delay to allow other files to complete.
    /// </summary>
    TimeSpan GetAssessmentDelay();

    /// <summary>
    /// Checks if an assessment is currently scheduled for a region and gets its scheduled time.
    /// Returns the scheduled DateTime if found, null if not scheduled.
    /// </summary>
    DateTime? GetScheduledAssessmentTime(Guid? regionId);

    /// <summary>
    /// Marks an assessment as scheduled for a specific region and time.
    /// This is used to reschedule assessments when new files complete.
    /// </summary>
    void SetScheduledAssessmentTime(Guid? regionId, DateTime scheduledTime);

    /// <summary>
    /// Clears the scheduled assessment time for a region.
    /// Called after assessment job is enqueued.
    /// </summary>
    void ClearScheduledAssessmentTime(Guid? regionId);

    /// <summary>
    /// Checks if assessment should be rescheduled for a region.
    /// Returns true if assessment is scheduled to run soon and should be postponed.
    /// This implements the sliding window: keeps pushing assessment back while files arrive.
    /// </summary>
    bool ShouldRescheduleAssessment(Guid? regionId);

    /// <summary>
    /// Records a reschedule for assessment and increments the counter.
    /// </summary>
    void RecordReschedule(Guid? regionId);
}

/// <summary>
/// In-memory consolidation service for managing assessment triggers.
/// Maintains a cache of pending assessments per region with timestamp-based deduplication.
/// 
/// HYBRID ASSESSMENT DELAY STRATEGY:
/// 1. TIME-BASED DELAY: Assessment scheduled to run after ASSESSMENT_DELAY_SECONDS
/// 2. ACTIVITY-BASED RESCHEDULING: If new file completes before scheduled time:
///    - Check if assessment is scheduled within RESCHEDULE_THRESHOLD_SECONDS
///    - If yes, reschedule assessment forward by another ASSESSMENT_DELAY_SECONDS
///    - This creates a "sliding window" - assessment waits for files to stop arriving
/// 
/// Benefits:
/// - Handles variable file processing times (small files vs large files)
/// - Adapts to batch size (more files = later assessment)
/// - Prevents premature assessment with incomplete data
/// - Still caps total wait time with MAX_RESCHEDULES limit
/// 
/// Example:
/// T=0s   - File 1 completes → Assessment scheduled for T=60s
/// T=30s  - File 2 completes (large file) → Assessment rescheduled to T=90s
/// T=50s  - File 3 completes → Assessment rescheduled to T=110s  
/// T=110s - Assessment runs with ALL files' data ✓
/// </summary>
public class AssessmentConsolidationService : IAssessmentConsolidationService
{
    private readonly ILogger<AssessmentConsolidationService> _logger;
    private readonly ReaderWriterLockSlim _lock = new();
    
    /// <summary>
    /// Dictionary tracking last assessment trigger time per region.
    /// Key: RegionId (Guid), Value: DateTime UTC of last trigger
    /// </summary>
    private readonly Dictionary<Guid, DateTime> _regionAssessmentTimestamps = new();

    /// <summary>
    /// Dictionary tracking when assessment is SCHEDULED to run per region.
    /// Key: RegionId (Guid), Value: DateTime UTC when assessment will be scheduled
    /// Used for sliding window rescheduling.
    /// </summary>
    private readonly Dictionary<Guid, DateTime> _scheduledAssessmentTimes = new();

    /// <summary>
    /// Dictionary tracking how many times assessment has been rescheduled per region.
    /// Key: RegionId (Guid), Value: Count of reschedules
    /// Prevents infinite rescheduling if files keep arriving.
    /// </summary>
    private readonly Dictionary<Guid, int> _rescheduleCounters = new();
    
    /// <summary>
    /// Consolidation window in seconds.
    /// If an assessment was triggered for a region within this window,
    /// subsequent triggers for the same region are skipped.
    /// </summary>
    private const int CONSOLIDATION_WINDOW_SECONDS = 30;
    
    /// <summary>
    /// Assessment delay in seconds.
    /// When a file completes, the assessment is SCHEDULED to run after this delay
    /// instead of running immediately. This allows other files to complete and persist
    /// their data before the assessment calculation begins.
    /// 
    /// Recommended value: 30-60 seconds depending on expected batch processing time.
    /// Must be >= CONSOLIDATION_WINDOW_SECONDS to be effective.
    /// </summary>
    public const int ASSESSMENT_DELAY_SECONDS = 60;

    /// <summary>
    /// Reschedule threshold in seconds.
    /// If a new file completes and the assessment is scheduled to run within this many seconds,
    /// the assessment will be rescheduled forward by another ASSESSMENT_DELAY_SECONDS.
    /// This creates a "sliding window" that waits for files to stop arriving.
    /// 
    /// Example: If threshold is 15 seconds and assessment is scheduled in 10 seconds,
    /// completing a new file will reschedule it forward by 60 more seconds.
    /// </summary>
    private const int RESCHEDULE_THRESHOLD_SECONDS = 15;

    /// <summary>
    /// Maximum number of times an assessment can be rescheduled.
    /// Prevents infinite rescheduling if files keep arriving forever.
    /// After this many reschedules, assessment will run regardless of new file arrivals.
    /// </summary>
    private const int MAX_RESCHEDULES = 5;

    public AssessmentConsolidationService(ILogger<AssessmentConsolidationService> logger)
    {
        _logger = logger;
    }

    public bool ShouldTriggerAssessment(Guid fileId, Guid? regionId)
    {
        // Files without region ID bypass consolidation - let explicit region validation handle them
        if (!regionId.HasValue)
        {
            _logger.LogInformation("File {FileId} has no RegionId - assessment will be evaluated for region validation", fileId);
            return true;
        }

        _lock.EnterReadLock();
        try
        {
            if (_regionAssessmentTimestamps.TryGetValue(regionId.Value, out var lastTrigger))
            {
                var timeSinceLastTrigger = DateTime.UtcNow - lastTrigger;
                
                if (timeSinceLastTrigger.TotalSeconds < CONSOLIDATION_WINDOW_SECONDS)
                {
                    _logger.LogInformation(
                        "Consolidating: Assessment for Region {RegionId} was triggered {SecondsSinceLastTrigger:F1}s ago. " +
                        "Skipping duplicate trigger from file {FileId}. (Consolidation window: {ConsolidationWindow}s)",
                        regionId.Value, timeSinceLastTrigger.TotalSeconds, fileId, CONSOLIDATION_WINDOW_SECONDS);
                    return false;
                }

                _logger.LogInformation(
                    "Assessment for Region {RegionId} was last triggered {SecondsSinceLastTrigger:F1}s ago (outside {ConsolidationWindow}s window). " +
                    "File {FileId} will trigger new assessment.",
                    regionId.Value, timeSinceLastTrigger.TotalSeconds, CONSOLIDATION_WINDOW_SECONDS, fileId);
            }
            else
            {
                _logger.LogInformation(
                    "No recent assessment trigger recorded for Region {RegionId}. File {FileId} will trigger assessment.",
                    regionId.Value, fileId);
            }

            return true;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public void MarkAssessmentTriggered(Guid? regionId)
    {
        if (!regionId.HasValue)
        {
            return;
        }

        _lock.EnterWriteLock();
        try
        {
            _regionAssessmentTimestamps[regionId.Value] = DateTime.UtcNow;
            _logger.LogDebug("Marked assessment as triggered for Region {RegionId} at {Timestamp:O}", 
                regionId.Value, DateTime.UtcNow);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public DateTime? GetLastAssessmentTrigger(Guid? regionId)
    {
        if (!regionId.HasValue)
        {
            return null;
        }

        _lock.EnterReadLock();
        try
        {
            _regionAssessmentTimestamps.TryGetValue(regionId.Value, out var timestamp);
            return timestamp == default ? null : timestamp;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Gets the delay duration for scheduling assessment jobs.
    /// This delay allows multiple files to complete and persist before assessment runs.
    /// </summary>
    public TimeSpan GetAssessmentDelay()
    {
        return TimeSpan.FromSeconds(ASSESSMENT_DELAY_SECONDS);
    }

    /// <summary>
    /// Gets the scheduled assessment time for a region (if any).
    /// Returns the DateTime when assessment is scheduled to run.
    /// </summary>
    public DateTime? GetScheduledAssessmentTime(Guid? regionId)
    {
        if (!regionId.HasValue)
        {
            return null;
        }

        _lock.EnterReadLock();
        try
        {
            _scheduledAssessmentTimes.TryGetValue(regionId.Value, out var scheduledTime);
            return scheduledTime == default ? null : scheduledTime;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Sets the scheduled assessment time for a region.
    /// Called when initially scheduling or rescheduling an assessment.
    /// </summary>
    public void SetScheduledAssessmentTime(Guid? regionId, DateTime scheduledTime)
    {
        if (!regionId.HasValue)
        {
            return;
        }

        _lock.EnterWriteLock();
        try
        {
            _scheduledAssessmentTimes[regionId.Value] = scheduledTime;
            _logger.LogDebug(
                "Scheduled assessment for Region {RegionId} to run at {ScheduledTime:O} ({SecondsFromNow:F1}s from now)",
                regionId.Value, scheduledTime, (scheduledTime - DateTime.UtcNow).TotalSeconds);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Clears the scheduled assessment time for a region.
    /// Called after the assessment job has been enqueued.
    /// </summary>
    public void ClearScheduledAssessmentTime(Guid? regionId)
    {
        if (!regionId.HasValue)
        {
            return;
        }

        _lock.EnterWriteLock();
        try
        {
            _scheduledAssessmentTimes.Remove(regionId.Value);
            _rescheduleCounters.Remove(regionId.Value);
            _logger.LogDebug("Cleared scheduled assessment time for Region {RegionId}", regionId.Value);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Checks if assessment should be rescheduled for a region.
    /// Returns true if assessment is scheduled to run soon and should be postponed.
    /// This implements the sliding window: keeps pushing assessment back while files arrive.
    /// </summary>
    public bool ShouldRescheduleAssessment(Guid? regionId)
    {
        if (!regionId.HasValue)
        {
            return false;
        }

        _lock.EnterReadLock();
        try
        {
            // Check if assessment is scheduled for this region
            if (!_scheduledAssessmentTimes.TryGetValue(regionId.Value, out var scheduledTime))
            {
                return false; // No scheduled assessment
            }

            // Check how many times we've rescheduled
            _rescheduleCounters.TryGetValue(regionId.Value, out var rescheduleCount);
            if (rescheduleCount >= MAX_RESCHEDULES)
            {
                _logger.LogInformation(
                    "Assessment for Region {RegionId} reached max reschedules ({MaxReschedules}). " +
                    "Will NOT reschedule further - assessment will run as scheduled.",
                    regionId.Value, MAX_RESCHEDULES);
                return false; // Max reschedules reached, don't reschedule
            }

            // Check if assessment is scheduled within threshold window
            var timeUntilAssessment = scheduledTime - DateTime.UtcNow;
            if (timeUntilAssessment.TotalSeconds <= RESCHEDULE_THRESHOLD_SECONDS && timeUntilAssessment.TotalSeconds > 0)
            {
                _logger.LogInformation(
                    "Assessment for Region {RegionId} scheduled in {SecondsUntilAssessment:F1}s (within {RescheduleThreshold}s threshold). " +
                    "New file detected - assessment will be RESCHEDULED ({RescheduleCount}/{MaxReschedules}).",
                    regionId.Value, timeUntilAssessment.TotalSeconds, RESCHEDULE_THRESHOLD_SECONDS,
                    rescheduleCount + 1, MAX_RESCHEDULES);
                return true; // Should reschedule
            }

            return false; // Assessment not within threshold, don't reschedule
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Records a reschedule for assessment and increments the counter.
    /// </summary>
    public void RecordReschedule(Guid? regionId)
    {
        if (!regionId.HasValue)
        {
            return;
        }

        _lock.EnterWriteLock();
        try
        {
            if (!_rescheduleCounters.ContainsKey(regionId.Value))
            {
                _rescheduleCounters[regionId.Value] = 0;
            }
            _rescheduleCounters[regionId.Value]++;
            _logger.LogDebug("Incremented reschedule counter for Region {RegionId}: {Count}", 
                regionId.Value, _rescheduleCounters[regionId.Value]);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }
}
