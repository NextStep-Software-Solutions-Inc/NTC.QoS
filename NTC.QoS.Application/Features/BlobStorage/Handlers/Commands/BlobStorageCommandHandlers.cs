using MediatR;
using Microsoft.Extensions.Logging;
using NTC.QoS.Application.Features.BlobStorage.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.BlobStorage;

namespace NTC.QoS.Application.Features.BlobStorage.Handlers.Commands;

/// <summary>
/// Handler for uploading blobs to Azure Blob Storage
/// </summary>
public class UploadBlobHandler : IRequestHandler<UploadBlobRequest, ApiResponse<BlobUploadResponseDto>>
{
    private readonly IBlobStorageService _blobStorageService;
    private readonly ILogger<UploadBlobHandler> _logger;

    public UploadBlobHandler(IBlobStorageService blobStorageService, ILogger<UploadBlobHandler> logger)
    {
        _blobStorageService = blobStorageService;
        _logger = logger;
    }

    public async Task<ApiResponse<BlobUploadResponseDto>> Handle(UploadBlobRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Processing blob upload request for {FileName} to container {ContainerName}", 
                request.FileName, request.ContainerName);

            var blobUrl = await _blobStorageService.UploadAsync(
                request.ContainerName,
                request.FileName,
                request.FileStream,
                request.ContentType,
                cancellationToken);

            var response = new BlobUploadResponseDto
            {
                BlobUrl = blobUrl,
                ContainerName = request.ContainerName,
                BlobName = request.FileName,
                UploadedAt = DateTime.UtcNow
            };

            _logger.LogInformation("Successfully uploaded blob {FileName} to {BlobUrl}", request.FileName, blobUrl);

            return new ApiResponse<BlobUploadResponseDto>
            {
                Data = response,
                Message = "Blob uploaded successfully",
                Succeeded = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload blob {FileName} to container {ContainerName}", 
                request.FileName, request.ContainerName);

            return new ApiResponse<BlobUploadResponseDto>
            {
                Message = $"Failed to upload blob: {ex.Message}",
                Succeeded = false
            };
        }
    }
}

/// <summary>
/// Handler for deleting blobs from Azure Blob Storage
/// </summary>
public class DeleteBlobHandler : IRequestHandler<DeleteBlobRequest, ApiResponse<bool>>
{
    private readonly IBlobStorageService _blobStorageService;
    private readonly ILogger<DeleteBlobHandler> _logger;

    public DeleteBlobHandler(IBlobStorageService blobStorageService, ILogger<DeleteBlobHandler> logger)
    {
        _blobStorageService = blobStorageService;
        _logger = logger;
    }

    public async Task<ApiResponse<bool>> Handle(DeleteBlobRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Processing blob deletion request for {FileName} from container {ContainerName}", 
                request.FileName, request.ContainerName);

            var deleted = await _blobStorageService.DeleteAsync(
                request.ContainerName,
                request.FileName,
                cancellationToken);

            var message = deleted 
                ? "Blob deleted successfully" 
                : "Blob was not found";

            _logger.LogInformation("Blob deletion result for {FileName}: {Result}", request.FileName, deleted);

            return new ApiResponse<bool>
            {
                Data = deleted,
                Message = message,
                Succeeded = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete blob {FileName} from container {ContainerName}", 
                request.FileName, request.ContainerName);

            return new ApiResponse<bool>
            {
                Message = $"Failed to delete blob: {ex.Message}",
                Succeeded = false
            };
        }
    }
}