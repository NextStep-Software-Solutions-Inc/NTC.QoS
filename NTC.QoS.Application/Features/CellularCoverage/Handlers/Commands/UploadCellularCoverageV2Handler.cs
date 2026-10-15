using Hangfire;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NTC.QoS.Application.Features.CellularCoverage.Requests;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.CellularCoverage;
using NTC.QoS.Domain.Models.DTOs.File;

namespace NTC.QoS.Application.Features.CellularCoverage.Handlers.Commands;

/// <summary>
/// Handler for uploading CellularCoverage V2 CSV files
/// </summary>
public class UploadCellularCoverageV2Handler : IRequestHandler<UploadCellularCoverageV2Command, ApiResponse<CellularCoverageV2UploadResultDto>>
{
    private readonly IRepository<Domain.Entities.File> _fileRepository;
    private readonly IBlobStorageService _blobStorageService;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly ILogger<UploadCellularCoverageV2Handler> _logger;
    private readonly IUserContextService _userContextService;
    private readonly IUserAuthorizationService _userAuthorizationService;
    private readonly IRegionRepository _regionRepository;
    private readonly IConfiguration _configuration;
    private readonly IBackgroundJobService _backgroundJobService;

    public UploadCellularCoverageV2Handler(
        IRepository<Domain.Entities.File> fileRepository,
        IBlobStorageService blobStorageService,
        IBackgroundJobClient backgroundJobClient,
        ILogger<UploadCellularCoverageV2Handler> logger,
        IUserContextService userContextService,
        IUserAuthorizationService userAuthorizationService,
        IRegionRepository regionRepository,
        IConfiguration configuration,
        IBackgroundJobService backgroundJobService)
    {
        _fileRepository = fileRepository;
        _blobStorageService = blobStorageService;
        _backgroundJobClient = backgroundJobClient;
        _logger = logger;
        _userContextService = userContextService;
        _userAuthorizationService = userAuthorizationService;
        _regionRepository = regionRepository;
        _configuration = configuration;
        _backgroundJobService = backgroundJobService;
    }

