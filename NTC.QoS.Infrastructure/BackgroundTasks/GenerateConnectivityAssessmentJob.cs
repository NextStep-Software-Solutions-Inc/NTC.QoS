using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.Analytics;
using NTC.QoS.Infrastructure.Services;

namespace NTC.QoS.Infrastructure.BackgroundTasks;

public class GenerateConnectivityAssessmentJob : IGenerateConnectivityAssessmentJob
{
    private readonly ILogger<GenerateConnectivityAssessmentJob> _logger;
    private readonly IRepository<NTC.QoS.Domain.Entities.File> _fileRepository;
    private readonly IBackgroundJobService _backgroundJobService;
    private readonly IAssessmentConsolidationService _consolidationService;

    public GenerateConnectivityAssessmentJob(
        ILogger<GenerateConnectivityAssessmentJob> logger,
        IRepository<NTC.QoS.Domain.Entities.File> fileRepository,
        IBackgroundJobService backgroundJobService,
        IAssessmentConsolidationService consolidationService)
    {
        _logger = logger;
        _fileRepository = fileRepository;
        _backgroundJobService = backgroundJobService;
        _consolidationService = consolidationService;
    }

    [AutomaticRetry(Attempts = 2, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
    public async Task ExecuteAsync(Guid fileId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting connectivity assessment generation for file {FileId}", fileId);

        var file = await _fileRepository.Query().FirstOrDefaultAsync(f => f.Id == fileId, cancellationToken);
        if (file == null)
        {
            _logger.LogWarning("File {FileId} not found for connectivity assessment generation", fileId);
            return;
        }

        if (file.Status != NTC.QoS.Domain.Enums.FileStatusEnum.Success && file.Status != NTC.QoS.Domain.Enums.FileStatusEnum.PartiallyProcessed)
        {
            _logger.LogInformation("File {FileId} status is {Status}, skipping assessment generation", fileId, file.Status);
            return;
        }

        // Check if assessment already triggered for this file (idempotency)
        if (!string.IsNullOrEmpty(file.AssessmentJobId))
        {
            _logger.LogInformation("Assessment generation already triggered for file {FileId} with job ID {JobId}, skipping duplicate trigger", fileId, file.AssessmentJobId);
            return;
        }

        // Determine if this file type should trigger assessment generation
        var relevantTypes = new[] { NTC.QoS.Domain.Enums.FileTypeEnum.NAP, NTC.QoS.Domain.Enums.FileTypeEnum.Cell, NTC.QoS.Domain.Enums.FileTypeEnum.Speedtest, NTC.QoS.Domain.Enums.FileTypeEnum.WirelessValidation, NTC.QoS.Domain.Enums.FileTypeEnum.CellularCoverage };
        if (!relevantTypes.Contains(file.Type))
        {
            _logger.LogInformation("File type {Type} does not trigger connectivity assessment generation", file.Type);
            return;
        }

        // EXPLICIT REGION VALIDATION
        // Ensure file has a valid region before triggering assessment
        if (!file.RegionId.HasValue)
        {
            _logger.LogWarning(
                "File {FileId} ({FileName}) of type {FileType} has no RegionId assigned. " +
                "Assessment generation CANCELLED. Action required: Assign region to file before processing.",
                fileId, file.Name, file.Type);
            return;
        }

        // SMART CONSOLIDATION CHECK WITH SLIDING WINDOW
        // Step 1: Check if assessment was recently triggered for this region (file-level dedup)
        if (!_consolidationService.ShouldTriggerAssessment(fileId, file.RegionId))
        {
            _logger.LogInformation(
                "Assessment consolidation: Skipping assessment trigger for file {FileId} in Region {RegionId} " +
                "due to recent assessment. File-level idempotency check will prevent duplicate job IDs.",
                fileId, file.RegionId.Value);
            
            // Still mark the file as having triggered assessment (set AssessmentJobId to indicate attempt)
            // This prevents re-processing even though the actual job was consolidated
            file.AssessmentJobId = "consolidated";
            _fileRepository.Update(file);
            await _fileRepository.SaveChangesAsync(cancellationToken);
            return;
        }

        // Step 2: Check if assessment is already scheduled for this region (sliding window logic)
        var scheduledTime = _consolidationService.GetScheduledAssessmentTime(file.RegionId);
        if (scheduledTime.HasValue)
        {
            // Assessment is already scheduled for this region
            if (_consolidationService.ShouldRescheduleAssessment(file.RegionId))
            {
                // Assessment is scheduled soon, and we just detected a new file completing
                // Reschedule it forward to wait for more files (sliding window)
                _logger.LogInformation(
                    "File {FileId} completed while assessment was pending for Region {RegionId}. " +
                    "Rescheduling assessment to allow more batch files to complete.",
                    fileId, file.RegionId.Value);

                file.AssessmentJobId = "consolidated-rescheduled";
                _fileRepository.Update(file);
                await _fileRepository.SaveChangesAsync(cancellationToken);

                _consolidationService.RecordReschedule(file.RegionId);
                var newScheduledTime = DateTime.UtcNow.AddSeconds(AssessmentConsolidationService.ASSESSMENT_DELAY_SECONDS);
                _consolidationService.SetScheduledAssessmentTime(file.RegionId, newScheduledTime);

                _logger.LogInformation(
                    "Assessment for Region {RegionId} rescheduled to {NewScheduledTime:O} " +
                    "(will wait {AdditionalSeconds}s more for other files)",
                    file.RegionId.Value, newScheduledTime, AssessmentConsolidationService.ASSESSMENT_DELAY_SECONDS);
                return;
            }
            else
            {
                // Assessment is scheduled but not within threshold, or max reschedules reached
                // Skip this file - it will be included in the already-scheduled assessment
                _logger.LogInformation(
                    "Assessment already scheduled for Region {RegionId} at {ScheduledTime:O}. " +
                    "File {FileId} consolidation: will be included in scheduled assessment.",
                    file.RegionId.Value, scheduledTime.Value, fileId);

                file.AssessmentJobId = "consolidated-queued";
                _fileRepository.Update(file);
                await _fileRepository.SaveChangesAsync(cancellationToken);
                return;
            }
        }

        // Create request for assessment generation
        var request = new ConnectivityAssessmentJobRequestDto
        {
            // Regenerate assessments for the file's region only for dashboard display purposes
            RegionIds = [file.RegionId.Value],
            GenerateHierarchicalAssessments = false,
            HierarchicalDepth = 0,
            ForceRecalculate = true, // Since new data may change existing assessments
            JobId = Guid.NewGuid().ToString(),
            // Also populate deprecated properties for backward compatibility
            #pragma warning disable CS0618 // Type or member is obsolete
            LocationIds = [file.RegionId.Value],
            LocationType = Domain.Enums.LocationTypeEnum.Regional
            #pragma warning restore CS0618
        };

        // Store the assessment job ID in the file record for status tracking
        file.AssessmentJobId = request.JobId;
        _fileRepository.Update(file);
        await _fileRepository.SaveChangesAsync(cancellationToken);

        // Record this assessment trigger in consolidation service for future deduplication
        _consolidationService.MarkAssessmentTriggered(file.RegionId);

        // Schedule the assessment job to run after a delay instead of enqueueing immediately.
        // This allows other files being processed in the same batch to complete and persist
        // their data before the assessment calculation begins, ensuring all batch data is included.
        var delay = _consolidationService.GetAssessmentDelay();
        var assessmentScheduledTime = DateTime.UtcNow.Add(delay);
        
        // Record the scheduled time for sliding window rescheduling
        _consolidationService.SetScheduledAssessmentTime(file.RegionId, assessmentScheduledTime);

        _backgroundJobService.Schedule<IProcessConnectivityAssessmentJob>(
            job => job.ExecuteAsync(request, cancellationToken), 
            delay);

        _logger.LogInformation(
            "Scheduled connectivity assessment for file {FileId} (Region {RegionId}) with job ID {JobId}. " +
            "Assessment scheduled for {ScheduledTime:O} ({DelaySeconds}s from now). " +
            "If more files complete before then, assessment will be rescheduled to include their data.",
            fileId, file.RegionId.Value, request.JobId, assessmentScheduledTime, delay.TotalSeconds);
    }
}