using Hangfire;
using MediatR;
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

    public UploadCellularCoverageV2Handler(
        IRepository<Domain.Entities.File> fileRepository,
        IBlobStorageService blobStorageService,
        IBackgroundJobClient backgroundJobClient,
        ILogger<UploadCellularCoverageV2Handler> logger,
        IUserContextService userContextService)
    {
        _fileRepository = fileRepository;
        _blobStorageService = blobStorageService;
        _backgroundJobClient = backgroundJobClient;
        _logger = logger;
        _userContextService = userContextService;
    }

    public async Task<ApiResponse<CellularCoverageV2UploadResultDto>> Handle(
        UploadCellularCoverageV2Command request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Starting CellularCoverage V2 upload for NetworkId={NetworkId}, ServiceProviderId={ServiceProviderId}, MunicipalityId={MunicipalityId}",
                request.NetworkId, request.ServiceProviderId, request.MunicipalityId);

            // Step 1: Upload file to blob storage
            var containerName = "cellular-coverage-v2";
            var fileName = $"{Guid.NewGuid()}_{request.CsvFile.FileName}";
            string blobUrl;

            using (var stream = request.CsvFile.OpenReadStream())
            {
                blobUrl = await _blobStorageService.UploadAsync(
                    containerName,
                    fileName,
                    stream,
                    request.CsvFile.ContentType,
                    cancellationToken);
            }

            _logger.LogInformation("File uploaded to blob storage: {BlobUrl}", blobUrl);

            // Step 2: Create file record in database
            var fileRecord = new Domain.Entities.File
            {
                Name = request.CsvFile.FileName,
                Path = blobUrl,
                Type = FileTypeEnum.CellularCoverageV2,
                Status = FileStatusEnum.Pending,
                UploadedBy = _userContextService.GetCurrentUserId() ?? "Unknown",
                UploadDate = DateTime.UtcNow
            };

            _fileRepository.Add(fileRecord);
            await _fileRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("File record created with Id={FileId}", fileRecord.Id);

            // Step 3: Enqueue background job for processing
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

            _logger.LogInformation("Background job enqueued with JobId={JobId}", jobId);

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
}
