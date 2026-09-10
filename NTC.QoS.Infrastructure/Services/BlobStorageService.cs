using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Azure.Storage;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Infrastructure.Configuration;

namespace NTC.QoS.Infrastructure.Services;

/// <summary>
/// Azure Blob Storage service implementation with enhanced region support
/// </summary>
public class BlobStorageService(
    BlobServiceClient blobServiceClient,
    IOptions<BlobStorageOptions> blobStorageOptions,
    ILogger<BlobStorageService> logger,
    StorageSharedKeyCredential? sharedKeyCredential = null) : IBlobStorageService
{
    private readonly BlobServiceClient _blobServiceClient = blobServiceClient;
    private readonly BlobStorageOptions _blobStorageOptions = blobStorageOptions.Value;
    private readonly ILogger<BlobStorageService> _logger = logger;
    private readonly StorageSharedKeyCredential? _sharedKeyCredential = sharedKeyCredential;

    /// <inheritdoc />
    public string? StorageAccountName => _blobServiceClient.AccountName;

    /// <inheritdoc />
    public async Task<string> UploadAsync(string containerName, string fileName, Stream fileStream, string contentType, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting upload of file {FileName} to container {ContainerName}", fileName, containerName);

        // ValidateFileSize(fileStream);
        ValidateContentType(contentType);

        var containerClient = await GetOrCreateContainerAsync(containerName, cancellationToken);
        var blobClient = GetBlobClient(containerClient, containerName, fileName);

        var uploadOptions = CreateUploadOptions(fileName, contentType);

        await blobClient.UploadAsync(fileStream, uploadOptions, cancellationToken);

        var blobUrl = blobClient.Uri.ToString();
        _logger.LogInformation("Successfully uploaded file {FileName} to {BlobUrl}", fileName, blobUrl);

        return blobUrl;
    }

    /// <summary>
    /// Validates the file size against the configured maximum.
    /// </summary>
    private void ValidateFileSize(Stream fileStream)
    {
        long fileSize = 0;
        if (fileStream.CanSeek)
        {
            fileSize = fileStream.Length;
            _logger.LogDebug("File size from stream Length: {FileSize} bytes", fileSize);
        }
        else
        {
            _logger.LogDebug("Stream is not seekable, cannot pre-validate file size");
        }

        var maxFileSizeBytes = (long)_blobStorageOptions.MaxFileSizeMB * 1024 * 1024;
        if (fileSize > 0 && fileSize > maxFileSizeBytes)
        {
            var fileSizeMB = (double)fileSize / (1024 * 1024);
            _logger.LogWarning("File size validation failed: {FileSizeMB:F2}MB exceeds limit of {MaxFileSizeMB}MB", fileSizeMB, _blobStorageOptions.MaxFileSizeMB);
            throw new InvalidOperationException($"File size ({fileSizeMB:F2}MB) exceeds the maximum allowed size of {_blobStorageOptions.MaxFileSizeMB}MB");
        }
    }

    /// <summary>
    /// Validates the content type against allowed types.
    /// </summary>
    private void ValidateContentType(string contentType)
    {
        if (!_blobStorageOptions.AllowedContentTypes.Contains(contentType.ToLowerInvariant()))
        {
            throw new InvalidOperationException($"Content type {contentType} is not allowed");
        }
    }

    /// <summary>
    /// Creates upload options with metadata.
    /// </summary>
    private static BlobUploadOptions CreateUploadOptions(string fileName, string contentType)
    {
        return new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders
            {
                ContentType = contentType
            },
            Metadata = new Dictionary<string, string>
            {
                ["UploadedAt"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                ["OriginalName"] = Path.GetFileName(fileName),
                ["Region"] = ExtractRegionFromPath(fileName),
                ["Environment"] = ExtractEnvironmentFromPath(fileName),
                ["FileType"] = ExtractFileTypeFromPath(fileName)
            }
        };
    }

    /// <summary>
    /// Lists blobs by region and file type
    /// </summary>
    public async Task<IEnumerable<string>> ListBlobsByRegionAsync(string containerName, string region, string? fileType = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var environment = containerName; // Assuming container name matches environment
            var prefix = fileType != null
                ? $"{environment}/{fileType}/{region}/"
                : $"{environment}/";

            return await ListBlobsAsync(containerName, prefix, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list blobs for region {Region} in container {ContainerName}", region, containerName);
            throw;
        }
    }

    /// <summary>
    /// Extracts region information from the blob path
    /// </summary>
    private static string ExtractRegionFromPath(string blobPath)
    {
        var pathParts = blobPath.Split('/');
        var regionPart = pathParts.FirstOrDefault(part => part.StartsWith("region", StringComparison.OrdinalIgnoreCase));
        return regionPart ?? "unknown";
    }

    /// <summary>
    /// Extracts environment information from the blob path
    /// </summary>
    private static string ExtractEnvironmentFromPath(string blobPath)
    {
        var pathParts = blobPath.Split('/');
        return pathParts.Length > 0 ? pathParts[0] : "unknown";
    }

    /// <summary>
    /// Extracts file type information from the blob path
    /// </summary>
    private static string ExtractFileTypeFromPath(string blobPath)
    {
        var pathParts = blobPath.Split('/');
        return pathParts.Length > 1 ? pathParts[1] : "unknown";
    }

    /// <inheritdoc />
    public async Task<Stream> DownloadAsync(string containerName, string fileName, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting download of file {FileName} from container {ContainerName}", fileName, containerName);

            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = GetBlobClient(containerClient, containerName, fileName);

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
            var blobClient = GetBlobClient(containerClient, containerName, fileName);

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
            var blobClient = GetBlobClient(containerClient, containerName, fileName);

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
            var blobClient = GetBlobClient(containerClient, containerName, fileName);

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
        var blobClient = GetBlobClient(containerClient, containerName, fileName);
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

        try
        {
            // Create container if it doesn't exist
            await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
            return containerClient;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create or access blob container {ContainerName}", containerName);
            throw new InvalidOperationException($"Failed to create or access blob container '{containerName}'", ex);
        }
    }

    /// <inheritdoc />
    public string GeneratePreSignedUploadUrl(string containerName, string fileName, TimeSpan? expiryWindow = null)
    {
        var expiry = expiryWindow ?? TimeSpan.FromMinutes(1);
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = GetBlobClient(containerClient, containerName, fileName);

        if (_sharedKeyCredential == null && !blobClient.CanGenerateSasUri)
        {
            _logger.LogError("BlobClient cannot generate SAS URI for container {ContainerName}, blob {FileName}", containerName, fileName);
            throw new InvalidOperationException("BlobClient cannot generate SAS URI with the configured Azure credentials.");
        }

        var sasBuilder = new BlobSasBuilder
        {
            // Keep the SAS resource tied to the caller's logical storage model.
            // GetBlobClient can adapt an endpoint whose path is parsed ambiguously
            // by the SDK, but that URI adaptation must not change the container
            // and blob values used to sign the SAS resource.
            BlobContainerName = containerName,
            BlobName = fileName,
            Resource = "b",
            StartsOn = DateTimeOffset.UtcNow.AddMinutes(-1),
            ExpiresOn = DateTimeOffset.UtcNow.Add(expiry)
        };

        sasBuilder.SetPermissions(BlobSasPermissions.Write | BlobSasPermissions.Create);

        var sasUri = GenerateSasUri(blobClient, sasBuilder);
        _logger.LogInformation("Generated pre-signed upload SAS URL for blob {FileName} in container {ContainerName} (expires in {Expiry})", fileName, containerName, expiry);
        return sasUri.ToString();
    }

    /// <inheritdoc />
    public string GeneratePreSignedDownloadUrl(string containerName, string fileName, TimeSpan? expiryWindow = null)
    {
        var expiry = expiryWindow ?? TimeSpan.FromMinutes(1);
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = GetBlobClient(containerClient, containerName, fileName);

        if (_sharedKeyCredential == null && !blobClient.CanGenerateSasUri)
        {
            _logger.LogError("BlobClient cannot generate SAS URI for container {ContainerName}, blob {FileName}", containerName, fileName);
            throw new InvalidOperationException("BlobClient cannot generate SAS URI with the configured Azure credentials.");
        }

        var sasBuilder = new BlobSasBuilder
        {
            // See GeneratePreSignedUploadUrl: do not derive the signed resource
            // from BlobClient properties after endpoint-specific URI adaptation.
            BlobContainerName = containerName,
            BlobName = fileName,
            Resource = "b",
            StartsOn = DateTimeOffset.UtcNow.AddMinutes(-1),
            ExpiresOn = DateTimeOffset.UtcNow.Add(expiry)
        };

        sasBuilder.SetPermissions(BlobSasPermissions.Read);

        var sasUri = GenerateSasUri(blobClient, sasBuilder);
        _logger.LogInformation("Generated pre-signed download SAS URL for blob {FileName} in container {ContainerName} (expires in {Expiry})", fileName, containerName, expiry);
        return sasUri.ToString();
    }

    private Uri GenerateSasUri(BlobClient blobClient, BlobSasBuilder sasBuilder)
    {
        if (_sharedKeyCredential == null)
        {
            return blobClient.GenerateSasUri(sasBuilder);
        }

        // Generate the signature directly from the logical resource. This avoids
        // BlobClient.GenerateSasUri validating against an endpoint-specific URI
        // parse that can misidentify the account path prefix as a container.
        var uriBuilder = new UriBuilder(blobClient.Uri)
        {
            Query = sasBuilder.ToSasQueryParameters(_sharedKeyCredential).ToString()
        };

        return uriBuilder.Uri;
    }

    /// <summary>
    /// Gets a BlobClient, correctly resolving paths for custom domain emulators (e.g. https://blob.qos.ntc.gov.ph/devstoreaccount1)
    /// where Azure SDK's BlobUriBuilder treats the custom domain as cloud Azure and parses the first path segment
    /// as the container name instead of the emulator account name.
    /// </summary>
    private static BlobClient GetBlobClient(BlobContainerClient containerClient, string containerName, string fileName)
    {
        if (!string.Equals(containerClient.Name, containerName, StringComparison.OrdinalIgnoreCase))
        {
            return containerClient.GetBlobClient($"{containerName}/{fileName}");
        }

        return containerClient.GetBlobClient(fileName);
    }
}
