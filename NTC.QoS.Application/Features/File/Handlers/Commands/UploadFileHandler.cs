using MediatR;
using NTC.QoS.Application.Features.File.Requests;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.File;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace NTC.QoS.Application.Features.File.Handlers.Commands;

public class UploadFileHandler(
    IFileRepository repository,
    IBlobStorageService blobStorageService,
    IConfiguration configuration,
    ILogger<UploadFileHandler> logger) : IRequestHandler<UploadFileRequest, ApiResponse<FileDto>>
{
    private readonly IFileRepository _repository = repository;
    private readonly IBlobStorageService _blobStorageService = blobStorageService;
    private readonly IConfiguration _configuration = configuration;
    private readonly ILogger<UploadFileHandler> _logger = logger;

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

            // Log file information for debugging
            _logger.LogInformation("Processing file upload - Original: {OriginalFileName}, New: {NewFileName}, ContentType: {ContentType}, Size: {FileSize} bytes", 
                originalFileName, newFileName, request.File.File.ContentType, request.File.File.Length);

            // Determine container name based on environment
            string containerName = GetContainerName();
            
            // Create blob path with file type prefix
            string blobPath = GetBlobPath(request.File.Type, newFileName);
            
            _logger.LogInformation("Uploading file {OriginalFileName} as {BlobPath} to container {ContainerName}", 
                originalFileName, blobPath, containerName);

            // Upload file to blob storage
            using var fileStream = request.File.File.OpenReadStream();
            
            // Reset stream position if possible
            if (fileStream.CanSeek)
            {
                fileStream.Position = 0;
                _logger.LogDebug("Stream position reset to 0, stream length: {StreamLength}", fileStream.Length);
            }
            
            string blobUrl = await _blobStorageService.UploadAsync(
                containerName,
                blobPath,
                fileStream,
                request.File.File.ContentType,
                cancellationToken);

            _logger.LogInformation("Successfully uploaded file to blob storage. URL: {BlobUrl}", blobUrl);

            // Create file entity with blob URL as path
            var newFile = new Domain.Entities.File
            {
                Name = newFileName,
                Type = fileType,
                Path = blobUrl, // Store the blob URL instead of local path
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

            return new ApiResponse<FileDto>
            {
                Data = newFileDto,
                Message = "File uploaded successfully to blob storage and queued for processing.",
                Succeeded = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file {FileName}", request.File.FileName ?? request.File.File.FileName);
            
            return new ApiResponse<FileDto>
            {
                Message = $"Error uploading file: {ex.Message}",
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    { "Upload", new[] { ex.Message } }
                }
            };
        }
    }

    /// <summary>
    /// Gets the appropriate container name based on environment
    /// </summary>
    private string GetContainerName()
    {
        var environment = _configuration["ASPNETCORE_ENVIRONMENT"];
        return environment?.ToLowerInvariant() switch
        {
            "production" => "production",
            "staging" => "production", // Use production container for staging
            _ => "development" // Default to development for dev, test, etc.
        };
    }

    /// <summary>
    /// Creates the blob path with file type prefix
    /// </summary>
    private static string GetBlobPath(string fileType, string fileName)
    {
        // Create subdirectory based on file type
        var fileTypePrefix = fileType.ToLowerInvariant() switch
        {
            "cell" => "cells",
            "nap" => "naps", 
            "speedtest" => "speedtests",
            "cellular" => "cellulars",
            "cellularcoverage" => "cellular-coverages",
            "wirelessvalidation" => "wireless-validations",
            _ => "uploads" // Default folder for unknown types
        };

        return $"{fileTypePrefix}/{fileName}";
    }
}
