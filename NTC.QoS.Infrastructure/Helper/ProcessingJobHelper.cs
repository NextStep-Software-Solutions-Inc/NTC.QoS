using Azure.Storage.Blobs;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Interfaces;
using System.Web;

namespace NTC.QoS.Infrastructure.Helper;

/// <summary>
/// Shared helper methods for processing jobs to eliminate redundant code
/// </summary>
public static class ProcessingJobHelper
{
    /// <summary>
    /// Extracts container name and blob name from a blob storage URL (supports both cloud Azure URLs and local Azurite emulator URLs)
    /// </summary>
    /// <param name="blobUri">The blob storage URI</param>
    /// <returns>Tuple containing container name and blob name</returns>
    /// <exception cref="ArgumentException">Thrown when blob URL format is invalid</exception>
    public static (string containerName, string blobName) ExtractBlobInfo(Uri blobUri, string? storageAccountName = null)
    {
        try
        {
            var blobUriBuilder = new BlobUriBuilder(blobUri);
            var containerName = blobUriBuilder.BlobContainerName;
            var blobName = blobUriBuilder.BlobName;

            // For a path-style endpoint on a custom hostname, BlobUriBuilder can
            // treat the account path prefix as the container. Strip only the
            // account configured for this client; application code must not know
            // emulator-specific account names.
            if (IsStorageAccount(containerName, storageAccountName))
            {
                var blobSegments = blobName.Split('/', 2);
                if (blobSegments.Length >= 1)
                {
                    containerName = blobSegments[0];
                    blobName = blobSegments.Length > 1 ? blobSegments[1] : string.Empty;
                }
            }

            if (string.IsNullOrEmpty(containerName) || string.IsNullOrEmpty(blobName))
            {
                throw new ArgumentException($"Could not extract container or blob name from URI: {blobUri}");
            }

            return (containerName, HttpUtility.UrlDecode(blobName));
        }
        catch (Exception)
        {
            // Fallback parsing for non-standard local paths
            var segments = blobUri.Segments.Skip(1).Select(s => s.TrimEnd('/')).ToArray(); // Skip leading '/'

            if (segments.Length < 2)
            {
                throw new ArgumentException($"Invalid blob URL format: {blobUri}");
            }

            var isEmulatorAccount = IsStorageAccount(segments[0], storageAccountName);
            var startIndex = isEmulatorAccount && segments.Length > 2 ? 1 : 0;

            var containerName = segments[startIndex];
            var blobName = string.Join("/", segments.Skip(startIndex + 1));
            blobName = HttpUtility.UrlDecode(blobName);

            return (containerName, blobName);
        }
    }

    private static bool IsStorageAccount(string value, string? storageAccountName) =>
        !string.IsNullOrWhiteSpace(storageAccountName) &&
        string.Equals(value, storageAccountName, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Downloads file from blob storage or accesses local file system as fallback
    /// </summary>
    /// <param name="filePath">The file path or blob URL</param>
    /// <param name="blobStorageService">Blob storage service instance</param>
    /// <param name="logger">Logger instance</param>
    /// <param name="fileType">Type of file being processed (for logging)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Stream containing file data</returns>
    public static async Task<Stream> GetFileStreamAsync(
        string filePath,
        IBlobStorageService blobStorageService,
        ILogger logger,
        FileTypeEnum fileType,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (Uri.TryCreate(filePath, UriKind.Absolute, out var blobUri))
            {
                // Extract container and blob name from the blob URL
                var (containerName, blobName) = ExtractBlobInfo(blobUri, blobStorageService.StorageAccountName);

                logger.LogInformation("Checking if {FileType} file exists in blob storage: container={ContainerName}, blob={BlobName}",
                    fileType, containerName, blobName);

                // Check if the blob exists before attempting to download
                var exists = await blobStorageService.ExistsAsync(containerName, blobName, cancellationToken);
                if (!exists)
                {
                    throw new FileNotFoundException($"{fileType} file not found in blob storage: container={containerName}, blob={blobName}. " +
                        "The file may have been deleted or never uploaded successfully.");
                }

                logger.LogInformation("Downloading {FileType} file from blob storage: container={ContainerName}, blob={BlobName}",
                    fileType, containerName, blobName);

                // Download to MemoryStream to support seeking operations
                using var blobStream = await blobStorageService.DownloadAsync(containerName, blobName, cancellationToken);
                var memoryStream = new MemoryStream();
                await blobStream.CopyToAsync(memoryStream, cancellationToken);
                memoryStream.Position = 0;
                return memoryStream;
            }
            else
            {
                // Fallback for legacy local file paths
                logger.LogWarning("Using legacy file path access for {FileType} file: {FilePath}", fileType, filePath);

                if (!System.IO.File.Exists(filePath))
                {
                    throw new FileNotFoundException($"Legacy {fileType} file not found on disk: {filePath}");
                }

                return new FileStream(filePath, FileMode.Open, FileAccess.Read);
            }
        }
        catch (FileNotFoundException)
        {
            // Re-throw FileNotFoundException as-is
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to access {fileType} file: {filePath}. This could be due to file deletion, network issues, or storage problems.", ex);
        }
    }

