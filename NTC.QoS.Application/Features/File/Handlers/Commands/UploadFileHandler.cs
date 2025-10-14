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
            var fileType = EnumHelper<FileTypeEnum>.GetValue(request.File.Type);
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            string originalFileName = request.File.FileName ?? request.File.File.FileName;
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(originalFileName).Replace(" ", "-");
            string fileExtension = Path.GetExtension(originalFileName);
            string newFileName = $"{fileNameWithoutExtension}_{timestamp}{fileExtension}";

            _logger.LogInformation("Processing file upload - Original: {OriginalFileName}, New: {NewFileName}, ContentType: {ContentType}, Size: {FileSize} bytes", 
                originalFileName, newFileName, request.File.File.ContentType, request.File.File.Length);

            // Get region information for the current user
            var regionCode = await GetUserRegionCodeAsync(request.File.RegionId, cancellationToken);
            if (string.IsNullOrEmpty(regionCode))
            {
                return new ApiResponse<FileDto>
                {
                    Message = "Invalid region or user does not have access to the specified region.",
                    Succeeded = false,
                    Errors = new Dictionary<string, string[]>
                    {
                        { "Region", new[] { "Invalid region or insufficient region access." } }
                    }
                };
            }

            // Determine container name based on environment
            string containerName = GetContainerName();
            
            // Create blob path with region-based folder structure
            string blobPath = GetBlobPathWithRegion(request.File.Type, regionCode, newFileName);
            
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
                Name = newFileName,
                Type = fileType,
                Path = blobUrl,
                Status = FileStatusEnum.Pending,
                UploadedBy = uploadedBy,
                UploadDate = DateTime.UtcNow
                // Store region information if you have a RegionId field in File entity
                // RegionId = request.File.RegionId
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
                Message = $"File uploaded successfully to blob storage in region {regionCode} and queued for processing.",
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
    /// Creates the blob path with region-based folder structure
    /// Format: environment/file-type/regionXX/filename.xlsx
    /// Example: development/cells/region10/cell_data_20241014123456.xlsx
    /// </summary>
    private string GetBlobPathWithRegion(string fileType, string regionCode, string fileName)
    {
        var environment = GetContainerName();
        
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

        // Format: environment/file-type/regionXX/filename
        return $"{environment}/{fileTypePrefix}/{regionCode}/{fileName}";
    }

    /// <summary>
    /// Gets the region code for the specified region ID and validates user access
    /// </summary>
    private async Task<string?> GetUserRegionCodeAsync(Guid? regionId, CancellationToken cancellationToken)
    {
        // If no region ID specified, try to get the user's primary region
        if (!regionId.HasValue)
        {
            var accessibleRegions = await _userAuthorizationService.GetAccessibleRegionsAsync(cancellationToken);
            if (accessibleRegions.Count == 0)
            {
                _logger.LogWarning("User has no accessible regions");
                return null;
            }
            
            // Use the first accessible region as default
            regionId = accessibleRegions.First();
            _logger.LogInformation("No region specified, using first accessible region: {RegionId}", regionId);
        }

        // Verify user has access to the specified region
        var hasAccess = await _userAuthorizationService.HasRegionAccessAsync(regionId.Value, cancellationToken);
        if (!hasAccess)
        {
            _logger.LogWarning("User does not have access to region: {RegionId}", regionId);
            return null;
        }

        // Get region details
        var regionQuery = _regionRepository.Query(r => r.Id == regionId.Value);
        var region = await _regionRepository.FirstOrDefaultAsync(regionQuery, cancellationToken);

        if (region == null)
        {
            _logger.LogWarning("Region not found: {RegionId}", regionId);
            return null;
        }

        // Return region code in the format "regionXX" where XX is the region code
        var regionCode = !string.IsNullOrEmpty(region.Code) 
            ? $"region{region.Code.ToLowerInvariant()}" 
            : $"region{region.Id.ToString().Substring(0, 8)}"; // Fallback to part of GUID

        return regionCode;
    }
}