    public async Task<ApiResponse<CellularCoverageV2UploadResultDto>> Handle(
        UploadCellularCoverageV2Command request,
        CancellationToken cancellationToken)
    {
        try
        {
            var maxUploadSizeBytes = GetMaxUploadSizeBytes();
            if (request.CsvFile.Length <= 0)
            {
                return new ApiResponse<CellularCoverageV2UploadResultDto>
                {
                    Succeeded = false,
                    Message = "Uploaded file is empty.",
                    Data = null
                };
            }

            if (request.CsvFile.Length > maxUploadSizeBytes)
            {
                var maxMb = maxUploadSizeBytes / (1024.0 * 1024.0);
                return new ApiResponse<CellularCoverageV2UploadResultDto>
                {
                    Succeeded = false,
                    Message = $"File is too large. Maximum allowed size is {maxMb:0.#} MB.",
                    Data = null
                };
            }

            _logger.LogInformation(
                "Starting CellularCoverage V2 upload for NetworkId={NetworkId}, ServiceProviderId={ServiceProviderId}, MunicipalityId={MunicipalityId}, RegionId={RegionId}",
                request.NetworkId, request.ServiceProviderId, request.MunicipalityId, request.RegionId);

            // Step 1: Validate region access
            var regionCode = await GetRegionCodeAsync(request.RegionId, cancellationToken);
            if (string.IsNullOrEmpty(regionCode))
            {
                return new ApiResponse<CellularCoverageV2UploadResultDto>
                {
                    Succeeded = false,
                    Message = "Invalid region or user does not have access to the specified region.",
                    Data = null
                };
            }

            // Step 2: Prepare file for upload
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            string originalFileName = request.CsvFile.FileName;
            string fileExtension = Path.GetExtension(originalFileName);
            string fileGuid = Guid.NewGuid().ToString("N");
            string fileName = $"{timestamp}_{fileGuid}{fileExtension}";

            // Step 3: Determine container and blob path
            string containerName = GetContainerName();
            string blobPath = $"{regionCode}/cellular-coverages/{fileName}";

            _logger.LogInformation("Uploading file {OriginalFileName} as {BlobPath} to container {ContainerName} for region {RegionCode}",
                originalFileName, blobPath, containerName, regionCode);

            // Step 4: Upload file to blob storage
            string blobUrl;
            using (var stream = request.CsvFile.OpenReadStream())
            {
                if (stream.CanSeek)
                {
                    stream.Position = 0;
                }

                blobUrl = await _blobStorageService.UploadAsync(
                    containerName,
                    blobPath,
                    stream,
                    request.CsvFile.ContentType,
                    cancellationToken);
            }

            _logger.LogInformation("File uploaded to blob storage: {BlobUrl}", blobUrl);

            // Step 5: Create file record in database
            var fileRecord = new Domain.Entities.File
            {
                Name = originalFileName,
                Path = blobUrl,
                Type = FileTypeEnum.CellularCoverageV2,
                Status = FileStatusEnum.Pending,
                UploadedBy = _userContextService.GetCurrentUserId() ?? "Unknown",
                UploadDate = DateTime.UtcNow,
                RegionId = request.RegionId
            };

            _fileRepository.Add(fileRecord);
            await _fileRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("File record created with Id={FileId}", fileRecord.Id);

            // Step 6: Enqueue background job for processing
            var fileDto = new FileDto
            {
                Id = fileRecord.Id,
                Name = fileRecord.Name,
                Path = fileRecord.Path,
                Type = fileRecord.Type.ToString(),
                Status = fileRecord.Status.ToString(),
                UploadedBy = fileRecord.UploadedBy,
                UploadDate = fileRecord.UploadDate
            };

            var jobId = _backgroundJobClient.Enqueue<IProcessCellularCoverageV2Job>(
                job => job.ExecuteAsync(
                    fileDto,
                    request.NetworkId,
                    request.ServiceProviderId,
                    request.MunicipalityId,
                    CancellationToken.None));

            // Store the job ID in the file record to prevent duplicate processing
            fileRecord.ProcessingJobId = jobId;
            _fileRepository.Update(fileRecord);
            await _fileRepository.SaveChangesAsync(cancellationToken);

            // Schedule file deletion after 1 day (same as other file types)
            _backgroundJobService.Schedule<IDeleteUploadedFileJob>(
                job => job.ExecuteAsync(fileDto, CancellationToken.None), 
                TimeSpan.FromDays(1));

            _logger.LogInformation("Background job enqueued with JobId={JobId} and file deletion scheduled for 1 day", jobId);

            return new ApiResponse<CellularCoverageV2UploadResultDto>
            {
                Succeeded = true,
                Message = "CSV file uploaded successfully. Processing will begin shortly.",
                Data = new CellularCoverageV2UploadResultDto
                {
                    JobId = fileRecord.Id, // Use file record ID instead of Hangfire job ID
                    Message = "File is being processed in the background",
                    Status = "Processing"
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading CellularCoverage V2 file");
            return new ApiResponse<CellularCoverageV2UploadResultDto>
            {
                Succeeded = false,
                Message = $"Error uploading file: {ex.Message}",
                Data = null
            };
        }
    }

    private long GetMaxUploadSizeBytes()
    {
        var maxSizeMb = _configuration.GetValue<long?>("CellularCoverageV2Upload:MaxFileSizeMB")
            ?? _configuration.GetValue<long?>("AzureBlobStorage:MaxFileSizeMB")
            ?? 50;

        if (maxSizeMb <= 0)
        {
            maxSizeMb = 50;
        }

        return maxSizeMb * 1024L * 1024L;
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
    /// Gets the region code for the specified region ID and validates user access
    /// </summary>
    private async Task<string?> GetRegionCodeAsync(Guid regionId, CancellationToken cancellationToken)
    {
        // Verify user has access to the specified region
        var hasAccess = await _userAuthorizationService.HasRegionAccessAsync(regionId, cancellationToken);
        if (!hasAccess)
        {
            _logger.LogWarning("User does not have access to region: {RegionId}", regionId);
            return null;
        }

        // Get region details
        var regionQuery = _regionRepository.Query(r => r.Id == regionId).Select(r => new { r.Id, r.Code });
        var region = await _regionRepository.FirstOrDefaultAsync(regionQuery, cancellationToken);

        if (region is null)
        {
            _logger.LogWarning("Region not found: {RegionId}", regionId);
            return null;
        }

        // Return region code in the format "region-XX" where XX is the region code
        return !string.IsNullOrWhiteSpace(region.Code) ? $"region-{region.Code:D2}" : null;
    }
}

