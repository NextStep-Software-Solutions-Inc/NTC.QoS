using Hangfire;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.Reports;

namespace NTC.QoS.Infrastructure.BackgroundTasks;

/// <summary>
/// Background job for generating connectivity gap reports with progress tracking.
/// 
/// =============================================================================
/// MAIN RESPONSIBILITIES
/// =============================================================================
/// 1. Generate connectivity gap reports (Excel/PDF) as background jobs
/// 2. Track progress through report generation steps
/// 3. Store generated reports in blob storage
/// 4. Provide real-time status updates via in-memory cache
/// 5. Handle errors and job failures gracefully
/// 
/// =============================================================================
/// PROCESS FLOW
/// =============================================================================
/// 1. Update job status to Processing with start time
/// 2. Fetch report data using optimized queries
/// 3. Track progress: Fetching data -> Generating report -> Saving file
/// 4. Generate report file (Excel or PDF)
/// 5. Upload file to blob storage
/// 6. Update job status to Completed with download URL
/// 7. Clean up cached status after retention period
/// 
/// =============================================================================
/// PROGRESS TRACKING
/// =============================================================================
/// Uses in-memory cache to store job status for real-time updates:
/// - Step 1: Fetching report data (0-40%)
/// - Step 2: Generating report file (40-80%)
/// - Step 3: Uploading to storage (80-100%)
/// 
/// Status cache key: connectivity_gap_report_job_{jobId}
/// Cache retention: 1 hour after completion
/// 
/// =============================================================================
/// JOB CONFIGURATION
/// =============================================================================
/// Queue: "report-generation" (dedicated queue for report jobs)
/// Retry: 2 attempts with exponential backoff
/// Timeout: 30 minutes
/// </summary>
[Queue("report-generation")]
[AutomaticRetry(Attempts = 2, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
public class ProcessConnectivityGapReportJob : IProcessConnectivityGapReportJob
{
    private readonly ILogger<ProcessConnectivityGapReportJob> _logger;
    private readonly IReportGenerationService _reportGenerationService;
    private readonly IBlobStorageService _blobStorageService;
    private readonly IMemoryCache _memoryCache;
    private readonly IConnectivityAssessmentRepository _connectivityAssessmentRepository;
    private readonly IBarangayRepository _barangayRepository;
    private readonly IMunicipalityRepository _municipalityRepository;
    private readonly IRepository<Province> _provinceRepository;
    private readonly IRepository<Region> _regionRepository;
    private readonly IBarangayCoverageRepository _barangayCoverageRepository;
    private readonly IConfiguration _configuration;

    private const string CACHE_KEY_PREFIX = "connectivity_gap_report_job_";
    private const int CACHE_RETENTION_HOURS = 1;
    private const string BLOB_CONTAINER_NAME = "reports";

    public ProcessConnectivityGapReportJob(
        ILogger<ProcessConnectivityGapReportJob> logger,
        IReportGenerationService reportGenerationService,
        IBlobStorageService blobStorageService,
        IMemoryCache memoryCache,
        IConnectivityAssessmentRepository connectivityAssessmentRepository,
        IBarangayRepository barangayRepository,
        IMunicipalityRepository municipalityRepository,
        IRepository<Province> provinceRepository,
        IRepository<Region> regionRepository,
        IBarangayCoverageRepository barangayCoverageRepository,
        IConfiguration configuration)
    {
        _logger = logger;
        _reportGenerationService = reportGenerationService;
        _blobStorageService = blobStorageService;
        _memoryCache = memoryCache;
        _connectivityAssessmentRepository = connectivityAssessmentRepository;
        _barangayRepository = barangayRepository;
        _municipalityRepository = municipalityRepository;
        _provinceRepository = provinceRepository;
        _regionRepository = regionRepository;
        _barangayCoverageRepository = barangayCoverageRepository;
        _configuration = configuration;
    }

    /// <summary>
    /// Executes the connectivity gap report generation job
    /// </summary>
    [AutomaticRetry(Attempts = 2, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
    public async Task ExecuteAsync(ConnectivityGapReportJobRequestDto jobRequest, CancellationToken cancellationToken)
    {
        var jobId = jobRequest.JobId;
        var startTime = DateTime.UtcNow;

        try
        {
            _logger.LogInformation(
                "Starting connectivity gap report job {JobId}. Format: {Format}, Region: {Region}, Province: {Province}, Municipality: {Municipality}, FilterByStatus: {FilterByStatus}, User: {UserId}",
                jobId, jobRequest.Format, jobRequest.RegionId, jobRequest.ProvinceId, jobRequest.MunicipalityId, jobRequest.FilterByStatus, jobRequest.UserId);

            // Step 1: Update status to Processing
            await UpdateJobStatusAsync(jobId, ConnectivityGapReportJobStatusEnum.Processing,
                "Initializing report generation...",
                startTime: startTime,
                currentStep: "Initializing",
                progressPercentage: 0,
                format: jobRequest.Format,
                cancellationToken: cancellationToken);

            // Step 2: Fetch report data using optimized queries
            await UpdateJobStatusAsync(jobId, ConnectivityGapReportJobStatusEnum.Processing,
                "Fetching connectivity gap data...",
                currentStep: "Fetching Data",
                progressPercentage: 10,
                cancellationToken: cancellationToken);

            _logger.LogInformation("Job {JobId}: Fetching connectivity assessments with filters", jobId);

            // Validate FilterByStatus: gap report only supports Unserved/Underserved; reject Served and any undefined values
            if (jobRequest.FilterByStatus.HasValue)
            {
                var filterByStatus = jobRequest.FilterByStatus.Value;

                if (!Enum.IsDefined(typeof(ConnectivityStatusEnum), filterByStatus) ||
                    (filterByStatus != ConnectivityStatusEnum.Unserved &&
                     filterByStatus != ConnectivityStatusEnum.Underserved))
                {
                    await UpdateJobStatusAsync(jobId, ConnectivityGapReportJobStatusEnum.Failed,
                        $"Invalid FilterByStatus for the gap report: '{filterByStatus}' is not valid. Use 'Unserved' or 'Underserved'.",
                        startTime: startTime,
                        endTime: DateTime.UtcNow,
                        currentStep: "Failed",
                        cancellationToken: cancellationToken);
                    _logger.LogWarning("Job {JobId}: Rejected invalid FilterByStatus={FilterByStatus} for gap report", jobId, filterByStatus);
                    return;
                }
            }

            // Build assessment query with geographic filters using optimized projection pattern
            var assessmentQuery = _connectivityAssessmentRepository.QueryAsNoTracking();

            // Filter by location type: only barangays
            assessmentQuery = assessmentQuery.Where(a => a.LocationType == LocationTypeEnum.Barangay);

            // Gap report always excludes Served areas; apply additional status filter if requested
            assessmentQuery = assessmentQuery.Where(a => 
                a.ConnectivityStatus == ConnectivityStatusEnum.Unserved || 
                a.ConnectivityStatus == ConnectivityStatusEnum.Underserved);

            // Apply geographic filters using subquery pattern
            // Only include barangays with BarangayCoverage records
            var coveredBarangayIds = _barangayCoverageRepository.QueryAsNoTracking()
                .Select(bc => bc.BarangayId);
            assessmentQuery = assessmentQuery.Where(a => coveredBarangayIds.Contains(a.LocationId));

            if (jobRequest.RegionId.HasValue)
            {
                // Get provinces in the region
                var provinceIds = await _provinceRepository.QueryAsNoTracking()
                    .Where(p => p.RegionId == jobRequest.RegionId.Value)
                    .Select(p => p.Id)
                    .ToListAsync(cancellationToken);

                if (provinceIds.Any())
                {
                    // Get municipalities in those provinces
                    var municipalityIds = await _municipalityRepository.QueryAsNoTracking()
                        .Where(m => provinceIds.Contains(m.ProvinceId))
                        .Select(m => m.Id)
                        .ToListAsync(cancellationToken);

                    if (municipalityIds.Any())
                    {
                        // Get barangays in those municipalities
                        var barangayIds = await _barangayRepository.QueryAsNoTracking()
                            .Where(b => municipalityIds.Contains(b.MunicipalityId) && coveredBarangayIds.Contains(b.Id))
                            .Select(b => b.Id)
                            .ToListAsync(cancellationToken);

                        assessmentQuery = assessmentQuery.Where(a => barangayIds.Contains(a.LocationId));
                    }
                    else
                    {
                        // No municipalities in region, return empty results
                        assessmentQuery = assessmentQuery.Where(a => false);
                    }
                }
                else
                {
                    // No provinces in region, return empty results
                    assessmentQuery = assessmentQuery.Where(a => false);
                }
            }

            if (jobRequest.ProvinceId.HasValue)
            {
                // Get municipalities in the province
                var municipalityIds = await _municipalityRepository.QueryAsNoTracking()
                    .Where(m => m.ProvinceId == jobRequest.ProvinceId.Value)
                    .Select(m => m.Id)
                    .ToListAsync(cancellationToken);

                if (municipalityIds.Any())
                {
                    // Get barangays in those municipalities
                    var barangayIds = await _barangayRepository.QueryAsNoTracking()
                        .Where(b => municipalityIds.Contains(b.MunicipalityId) && coveredBarangayIds.Contains(b.Id))
                        .Select(b => b.Id)
                        .ToListAsync(cancellationToken);

                    assessmentQuery = assessmentQuery.Where(a => barangayIds.Contains(a.LocationId));
                }
                else
                {
                    // No municipalities in province, return empty results
                    assessmentQuery = assessmentQuery.Where(a => false);
                }
            }

            if (jobRequest.MunicipalityId.HasValue)
            {
                // Get barangays in the municipality
                var barangayIds = await _barangayRepository.QueryAsNoTracking()
                    .Where(b => b.MunicipalityId == jobRequest.MunicipalityId.Value && coveredBarangayIds.Contains(b.Id))
                    .Select(b => b.Id)
                    .ToListAsync(cancellationToken);

                if (barangayIds.Any())
                {
                    assessmentQuery = assessmentQuery.Where(a => barangayIds.Contains(a.LocationId));
                }
                else
                {
                    // No barangays in municipality, return empty results
                    assessmentQuery = assessmentQuery.Where(a => false);
                }
            }

            // Fetch assessments with optimized projection (only needed fields)
            var assessments = await _connectivityAssessmentRepository.ToListAsync(
                assessmentQuery
                    .OrderByDescending(a => a.Population ?? 0)
                    .ThenBy(a => a.LocationId),
                cancellationToken);

            _logger.LogInformation("Job {JobId}: Found {Count} connectivity gap areas", jobId, assessments.Count);

            await UpdateJobStatusAsync(jobId, ConnectivityGapReportJobStatusEnum.Processing,
                $"Processing {assessments.Count} connectivity gap areas",
                currentStep: "Processing Data",
                progressPercentage: 30,
                areasProcessed: 0,
                totalAreas: assessments.Count,
                cancellationToken: cancellationToken);

            // Build priority areas with location names (all gap areas, before optional status filter)
            var allPriorityAreas = new List<PriorityAreaDto>();
            var locationIds = assessments.Select(a => a.LocationId).ToList();

            // Single batch query for both location names and municipality/province details
            var barangayLocationData = locationIds.Count > 0
                ? await _barangayRepository.ToListAsync(
                    _barangayRepository.QueryAsNoTracking()
                        .Where(b => locationIds.Contains(b.Id))
                        .Select(b => new
                        {
                            b.Id,
                            b.Name,
                            MunicipalityName = b.Municipality != null ? b.Municipality.Name : string.Empty,
                            ProvinceName = b.Municipality != null && b.Municipality.Province != null
                                ? b.Municipality.Province.Name
                                : string.Empty
                        }),
                    cancellationToken)
                : [];
            var locationNames = barangayLocationData.ToDictionary(x => x.Id, x => x.Name);
            var barangayDetails = barangayLocationData.ToDictionary(
                x => x.Id,
                x => (x.MunicipalityName, x.ProvinceName));

            for (int i = 0; i < assessments.Count; i++)
            {
                var assessment = assessments[i];
                var locationName = locationNames.GetValueOrDefault(assessment.LocationId, $"Location-{assessment.LocationId}");
                var (municipalityName, provinceName) = barangayDetails.GetValueOrDefault(assessment.LocationId);

                allPriorityAreas.Add(new PriorityAreaDto
                {
                    LocationId = assessment.LocationId,
                    LocationName = locationName,
                    MunicipalityName = municipalityName,
                    ProvinceName = provinceName,
                    ConnectivityStatus = assessment.ConnectivityStatus,
                    Population = assessment.Population,
                    TotalScore = assessment.TotalScore,
                    LastAssessed = assessment.AssessmentDate
                });

                // Update progress every 10 areas
                if ((i + 1) % 10 == 0 || i == assessments.Count - 1)
                {
                    var progress = 30 + (10 * (i + 1) / assessments.Count);
                    await UpdateJobStatusAsync(jobId, ConnectivityGapReportJobStatusEnum.Processing,
                        $"Processed {i + 1} of {assessments.Count} areas",
                        currentStep: "Processing Data",
                        progressPercentage: progress,
                        areasProcessed: i + 1,
                        totalAreas: assessments.Count,
                        cancellationToken: cancellationToken);
                }
            }

            // Summary always reflects ALL gap areas (unserved + underserved) regardless of FilterByStatus
            var summary = new ConnectivityGapSummaryDto
            {
                TotalUnservedAreas = assessments.Count(a => a.ConnectivityStatus == ConnectivityStatusEnum.Unserved),
                TotalUnderservedAreas = assessments.Count(a => a.ConnectivityStatus == ConnectivityStatusEnum.Underserved),
                UnservedPopulation = assessments
                    .Where(a => a.ConnectivityStatus == ConnectivityStatusEnum.Unserved)
                    .Sum(a => a.Population ?? 0),
                UnderservedPopulation = assessments
                    .Where(a => a.ConnectivityStatus == ConnectivityStatusEnum.Underserved)
                    .Sum(a => a.Population ?? 0)
            };

            // Apply optional status filter to the priority areas list
            var priorityAreas = jobRequest.FilterByStatus.HasValue
                ? allPriorityAreas.Where(a => a.ConnectivityStatus == jobRequest.FilterByStatus.Value).ToList()
                : allPriorityAreas;

            // Create report DTO using overall ConnectivityStatus
            var reportData = new ConnectivityGapReportDto
            {
                Summary = summary,
                PriorityAreas = priorityAreas,
                TotalPriorityAreas = priorityAreas.Count,
                PageNumber = 1,
                PageSize = priorityAreas.Count,
                TotalPages = 1,
                GeneratedAt = DateTime.UtcNow
            };

            _logger.LogInformation(
                "Job {JobId}: Report data generated. Unserved: {Unserved}, Underserved: {Underserved}, Priority Areas in report: {PriorityCount}",
                jobId, summary.TotalUnservedAreas, summary.TotalUnderservedAreas, priorityAreas.Count);

            await UpdateJobStatusAsync(jobId, ConnectivityGapReportJobStatusEnum.Processing,
                $"Data generated: {summary.TotalUnservedAreas} unserved, {summary.TotalUnderservedAreas} underserved areas",
                currentStep: "Generating Report",
                progressPercentage: 40,
                areasProcessed: assessments.Count,
                totalAreas: assessments.Count,
                cancellationToken: cancellationToken);

            // Step 3: Generate report file
            _logger.LogInformation("Job {JobId}: Generating {Format} report file", jobId, jobRequest.Format);

            // Generate report title if not provided
            var reportTitle = jobRequest.ReportTitle;
            if (string.IsNullOrWhiteSpace(reportTitle))
            {
                reportTitle = await GenerateReportTitleAsync(jobRequest.RegionId, jobRequest.ProvinceId, jobRequest.MunicipalityId, cancellationToken);
            }

            byte[] reportBytes;
            string contentType;
            string fileExtension;

            if (jobRequest.Format == ReportFormat.Pdf)
            {
                reportBytes = await _reportGenerationService.GenerateConnectivityGapPdfAsync(
                    reportData,
                    reportTitle,
                    cancellationToken);
                contentType = "application/pdf";
                fileExtension = "pdf";
            }
            else
            {
                reportBytes = await _reportGenerationService.GenerateConnectivityGapExcelAsync(
                    reportData,
                    reportTitle,
                    cancellationToken);
                contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                fileExtension = "xlsx";
            }

            var fileSizeBytes = reportBytes.Length;
            _logger.LogInformation("Job {JobId}: Report file generated. Size: {FileSize} bytes", jobId, fileSizeBytes);

            await UpdateJobStatusAsync(jobId, ConnectivityGapReportJobStatusEnum.Processing,
                $"Report file generated ({FormatFileSize(fileSizeBytes)})",
                currentStep: "Uploading File",
                progressPercentage: 80,
                cancellationToken: cancellationToken);

            // Step 4: Upload to blob storage
            _logger.LogInformation("Job {JobId}: Uploading report file to blob storage", jobId);

            var (_, blobPath) = await GenerateFileNameAndBlobPathAsync(jobRequest.RegionId, jobRequest.ProvinceId, jobRequest.MunicipalityId, fileExtension, cancellationToken);
            
            using var stream = new MemoryStream(reportBytes);
            await _blobStorageService.UploadAsync(BLOB_CONTAINER_NAME, blobPath, stream, contentType, cancellationToken);
            
            var fullBlobPath = $"{BLOB_CONTAINER_NAME}/{blobPath}";

            _logger.LogInformation("Job {JobId}: Report file uploaded successfully to {Path}", jobId, fullBlobPath);

            // Step 5: Update status to Completed
            var endTime = DateTime.UtcNow;
            var totalTime = endTime - startTime;
            var downloadUrl = $"/api/reports/connectivity-gap/job/{jobId}/download";

            await UpdateJobStatusAsync(jobId, ConnectivityGapReportJobStatusEnum.Completed,
                $"Report generated successfully in {totalTime:mm\\:ss}",
                startTime: startTime,
                endTime: endTime,
                currentStep: "Completed",
                progressPercentage: 100,
                filePath: fullBlobPath,
                downloadUrl: downloadUrl,
                fileSizeBytes: fileSizeBytes,
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Job {JobId}: Report generation completed successfully in {Duration}. File: {FilePath}, Size: {FileSize}",
                jobId, totalTime, blobPath, FormatFileSize(fileSizeBytes));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Job {JobId}: Error occurred during report generation", jobId);

            await UpdateJobStatusAsync(jobId, ConnectivityGapReportJobStatusEnum.Failed,
                $"Report generation failed: {ex.Message}",
                startTime: startTime,
                endTime: DateTime.UtcNow,
                currentStep: "Failed",
                cancellationToken: cancellationToken);

            throw;
        }
    }

    /// <summary>
    /// Updates job status in memory cache for real-time status tracking
    /// </summary>
    private async Task UpdateJobStatusAsync(
        string jobId,
        ConnectivityGapReportJobStatusEnum status,
        string message,
        DateTime? startTime = null,
        DateTime? endTime = null,
        string? currentStep = null,
        double? progressPercentage = null,
        int? areasProcessed = null,
        int? totalAreas = null,
        ReportFormat? format = null,
        string? filePath = null,
        string? downloadUrl = null,
        long? fileSizeBytes = null,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{CACHE_KEY_PREFIX}{jobId}";

        // Get existing status or create new one
        var statusDto = _memoryCache.GetOrCreate(cacheKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(CACHE_RETENTION_HOURS);
            return new ConnectivityGapReportJobStatusDto
            {
                JobId = jobId,
                Status = status,
                Message = message,
                LastUpdated = DateTime.UtcNow
            };
        });

        if (statusDto != null)
        {
            // Update fields
            statusDto.Status = status;
            statusDto.Message = message;
            statusDto.LastUpdated = DateTime.UtcNow;

            if (startTime.HasValue) statusDto.StartTime = startTime.Value;
            if (endTime.HasValue) statusDto.EndTime = endTime.Value;
            if (!string.IsNullOrEmpty(currentStep)) statusDto.CurrentStep = currentStep;
            if (progressPercentage.HasValue) statusDto.ProgressPercentage = progressPercentage.Value;
            if (areasProcessed.HasValue) statusDto.AreasProcessed = areasProcessed.Value;
            if (totalAreas.HasValue) statusDto.TotalAreas = totalAreas.Value;
            if (format.HasValue) statusDto.Format = format.Value;
            if (!string.IsNullOrEmpty(filePath)) statusDto.FilePath = filePath;
            if (!string.IsNullOrEmpty(downloadUrl)) statusDto.DownloadUrl = downloadUrl;
            if (fileSizeBytes.HasValue) statusDto.FileSizeBytes = fileSizeBytes.Value;

            // Update cache
            _memoryCache.Set(cacheKey, statusDto, TimeSpan.FromHours(CACHE_RETENTION_HOURS));
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Generates a report title based on geographic filters
    /// </summary>
    private async Task<string> GenerateReportTitleAsync(
        Guid? regionId,
        Guid? provinceId,
        Guid? municipalityId,
        CancellationToken cancellationToken)
    {
        var titleParts = new List<string> { "Connectivity Gap Report" };

        if (municipalityId.HasValue)
        {
            var municipality = await _municipalityRepository.Query(m => m.Id == municipalityId.Value)
                .FirstOrDefaultAsync(cancellationToken);
            if (municipality != null)
            {
                titleParts.Add(municipality.Name);
            }
        }
        else if (provinceId.HasValue)
        {
            var province = await _provinceRepository.Query(p => p.Id == provinceId.Value)
                .FirstOrDefaultAsync(cancellationToken);
            if (province != null)
            {
                titleParts.Add(province.Name);
            }
        }
        else if (regionId.HasValue)
        {
            var region = await _regionRepository.Query(r => r.Id == regionId.Value)
                .FirstOrDefaultAsync(cancellationToken);
            if (region != null)
            {
                titleParts.Add(region.Name);
            }
        }

        return string.Join(" - ", titleParts);
    }

    /// <summary>
    /// Generates both file name and blob storage path in a single database query to avoid duplicate queries.
    /// Returns tuple of (fileName, blobPath).
    /// Blob path follows pattern: env/area-code/filename
    /// Example: development/region-PH01/ConnectivityGap_RegionI_YYYYMMDD_HHMMSS.xlsx
    /// </summary>
    private async Task<(string fileName, string blobPath)> GenerateFileNameAndBlobPathAsync(
        Guid? regionId,
        Guid? provinceId,
        Guid? municipalityId,
        string extension,
        CancellationToken cancellationToken)
    {
        var nameParts = new List<string> { "ConnectivityGap" };
        var environment = _configuration["ASPNETCORE_ENVIRONMENT"] ?? "Development";
        environment = environment.ToLower();
        string areaCode;

        if (municipalityId.HasValue)
        {
            var municipality = await _municipalityRepository.Query(m => m.Id == municipalityId.Value)
                .Select(m => new { m.Name, m.Code })
                .FirstOrDefaultAsync(cancellationToken);
            
            if (municipality != null)
            {
                nameParts.Add(SanitizeFileName(municipality.Name));
                areaCode = $"municipality-{municipality.Code}";
            }
            else
            {
                // Fallback when municipality not found - indicates data integrity issue
                areaCode = $"municipality-{municipalityId}";
                _logger.LogWarning("Municipality with ID {MunicipalityId} not found in database", municipalityId.Value);
            }
        }
        else if (provinceId.HasValue)
        {
            var province = await _provinceRepository.Query(p => p.Id == provinceId.Value)
                .Select(p => new { p.Name, p.Code })
                .FirstOrDefaultAsync(cancellationToken);
            
            if (province != null)
            {
                nameParts.Add(SanitizeFileName(province.Name));
                areaCode = $"province-{province.Code}";
            }
            else
            {
                // Fallback when province not found - indicates data integrity issue
                areaCode = $"province-{provinceId}";
                _logger.LogWarning("Province with ID {ProvinceId} not found in database", provinceId.Value);
            }
        }
        else if (regionId.HasValue)
        {
            var region = await _regionRepository.Query(r => r.Id == regionId.Value)
                .Select(r => new { r.Name, r.Code })
                .FirstOrDefaultAsync(cancellationToken);
            
            if (region != null)
            {
                nameParts.Add(SanitizeFileName(region.Name));
                areaCode = $"region-{region.Code}";
            }
            else
            {
                // Fallback when region not found - indicates data integrity issue
                areaCode = $"region-{regionId}";
                _logger.LogWarning("Region with ID {RegionId} not found in database", regionId.Value);
            }
        }
        else
        {
            nameParts.Add("National");
            areaCode = "national";
        }

        nameParts.Add(DateTime.UtcNow.ToString("yyyyMMdd_HHmmss"));
        var fileName = $"{string.Join("_", nameParts)}.{extension}";
        var blobPath = $"{environment}/{areaCode}/{fileName}";

        return (fileName, blobPath);
    }

    /// <summary>
    /// Sanitizes a string for use in file names
    /// </summary>
    private string SanitizeFileName(string fileName)
    {
        var invalids = System.IO.Path.GetInvalidFileNameChars();
        return string.Join("_", fileName.Split(invalids, StringSplitOptions.RemoveEmptyEntries)).TrimEnd('.');
    }

    /// <summary>
    /// Formats file size in human-readable format
    /// </summary>
    private string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        int order = 0;
        double len = bytes;

        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }

        return $"{len:0.##} {sizes[order]}";
    }
}
