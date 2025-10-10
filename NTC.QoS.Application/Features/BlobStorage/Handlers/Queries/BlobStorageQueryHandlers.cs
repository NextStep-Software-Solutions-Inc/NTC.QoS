using MediatR;
using Microsoft.Extensions.Logging;
using NTC.QoS.Application.Features.BlobStorage.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.BlobStorage;

namespace NTC.QoS.Application.Features.BlobStorage.Handlers.Queries;

/// <summary>
/// Handler for downloading blobs from Azure Blob Storage
/// </summary>
public class DownloadBlobHandler : IRequestHandler<DownloadBlobRequest, ApiResponse<BlobDownloadDto>>
{
    private readonly IBlobStorageService _blobStorageService;
    private readonly ILogger<DownloadBlobHandler> _logger;

    public DownloadBlobHandler(IBlobStorageService blobStorageService, ILogger<DownloadBlobHandler> logger)
    {
        _blobStorageService = blobStorageService;
        _logger = logger;
    }

    public async Task<ApiResponse<BlobDownloadDto>> Handle(DownloadBlobRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Processing blob download request for {FileName} from container {ContainerName}", 
                request.FileName, request.ContainerName);

            var fileStream = await _blobStorageService.DownloadAsync(
                request.ContainerName,
                request.FileName,
                cancellationToken);

            var response = new BlobDownloadDto
            {
                FileStream = fileStream,
                FileName = request.FileName,
                ContentType = "application/octet-stream", // Default, could be enhanced to get actual content type
                FileSize = fileStream.Length,
                LastModified = DateTime.UtcNow // Could be enhanced to get actual last modified date
            };

            _logger.LogInformation("Successfully downloaded blob {FileName} from container {ContainerName}", 
                request.FileName, request.ContainerName);

            return new ApiResponse<BlobDownloadDto>
            {
                Data = response,
                Message = "Blob downloaded successfully",
                Succeeded = true
            };
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogWarning("Blob {FileName} not found in container {ContainerName}", 
                request.FileName, request.ContainerName);

            return new ApiResponse<BlobDownloadDto>
            {
                Message = "Blob not found",
                Succeeded = false
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download blob {FileName} from container {ContainerName}", 
                request.FileName, request.ContainerName);

            return new ApiResponse<BlobDownloadDto>
            {
                Message = $"Failed to download blob: {ex.Message}",
                Succeeded = false
            };
        }
    }
}

/// <summary>
/// Handler for downloading blobs as byte array from Azure Blob Storage
/// </summary>
public class DownloadBlobAsBytesHandler : IRequestHandler<DownloadBlobAsBytesRequest, ApiResponse<byte[]>>
{
    private readonly IBlobStorageService _blobStorageService;
    private readonly ILogger<DownloadBlobAsBytesHandler> _logger;

    public DownloadBlobAsBytesHandler(IBlobStorageService blobStorageService, ILogger<DownloadBlobAsBytesHandler> logger)
    {
        _blobStorageService = blobStorageService;
        _logger = logger;
    }

    public async Task<ApiResponse<byte[]>> Handle(DownloadBlobAsBytesRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Processing blob download as bytes request for {FileName} from container {ContainerName}", 
                request.FileName, request.ContainerName);

            var bytes = await _blobStorageService.DownloadAsBytesAsync(
                request.ContainerName,
                request.FileName,
                cancellationToken);

            _logger.LogInformation("Successfully downloaded blob {FileName} as bytes from container {ContainerName}", 
                request.FileName, request.ContainerName);

            return new ApiResponse<byte[]>
            {
                Data = bytes,
                Message = "Blob downloaded successfully",
                Succeeded = true
            };
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogWarning("Blob {FileName} not found in container {ContainerName}", 
                request.FileName, request.ContainerName);

            return new ApiResponse<byte[]>
            {
                Message = "Blob not found",
                Succeeded = false
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download blob {FileName} as bytes from container {ContainerName}", 
                request.FileName, request.ContainerName);

            return new ApiResponse<byte[]>
            {
                Message = $"Failed to download blob: {ex.Message}",
                Succeeded = false
            };
        }
    }
}

/// <summary>
/// Handler for checking if a blob exists in Azure Blob Storage
/// </summary>
public class BlobExistsHandler : IRequestHandler<BlobExistsRequest, ApiResponse<bool>>
{
    private readonly IBlobStorageService _blobStorageService;
    private readonly ILogger<BlobExistsHandler> _logger;

    public BlobExistsHandler(IBlobStorageService blobStorageService, ILogger<BlobExistsHandler> logger)
    {
        _blobStorageService = blobStorageService;
        _logger = logger;
    }

    public async Task<ApiResponse<bool>> Handle(BlobExistsRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Checking if blob {FileName} exists in container {ContainerName}", 
                request.FileName, request.ContainerName);

            var exists = await _blobStorageService.ExistsAsync(
                request.ContainerName,
                request.FileName,
                cancellationToken);

            _logger.LogInformation("Blob {FileName} existence check result: {Exists}", request.FileName, exists);

            return new ApiResponse<bool>
            {
                Data = exists,
                Message = exists ? "Blob exists" : "Blob does not exist",
                Succeeded = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if blob {FileName} exists in container {ContainerName}", 
                request.FileName, request.ContainerName);

            return new ApiResponse<bool>
            {
                Message = $"Failed to check blob existence: {ex.Message}",
                Succeeded = false
            };
        }
    }
}

/// <summary>
/// Handler for listing blobs in a container
/// </summary>
public class ListBlobsHandler : IRequestHandler<ListBlobsRequest, ApiResponse<IEnumerable<BlobInfoDto>>>
{
    private readonly IBlobStorageService _blobStorageService;
    private readonly ILogger<ListBlobsHandler> _logger;

    public ListBlobsHandler(IBlobStorageService blobStorageService, ILogger<ListBlobsHandler> logger)
    {
        _blobStorageService = blobStorageService;
        _logger = logger;
    }

    public async Task<ApiResponse<IEnumerable<BlobInfoDto>>> Handle(ListBlobsRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Listing blobs in container {ContainerName} with prefix {Prefix}", 
                request.ContainerName, request.Prefix ?? "none");

            var blobNames = await _blobStorageService.ListBlobsAsync(
                request.ContainerName,
                request.Prefix,
                cancellationToken);

            var blobInfos = blobNames.Select(name => new BlobInfoDto
            {
                Name = name,
                ContainerName = request.ContainerName,
                Url = _blobStorageService.GetBlobUrl(request.ContainerName, name),
                // Additional properties could be populated by extending the service
                CreatedOn = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            });

            _logger.LogInformation("Successfully listed {Count} blobs in container {ContainerName}", 
                blobInfos.Count(), request.ContainerName);

            return new ApiResponse<IEnumerable<BlobInfoDto>>
            {
                Data = blobInfos,
                Message = "Blobs listed successfully",
                Succeeded = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list blobs in container {ContainerName}", request.ContainerName);

            return new ApiResponse<IEnumerable<BlobInfoDto>>
            {
                Message = $"Failed to list blobs: {ex.Message}",
                Succeeded = false
            };
        }
    }
}