    /// <summary>
    /// Marks a file as failed with the specified reason
    /// <summary>
    /// Marks a file as failed with the specified reason
    /// </summary>
    /// <param name="file">File entity to mark as failed</param>
    /// <param name="reason">Reason for failure</param>
    /// <param name="fileRepository">File repository instance</param>
    /// <param name="logger">Logger instance</param>
    /// <param name="fileType">Type of file being processed (for logging)</param>
    /// <param name="errorLogPath">Path/URL to downloadable error log (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public static async Task MarkFileAsFailedAsync(
        Domain.Entities.File file,
        string reason,
        IRepository<Domain.Entities.File> fileRepository,
        ILogger logger,
        FileTypeEnum fileType,
        CancellationToken cancellationToken = default,
        string? errorLogPath = null)
    {
        file.Status = FileStatusEnum.Failed;
        file.ErrorMessage = reason.Length > 2000 ? reason[..1997] + "..." : reason;
        if (!string.IsNullOrEmpty(errorLogPath))
        {
            file.ErrorLogPath = errorLogPath;
        }
        fileRepository.Update(file);
        await fileRepository.SaveChangesAsync(cancellationToken);
        logger.LogWarning("{FileType} file marked as failed: {Reason}", fileType, reason);
    }

    /// <summary>
    /// Terminates processing and marks file as failed
    /// </summary>
    /// <param name="file">File entity to mark as failed</param>
    /// <param name="fileRepository">File repository instance</param>
    /// <param name="logger">Logger instance</param>
    /// <param name="fileType">Type of file being processed (for logging)</param>
    /// <param name="errorMessage">Custom error message (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <param name="errorLogPath">Path/URL to downloadable error log (optional)</param>
    public static async Task TerminateProcessAsync(
        Domain.Entities.File file,
        IRepository<Domain.Entities.File> fileRepository,
        ILogger logger,
        FileTypeEnum fileType,
        string? errorMessage = null,
        CancellationToken cancellationToken = default,
        string? errorLogPath = null)
    {
        file.Status = FileStatusEnum.Failed;
        if (!string.IsNullOrEmpty(errorMessage))
        {
            file.ErrorMessage = errorMessage.Length > 2000 ? errorMessage[..1997] + "..." : errorMessage;
        }
        if (!string.IsNullOrEmpty(errorLogPath))
        {
            file.ErrorLogPath = errorLogPath;
        }
        fileRepository.Update(file);
        await fileRepository.SaveChangesAsync(cancellationToken);

        logger.LogError("=== {FileType} PROCESSING TERMINATED ===", fileType.ToString().ToUpper());

        if (!string.IsNullOrEmpty(errorMessage))
        {
            logger.LogError("{ErrorMessage}", errorMessage);
        }
        else
        {
            logger.LogError("{FileType} process terminated due to error in runtime.", fileType);
        }

        logger.LogError("======================================================");
    }

    /// <summary>
    /// Updates file status to PartiallyProcessed and saves error summary and error log path
    /// </summary>
    public static async Task SetFilePartiallyProcessedStatusAsync(
        Domain.Entities.File file,
        IRepository<Domain.Entities.File> fileRepository,
        ILogger logger,
        FileTypeEnum fileType,
        string? errorMessage = null,
        CancellationToken cancellationToken = default,
        string? errorLogPath = null)
    {
        file.Status = FileStatusEnum.PartiallyProcessed;
        if (!string.IsNullOrEmpty(errorMessage))
        {
            file.ErrorMessage = errorMessage.Length > 2000 ? errorMessage[..1997] + "..." : errorMessage;
        }
        if (!string.IsNullOrEmpty(errorLogPath))
        {
            file.ErrorLogPath = errorLogPath;
        }
        fileRepository.Update(file);
        await fileRepository.SaveChangesAsync(cancellationToken);
        logger.LogWarning("{FileType} file marked as partially processed. Error: {ErrorMessage}", fileType, errorMessage);
    }

    /// <summary>
    /// Reports processing progress
    /// </summary>
    /// <param name="message">Progress message</param>
    /// <param name="logger">Logger instance</param>
    public static void ReportProgress(string message, ILogger logger)
    {
        logger.LogInformation(message);
    }

    /// <summary>
    /// Updates file status to Processing at the beginning of job execution
    /// </summary>
    /// <param name="file">File entity to update</param>
    /// <param name="fileRepository">File repository instance</param>
    /// <param name="logger">Logger instance</param>
    /// <param name="fileType">Type of file being processed (for logging)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public static async Task SetFileProcessingStatusAsync(
        Domain.Entities.File file,
        IRepository<Domain.Entities.File> fileRepository,
        ILogger logger,
        FileTypeEnum fileType,
        CancellationToken cancellationToken = default)
    {
        file.Status = FileStatusEnum.Processing;
        fileRepository.Update(file);
        await fileRepository.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Started processing {FileType} file: {FileName}", fileType, file.Name);
    }

    /// <summary>
    /// Updates file status to Success at the end of job execution
    /// </summary>
    /// <param name="file">File entity to update</param>
    /// <param name="fileRepository">File repository instance</param>
    /// <param name="logger">Logger instance</param>
    /// <param name="fileType">Type of file being processed (for logging)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public static async Task SetFileSuccessStatusAsync(
        Domain.Entities.File file,
        IRepository<Domain.Entities.File> fileRepository,
        ILogger logger,
        FileTypeEnum fileType,
        CancellationToken cancellationToken = default)
    {
        file.Status = FileStatusEnum.Success;
        fileRepository.Update(file);
        await fileRepository.SaveChangesAsync(cancellationToken);
        logger.LogInformation("{FileType} processing completed successfully.", fileType);
    }

    /// <summary>
    /// Validates and retrieves file entity from repository
    /// </summary>
    /// <param name="fileId">File ID to retrieve</param>
    /// <param name="fileRepository">File repository instance</param>
    /// <param name="logger">Logger instance</param>
    /// <param name="fileType">Type of file being processed (for logging)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>File entity if found</returns>
    /// <exception cref="InvalidOperationException">Thrown when file is not found</exception>
    public static async Task<Domain.Entities.File> ValidateAndGetFileAsync(
        Guid fileId,
        IRepository<Domain.Entities.File> fileRepository,
        ILogger logger,
        FileTypeEnum fileType,
        CancellationToken cancellationToken = default)
    {
        var fileQuery = fileRepository.Query(f => f.Id.Equals(fileId));
        var file = await fileRepository.FirstOrDefaultAsync(fileQuery, cancellationToken);

        if (file == null)
        {
            const string message = "File not found.";
            logger.LogError("{FileType} - {Message}", fileType, message);
            throw new InvalidOperationException(message);
        }

        return file;
    }

    /// <summary>
    /// Handles file access errors with consistent logging and error handling
    /// </summary>
    /// <param name="ex">Exception that occurred during file access</param>
    /// <param name="filePath">Path of file that failed to access</param>
    /// <param name="file">File entity to mark as failed</param>
    /// <param name="fileRepository">File repository instance</param>
    /// <param name="logger">Logger instance</param>
    /// <param name="fileType">Type of file being processed (for logging)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public static async Task HandleFileAccessErrorAsync(
        Exception ex,
        string filePath,
        Domain.Entities.File file,
        IRepository<Domain.Entities.File> fileRepository,
        ILogger logger,
        FileTypeEnum fileType,
        CancellationToken cancellationToken = default)
    {
        string reason;

        if (ex is FileNotFoundException)
        {
            logger.LogError(ex, "{FileType} file not found: {FilePath}. Marking file as failed.", fileType, filePath);
            reason = "File not found in storage";
        }
        else
        {
            logger.LogError(ex, "Failed to access {FileType} file: {FilePath}. This could be due to file deletion, network issues, or storage problems.", fileType, filePath);
            reason = $"Failed to access file: {ex.Message}";
        }

        await MarkFileAsFailedAsync(file, reason, fileRepository, logger, fileType, cancellationToken);
    }

    /// <summary>
    /// Invalidates all memory-cache entries tracked under <paramref name="cacheKeysSet"/> and resets the
    /// key-set to an empty set.  Before snapshotting, the tracked key-set is atomically replaced with a fresh
    /// empty <see cref="HashSet{T}"/> so that any concurrent <c>TrackCacheKey()</c> call that runs after the
    /// swap adds to the new set and cannot become orphaned — even if it races with the removal loop.
    /// </summary>
    /// <param name="memoryCache">The <see cref="IMemoryCache"/> instance to invalidate entries in.</param>
    /// <param name="cacheKeysSet">The cache key that stores the <see cref="HashSet{T}"/> of tracked keys.</param>
    /// <param name="logger">Optional logger used to record snapshot failures caused by concurrent mutation.</param>
    /// <returns>
    /// <see langword="true"/> when the cache key-set was absent or invalidated successfully;
    /// <see langword="false"/> when snapshotting failed after all retries and invalidation was skipped.
    /// </returns>
    public static bool InvalidateCacheKeySet(IMemoryCache memoryCache, string cacheKeysSet, ILogger? logger = null)
    {
        if (!memoryCache.TryGetValue<HashSet<string>>(cacheKeysSet, out var oldKeys) || oldKeys == null)
            return true;

        // Atomically swap the tracked key-set to a new empty set BEFORE snapshotting the old one.
        // Any TrackCacheKey() call that runs after this point will add to the new set and will
        // not be orphaned, even if it races with the removal loop below.
        // The 1-hour TTL matches the controllers' TrackCacheKey() convention.
        memoryCache.Set(cacheKeysSet, new HashSet<string>(), TimeSpan.FromHours(1));

        var snapshot = GetCacheKeySnapshot(oldKeys);

        // If snapshotting failed after all retries the old entries remain stale until TTL,
        // but the key-set is now a fresh empty set so future tracking is unaffected.
        if (snapshot == null)
        {
            logger?.LogWarning(
                "Failed to invalidate cache key set '{CacheKeysSet}' because a stable snapshot could not be obtained after retries.",
                cacheKeysSet);
            return false;
        }

        foreach (var key in snapshot)
            memoryCache.Remove(key);

        return true;
    }

    /// <returns>
    /// A stable array snapshot of <paramref name="keys"/>, or <see langword="null"/> if every attempt
    /// was interrupted by a concurrent mutation (all retries exhausted).
    /// </returns>
    private static string[]? GetCacheKeySnapshot(HashSet<string> keys)
    {
        const int maxAttempts = 3;

        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            try
            {
                return keys.ToArray();
            }
            catch (InvalidOperationException) when (attempt < maxAttempts - 1)
            {
                // The tracked key set was modified while being enumerated.
                // Retry to obtain a stable snapshot so cache invalidation does not fail the job.
            }
        }

        // All attempts exhausted — signal failure so the caller can decide not to remove the key-set.
        return null;
    }
}
