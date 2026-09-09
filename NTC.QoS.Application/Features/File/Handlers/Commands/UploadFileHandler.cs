using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NTC.QoS.Application.Features.File.Requests;
using NTC.QoS.Application.Helpers;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.File;

namespace NTC.QoS.Application.Features.File.Handlers.Commands;

public class UploadFileHandler(
    IFileRepository repository,
    IBlobStorageService blobStorageService,
    IUserContextService userContextService,
    IUserAuthorizationService userAuthorizationService,
    IRegionRepository regionRepository,
    IConfiguration configuration,
    ILogger<UploadFileHandler> logger) : IRequestHandler<UploadFileRequest, ApiResponse<FileDto>>
{
    private readonly IFileRepository _repository = repository;
    private readonly IBlobStorageService _blobStorageService = blobStorageService;
    private readonly IUserContextService _userContextService = userContextService;
    private readonly IUserAuthorizationService _userAuthorizationService = userAuthorizationService;
    private readonly IRegionRepository _regionRepository = regionRepository;
    private readonly IConfiguration _configuration = configuration;
    private readonly ILogger<UploadFileHandler> _logger = logger;

    public async Task<ApiResponse<FileDto>> Handle(UploadFileRequest request, CancellationToken cancellationToken)
    {
        try
        {
            FileTypeEnum fileType = Enum.TryParse<FileTypeEnum>(request.File.Type, out var parsedType) ? parsedType : FileTypeEnum.Unknown;
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            string originalFileName = request.File.FileName ?? request.File.File.FileName;
            string fileExtension = Path.GetExtension(originalFileName);
            string fileGuid = Guid.NewGuid().ToString("N");
            string newFileName = $"{timestamp}_{fileGuid}{fileExtension}";

            _logger.LogInformation("Processing file upload - Original: {OriginalFileName}, New: {NewFileName}, ContentType: {ContentType}, Size: {FileSize} bytes",
                originalFileName, newFileName, request.File.File.ContentType, request.File.File.Length);

            // Get region information for the current user
            var regionCode = await GetRegionCodeAsync(request.File.RegionId, cancellationToken);
            if (string.IsNullOrEmpty(regionCode))
            {
                return new ApiResponse<FileDto>
                {
                    Message = "Invalid region or user does not have access to the specified region.",
                    Succeeded = false,
                    Errors = new Dictionary<string, string[]>
                    {
                        { "Region", ["Invalid region or insufficient region access."]}
                    }
                };
            }

            // Determine container name based on environment
            string containerName = BlobStoragePathHelper.GetContainerName(_configuration["ASPNETCORE_ENVIRONMENT"]);

            // Create blob path with region-based folder structure
            string blobPath = BlobStoragePathHelper.GetBlobPathWithRegion(fileType, regionCode, newFileName);

            _logger.LogInformation("Uploading file {OriginalFileName} as {BlobPath} to container {ContainerName} for region {RegionCode}",
                originalFileName, blobPath, containerName, regionCode);

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

            var currentUserId = _userContextService.GetCurrentUserId();
            var uploadedBy = currentUserId ?? "System";

            var newFile = new Domain.Entities.File
            {
                Name = originalFileName,
                Type = fileType,
                Path = blobUrl,
                Status = FileStatusEnum.Pending,
                UploadedBy = uploadedBy,
                UploadDate = DateTime.UtcNow,
                RegionId = request.File.RegionId
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
                Status = newFile.Status.ToString(),
                RegionId = newFile.RegionId
            };

            return new ApiResponse<FileDto>
            {
                Data = newFileDto,
                Message = $"File uploaded successfully to blob storage in region {regionCode} and queued for processing.",
                Succeeded = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file {FileName}", request.File.FileName ?? request.File.File.FileName);

            // Extract the innermost exception message (database errors are often nested)
            var errorMessage = GetInnermostExceptionMessage(ex);
            
            return new ApiResponse<FileDto>
            {
                Message = $"Error uploading file: {errorMessage}",
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    { "Upload", new[] { errorMessage } }
                }
            };
        }
    }

    /// <summary>
    /// Recursively gets the innermost exception message
    /// </summary>
    private static string GetInnermostExceptionMessage(Exception ex)
    {
        var current = ex;
        while (current.InnerException != null)
        {
            current = current.InnerException;
        }
        return current.Message;
    }

    /// <summary>
    /// Gets the region code for the specified region ID and validates user access
    /// </summary>
    private async Task<string?> GetRegionCodeAsync(Guid regionId, CancellationToken cancellationToken)
    {
        // Verify user has access to the specified region
        var hasAccess = await _userAuthorizationService.HasRegionAccessAsync(regionId, cancellationToken);
        if (!hasAccess)
        {
            _logger.LogWarning("User does not have access to region: {RegionId}", regionId);
            throw new UnauthorizedAccessException("User does not have access to the specified region.");
        }

        // Get region details
        var regionQuery = _regionRepository.Query(r => r.Id == regionId).Select(r => new { r.Id, r.Code });
        var region = await _regionRepository.FirstOrDefaultAsync(regionQuery, cancellationToken);

        if (region is null)
        {
            _logger.LogWarning("Region not found: {RegionId}", regionId);
            return null;
        }

        return BlobStoragePathHelper.SanitizeRegionCode(region.Code);
    }
}
