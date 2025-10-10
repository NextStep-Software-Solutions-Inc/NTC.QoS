using MediatR;
using Microsoft.Extensions.Logging;
using NTC.QoS.Application.Features.BlobStorage.Requests;
using NTC.QoS.Application.Features.File.Requests;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.File;

namespace NTC.QoS.Application.Features.File.Handlers.Commands;

/// <summary>
/// Enhanced file upload handler that uses Azure Blob Storage
/// </summary>
public class UploadFileToBlobHandler : IRequestHandler<UploadFileRequest, ApiResponse<FileDto>>
{
    private readonly IFileRepository _repository;
    private readonly IMediator _mediator;
    private readonly ILogger<UploadFileToBlobHandler> _logger;

    public UploadFileToBlobHandler(
        IFileRepository repository,
        IMediator mediator,
        ILogger<UploadFileToBlobHandler> logger)
    {
        _repository = repository;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<ApiResponse<FileDto>> Handle(UploadFileRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var fileType = EnumHelper<FileTypeEnum>.GetValue(request.File.Type);
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            string originalFileName = request.File.FileName ?? request.File.File.FileName;
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(originalFileName).Replace(" ", "-");
            string fileExtension = Path.GetExtension(originalFileName);
            string newFileName = $"{fileNameWithoutExtension}_{timestamp}{fileExtension}";

            // Upload to Azure Blob Storage
            using var fileStream = request.File.File.OpenReadStream();
            
            var blobUploadRequest = new UploadBlobRequest
            {
                ContainerName = request.File.Type.ToLowerInvariant(), // Use file type as container
                FileName = newFileName,
                FileStream = fileStream,
                ContentType = request.File.File.ContentType
            };

            var blobUploadResult = await _mediator.Send(blobUploadRequest, cancellationToken);

            if (!blobUploadResult.Succeeded || blobUploadResult.Data == null)
            {
                _logger.LogError("Failed to upload file to blob storage: {Message}", blobUploadResult.Message);
                return new ApiResponse<FileDto>
                {
                    Message = "Failed to upload file to storage",
                    Succeeded = false
                };
            }

            // Save file metadata to database with blob URL
            var newFile = new Domain.Entities.File
            {
                Name = newFileName,
                Type = fileType,
                Path = blobUploadResult.Data.BlobUrl, // Store blob URL instead of local path
                Status = FileStatusEnum.Pending,
            };

            _repository.Add(newFile);
            await _repository.SaveChangesAsync(cancellationToken);

            var newFileDto = new FileDto
            {
                Id = newFile.Id,
                Name = newFile.Name,
                Path = newFile.Path,
                UploadedBy = newFile.UploadedBy,
                UploadDate = newFile.UploadDate,
                Type = newFile.Type.ToString() ?? string.Empty,
                Status = newFile.Status.ToString()
            };

            _logger.LogInformation("Successfully uploaded file {FileName} to blob storage with URL {BlobUrl}", 
                newFileName, blobUploadResult.Data.BlobUrl);

            return new ApiResponse<FileDto>
            {
                Data = newFileDto,
                Message = "File uploaded to cloud storage and queued for processing.",
                Succeeded = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while uploading file to blob storage");
            return new ApiResponse<FileDto>
            {
                Message = "An error occurred while uploading the file",
                Succeeded = false
            };
        }
    }
}

/// <summary>
/// Enhanced file download handler that retrieves from Azure Blob Storage
/// </summary>
public class DownloadFileFromBlobHandler : IRequestHandler<DownloadFileRequest, ApiResponse<FileDto>>
{
    private readonly IFileRepository _repository;
    private readonly IMediator _mediator;
    private readonly ILogger<DownloadFileFromBlobHandler> _logger;

    public DownloadFileFromBlobHandler(
        IFileRepository repository,
        IMediator mediator,
        ILogger<DownloadFileFromBlobHandler> logger)
    {
        _repository = repository;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<ApiResponse<FileDto>> Handle(DownloadFileRequest request, CancellationToken cancellationToken)
    {
        try
        {
            // Get file metadata from database
            var file = await _repository.GetByIdAsync(request.FileId, cancellationToken);
            if (file == null)
            {
                return new ApiResponse<FileDto>
                {
                    Message = "File not found",
                    Succeeded = false
                };
            }

            // If path is a blob URL, extract container and blob name
            if (Uri.TryCreate(file.Path, UriKind.Absolute, out var blobUri))
            {
                var pathSegments = blobUri.AbsolutePath.Trim('/').Split('/');
                if (pathSegments.Length >= 2)
                {
                    var containerName = pathSegments[0];
                    var blobName = string.Join("/", pathSegments.Skip(1));

                    // Check if blob exists
                    var existsRequest = new BlobExistsRequest
                    {
                        ContainerName = containerName,
                        FileName = blobName
                    };

                    var existsResult = await _mediator.Send(existsRequest, cancellationToken);
                    if (!existsResult.Succeeded || !existsResult.Data)
                    {
                        return new ApiResponse<FileDto>
                        {
                            Message = "File not found in storage",
                            Succeeded = false
                        };
                    }
                }
            }

            var fileDto = new FileDto
            {
                Id = file.Id,
                Name = file.Name,
                Path = file.Path,
                UploadedBy = file.UploadedBy,
                UploadDate = file.UploadDate,
                Type = file.Type.ToString(),
                Status = file.Status.ToString()
            };

            return new ApiResponse<FileDto>
            {
                Data = fileDto,
                Message = "File information retrieved successfully",
                Succeeded = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving file information for file ID {FileId}", request.FileId);
            return new ApiResponse<FileDto>
            {
                Message = "An error occurred while retrieving the file",
                Succeeded = false
            };
        }
    }
}

// Request class for the download handler (if it doesn't exist)
public class DownloadFileRequest : IRequest<ApiResponse<FileDto>>
{
    public Guid FileId { get; set; }
}