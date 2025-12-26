using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.Analytics;

namespace NTC.QoS.Infrastructure.BackgroundTasks;

public class GenerateConnectivityAssessmentJob : IGenerateConnectivityAssessmentJob
{
    private readonly ILogger<GenerateConnectivityAssessmentJob> _logger;
    private readonly IRepository<NTC.QoS.Domain.Entities.File> _fileRepository;
    private readonly IBackgroundJobService _backgroundJobService;

    public GenerateConnectivityAssessmentJob(
        ILogger<GenerateConnectivityAssessmentJob> logger,
        IRepository<NTC.QoS.Domain.Entities.File> fileRepository,
        IBackgroundJobService backgroundJobService)
    {
        _logger = logger;
        _fileRepository = fileRepository;
        _backgroundJobService = backgroundJobService;
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

        // Create request for assessment generation
        var request = new ConnectivityAssessmentJobRequestDto
        {
            // Regenerate assessments for the file's region only for dashboard display purposes
            RegionIds = file.RegionId.HasValue ? [file.RegionId.Value] : null,
            GenerateHierarchicalAssessments = false,
            HierarchicalDepth = 0,
            ForceRecalculate = true, // Since new data may change existing assessments
            JobId = Guid.NewGuid().ToString()
        };

        // Store the assessment job ID in the file record for status tracking
        file.AssessmentJobId = request.JobId;
        _fileRepository.Update(file);
        await _fileRepository.SaveChangesAsync(cancellationToken);

        // Enqueue the existing assessment job
        _backgroundJobService.Enqueue<IProcessConnectivityAssessmentJob>(job => job.ExecuteAsync(request, cancellationToken));

        _logger.LogInformation("Enqueued connectivity assessment generation for file {FileId} with job ID {JobId}", fileId, request.JobId);
    }
}