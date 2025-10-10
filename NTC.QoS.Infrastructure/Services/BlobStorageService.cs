using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Infrastructure.Configuration;

namespace NTC.QoS.Infrastructure.Services;

/// <summary>
/// Azure Blob Storage service implementation
/// </summary>
public class BlobStorageService : IBlobStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly BlobStorageOptions _options;
    private readonly ILogger<BlobStorageService> _logger;

    public BlobStorageService(
        BlobServiceClient blobServiceClient,
        IOptions<BlobStorageOptions> options,
        ILogger<BlobStorageService> logger)
    {
        _blobServiceClient = blobServiceClient;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<string> UploadAsync(string containerName, string fileName, Stream fileStream, string contentType, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting upload of file {FileName} to container {ContainerName}", fileName, containerName);

            // Validate file size
            if (fileStream.Length > _options.MaxFileSizeMB * 1024 * 1024)
            {
                throw new InvalidOperationException($"File size exceeds the maximum allowed size of {_options.MaxFileSizeMB}MB");
            }

            // Validate content type
            if (!_options.AllowedContentTypes.Contains(contentType.ToLowerInvariant()))
            {
                throw new InvalidOperationException($"Content type {contentType} is not allowed");
            }

            // Get or create container
            var containerClient = await GetOrCreateContainerAsync(containerName, cancellationToken);

            // Upload blob
            var blobClient = containerClient.GetBlobClient(fileName);
            
            var uploadOptions = new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = contentType
                },
                Metadata = new Dictionary<string, string>
                {
                    ["UploadedAt"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    ["OriginalName"] = fileName
                }
            };

            await blobClient.UploadAsync(fileStream, uploadOptions, cancellationToken);

            var blobUrl = blobClient.Uri.ToString();
            _logger.LogInformation("Successfully uploaded file {FileName} to {BlobUrl}", fileName, blobUrl);

            return blobUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload file {FileName} to container {ContainerName}", fileName, containerName);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<Stream> DownloadAsync(string containerName, string fileName, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting download of file {FileName} from container {ContainerName}", fileName, containerName);

            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(fileName);

            if (!await blobClient.ExistsAsync(cancellationToken))
            {
                throw new FileNotFoundException($"Blob {fileName} not found in container {containerName}");
            }

            var response = await blobClient.DownloadStreamingAsync(cancellationToken: cancellationToken);
            
            _logger.LogInformation("Successfully downloaded file {FileName} from container {ContainerName}", fileName, containerName);
            
            return response.Value.Content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download file {FileName} from container {ContainerName}", fileName, containerName);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<byte[]> DownloadAsBytesAsync(string containerName, string fileName, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting download of file {FileName} as bytes from container {ContainerName}", fileName, containerName);

            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(fileName);

            if (!await blobClient.ExistsAsync(cancellationToken))
            {
                throw new FileNotFoundException($"Blob {fileName} not found in container {containerName}");
            }

            var response = await blobClient.DownloadContentAsync(cancellationToken);
            
            _logger.LogInformation("Successfully downloaded file {FileName} as bytes from container {ContainerName}", fileName, containerName);
            
            return response.Value.Content.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download file {FileName} as bytes from container {ContainerName}", fileName, containerName);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(string containerName, string fileName, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting deletion of file {FileName} from container {ContainerName}", fileName, containerName);

            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(fileName);

            var response = await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
            
            if (response.Value)
            {
                _logger.LogInformation("Successfully deleted file {FileName} from container {ContainerName}", fileName, containerName);
            }
            else
            {
                _logger.LogWarning("File {FileName} was not found in container {ContainerName} for deletion", fileName, containerName);
            }

            return response.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete file {FileName} from container {ContainerName}", fileName, containerName);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(string containerName, string fileName, CancellationToken cancellationToken = default)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(fileName);

            var response = await blobClient.ExistsAsync(cancellationToken);
            return response.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check existence of file {FileName} in container {ContainerName}", fileName, containerName);
            throw;
        }
    }

    /// <inheritdoc />
    public string GetBlobUrl(string containerName, string fileName)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(fileName);
        return blobClient.Uri.ToString();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<string>> ListBlobsAsync(string containerName, string? prefix = null, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Listing blobs in container {ContainerName} with prefix {Prefix}", containerName, prefix ?? "none");

            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            
            if (!await containerClient.ExistsAsync(cancellationToken))
            {
                _logger.LogWarning("Container {ContainerName} does not exist", containerName);
                return Enumerable.Empty<string>();
            }

            var blobs = new List<string>();
            
            await foreach (var blobItem in containerClient.GetBlobsAsync(prefix: prefix, cancellationToken: cancellationToken))
            {
                blobs.Add(blobItem.Name);
            }

            _logger.LogInformation("Found {Count} blobs in container {ContainerName}", blobs.Count, containerName);
            return blobs;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list blobs in container {ContainerName}", containerName);
            throw;
        }
    }

    /// <summary>
    /// Gets or creates a blob container with the specified access level
    /// </summary>
    /// <param name="containerName">The container name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The blob container client</returns>
    private async Task<BlobContainerClient> GetOrCreateContainerAsync(string containerName, CancellationToken cancellationToken)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);

        // Create container if it doesn't exist
        var response = await containerClient.CreateIfNotExistsAsync(
            publicAccessType: GetPublicAccessType(_options.AccessLevel),
            cancellationToken: cancellationToken);

        if (response != null)
        {
            _logger.LogInformation("Created new blob container: {ContainerName}", containerName);
        }

        return containerClient;
    }

    /// <summary>
    /// Converts string access level to PublicAccessType enum
    /// </summary>
    /// <param name="accessLevel">The access level string</param>
    /// <returns>The corresponding PublicAccessType</returns>
    private static PublicAccessType GetPublicAccessType(string accessLevel)
    {
        return accessLevel.ToLowerInvariant() switch
        {
            "blob" => PublicAccessType.Blob,
            "container" => PublicAccessType.BlobContainer,
            _ => PublicAccessType.None
        };
    }
}