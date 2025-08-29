using Hangfire;
using Microsoft.Extensions.Logging;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.File;

namespace NTC.QoS.Infrastructure.BackgroundTasks;

internal class DeleteUploadedFileJob : IDeleteUploadedFileJob
{
    private readonly IFileRepository _fileRepository;
    private readonly ILogger<DeleteUploadedFileJob> _logger;

    public DeleteUploadedFileJob(IFileRepository fileRepository, ILogger<DeleteUploadedFileJob> logger)
    {
        _fileRepository = fileRepository;
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
        // Delete the file from the file system  
        if (System.IO.File.Exists(file.Path))
        {
            System.IO.File.Delete(file.Path);
            _logger.LogInformation("File deleted: {FilePath}", file.Path);
        }
        else
        {
            _logger.LogWarning("File not found on disk: {FilePath}", file.Path);
        }
        // Delete the file record from the database  
        _fileRepository.Delete(file);
        await _fileRepository.SaveChangesAsync(token);
        _logger.LogInformation("File record deleted: {FileId}", file.Id);
    }
}
