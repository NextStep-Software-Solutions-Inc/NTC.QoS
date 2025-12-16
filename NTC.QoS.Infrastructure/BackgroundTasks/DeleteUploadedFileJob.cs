using Hangfire;
using Microsoft.Extensions.Logging;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.File;
using NTC.QoS.Infrastructure.Helper;

namespace NTC.QoS.Infrastructure.BackgroundTasks;

internal class DeleteUploadedFileJob : IDeleteUploadedFileJob
{
    private readonly IFileRepository _fileRepository;
    private readonly IBlobStorageService _blobStorageService;
    private readonly ILogger<DeleteUploadedFileJob> _logger;

    public DeleteUploadedFileJob(
        IFileRepository fileRepository,
        IBlobStorageService blobStorageService,
        ILogger<DeleteUploadedFileJob> logger)
    {
        _fileRepository = fileRepository;
        _blobStorageService = blobStorageService;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 2, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
    public async Task ExecuteAsync(FileDto parameters, CancellationToken token)
    {
        var fileQuery = _fileRepository.Query(f => !f.IsDeleted && f.Id == parameters.Id);

        var file = await _fileRepository.FirstOrDefaultAsync(fileQuery, cancellationToken: token);
        if (file is null)
        {
            _logger.LogWarning("File not found: {FileId}", parameters.Id);
            return;
        }

        // Try to delete from blob storage first
        try
        {
            if (Uri.TryCreate(file.Path, UriKind.Absolute, out var blobUri))
            {
                // Use shared helper to extract container and blob name from the blob URL
                var (containerName, blobName) = ProcessingJobHelper.ExtractBlobInfo(blobUri);

                _logger.LogInformation("Deleting file from blob storage: container={ContainerName}, blob={BlobName}",
                    containerName, blobName);

                var deleted = await _blobStorageService.DeleteAsync(containerName, blobName, token);

                if (deleted)
                {
                    _logger.LogInformation("Successfully deleted file from blob storage: {BlobPath}", file.Path);
                }
                else
                {
                    _logger.LogWarning("File was not found in blob storage: {BlobPath}", file.Path);
                }
            }
            else
            {
                // Fallback for legacy local file paths
                _logger.LogWarning("Attempting to delete legacy local file: {FilePath}", file.Path);

                if (System.IO.File.Exists(file.Path))
                {
                    System.IO.File.Delete(file.Path);
                    _logger.LogInformation("Legacy file deleted: {FilePath}", file.Path);
                }
                else
                {
                    _logger.LogWarning("Legacy file not found on disk: {FilePath}", file.Path);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file from storage: {FilePath}", file.Path);
            // Continue to delete the database record even if file deletion fails
        }

        // Delete the file record from the database
        try
        {
            _fileRepository.Delete(file);
            await _fileRepository.SaveChangesAsync(token);
            _logger.LogInformation("File record deleted from database: {FileId}", file.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file record from database: {FileId}", file.Id);
            throw;
        }
    }
}
