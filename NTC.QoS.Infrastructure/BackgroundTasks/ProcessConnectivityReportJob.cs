using Hangfire;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.Reports;

namespace NTC.QoS.Infrastructure.BackgroundTasks;

/// <summary>
/// Background job for generating connectivity reports (served areas) with progress tracking.
/// 
/// =============================================================================
/// MAIN RESPONSIBILITIES
/// =============================================================================
/// 1. Generate connectivity reports (Excel/PDF) as background jobs
/// 2. Show served areas with full CMTS rubric score breakdown
/// 3. Track progress through report generation steps
/// 4. Store generated reports in blob storage
/// 5. Provide real-time status updates via in-memory cache
/// 
/// =============================================================================
/// PROCESS FLOW
/// =============================================================================
/// 1. Update job status to Processing with start time
/// 2. Fetch connectivity assessments for served areas
/// 3. Build detailed area information with rubric scores
/// 4. Calculate summary statistics
/// 5. Generate report file (Excel or PDF)
/// 6. Upload file to blob storage
/// 7. Update job status to Completed with download URL
/// 
/// Status cache key: connectivity_report_job_{jobId}
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
public class ProcessConnectivityReportJob : IProcessConnectivityReportJob
{
    private readonly ILogger<ProcessConnectivityReportJob> _logger;
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

    private const string CACHE_KEY_PREFIX = "connectivity_report_job_";
    private const int CACHE_RETENTION_HOURS = 1;
    private const string BLOB_CONTAINER_NAME = "reports";

    public ProcessConnectivityReportJob(
        ILogger<ProcessConnectivityReportJob> logger,
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
    /// Executes the connectivity report generation job
    /// </summary>
    [AutomaticRetry(Attempts = 2, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
    public async Task ExecuteAsync(ConnectivityReportJobRequestDto jobRequest, CancellationToken cancellationToken)
    {
        var jobId = jobRequest.JobId;
        var startTime = DateTime.UtcNow;

        try
        {
            _logger.LogInformation(
                "Starting connectivity report job {JobId}. Format: {Format}, Region: {Region}, Province: {Province}, Municipality: {Municipality}, FilterByStatus: {FilterByStatus}, User: {UserId}",
                jobId, jobRequest.Format, jobRequest.RegionId, jobRequest.ProvinceId, jobRequest.MunicipalityId, jobRequest.FilterByStatus, jobRequest.UserId);

            // Step 1: Update status to Processing
            await UpdateJobStatusAsync(jobId, ConnectivityReportJobStatusEnum.Processing,
                "Initializing report generation...",
                startTime: startTime,
                currentStep: "Initializing",
                progressPercentage: 0,
                format: jobRequest.Format,
                cancellationToken: cancellationToken);

            // Step 2: Fetch report data using optimized queries
            await UpdateJobStatusAsync(jobId, ConnectivityReportJobStatusEnum.Processing,
                "Fetching connectivity data...",
                currentStep: "Fetching Data",
                progressPercentage: 10,
                cancellationToken: cancellationToken);

            // Validate FilterByStatus: reject undefined enum values early
            if (jobRequest.FilterByStatus.HasValue && !Enum.IsDefined(typeof(ConnectivityStatusEnum), jobRequest.FilterByStatus.Value))
            {
                await UpdateJobStatusAsync(jobId, ConnectivityReportJobStatusEnum.Failed,
                    $"Invalid FilterByStatus value '{(int)jobRequest.FilterByStatus.Value}'. Valid values are: Served (3), Underserved (2), Unserved (1).",
                    startTime: startTime,
                    endTime: DateTime.UtcNow,
                    currentStep: "Failed",
                    cancellationToken: cancellationToken);
                _logger.LogWarning("Job {JobId}: Rejected invalid FilterByStatus={FilterByStatus}", jobId, jobRequest.FilterByStatus.Value);
                return;
            }

            _logger.LogInformation("Job {JobId}: Fetching connectivity assessments for served areas", jobId);

            // Build assessment query with geographic filters
            var assessmentQuery = _connectivityAssessmentRepository.QueryAsNoTracking();

            // Filter by location type: only barangays
            assessmentQuery = assessmentQuery.Where(a => a.LocationType == LocationTypeEnum.Barangay);

            // NOTE: Do NOT apply FilterByStatus to the DB query — we need the full dataset so
            // summary statistics (TotalServedAreas, populations, score averages) always reflect
            // the entire geographic scope. The status filter is applied in-memory to the Areas list.

            // Apply geographic filters
            // Only include barangays with BarangayCoverage records
            var coveredBarangayIds = _barangayCoverageRepository.QueryAsNoTracking()
                .Select(bc => bc.BarangayId);
            assessmentQuery = assessmentQuery.Where(a => coveredBarangayIds.Contains(a.LocationId));

            if (jobRequest.RegionId.HasValue)
            {
                var provinceIds = await _provinceRepository.QueryAsNoTracking()
                    .Where(p => p.RegionId == jobRequest.RegionId.Value)
                    .Select(p => p.Id)
                    .ToListAsync(cancellationToken);

                if (provinceIds.Any())
                {
                    var municipalityIds = await _municipalityRepository.QueryAsNoTracking()
                        .Where(m => provinceIds.Contains(m.ProvinceId))
                        .Select(m => m.Id)
                        .ToListAsync(cancellationToken);

                    if (municipalityIds.Any())
                    {
                        var barangayIds = await _barangayRepository.QueryAsNoTracking()
                            .Where(b => municipalityIds.Contains(b.MunicipalityId) && coveredBarangayIds.Contains(b.Id))
                            .Select(b => b.Id)
                            .ToListAsync(cancellationToken);

                        assessmentQuery = assessmentQuery.Where(a => barangayIds.Contains(a.LocationId));
                    }
                    else
                    {
                        assessmentQuery = assessmentQuery.Where(a => false);
                    }
                }
                else
                {
                    assessmentQuery = assessmentQuery.Where(a => false);
                }
            }

            if (jobRequest.ProvinceId.HasValue)
            {
                var municipalityIds = await _municipalityRepository.QueryAsNoTracking()
                    .Where(m => m.ProvinceId == jobRequest.ProvinceId.Value)
                    .Select(m => m.Id)
                    .ToListAsync(cancellationToken);

                if (municipalityIds.Any())
                {
                    var barangayIds = await _barangayRepository.QueryAsNoTracking()
                        .Where(b => municipalityIds.Contains(b.MunicipalityId) && coveredBarangayIds.Contains(b.Id))
                        .Select(b => b.Id)
                        .ToListAsync(cancellationToken);

                    assessmentQuery = assessmentQuery.Where(a => barangayIds.Contains(a.LocationId));
                }
                else
                {
                    assessmentQuery = assessmentQuery.Where(a => false);
                }
            }

            if (jobRequest.MunicipalityId.HasValue)
            {
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
                    assessmentQuery = assessmentQuery.Where(a => false);
                }
            }

            // Fetch assessments ordered by total score descending
            var assessments = await _connectivityAssessmentRepository.ToListAsync(
                assessmentQuery
                    .OrderByDescending(a => a.TotalScore)
                    .ThenByDescending(a => a.Population ?? 0),
                cancellationToken);

            _logger.LogInformation("Job {JobId}: Found {Count} areas", jobId, assessments.Count);

            await UpdateJobStatusAsync(jobId, ConnectivityReportJobStatusEnum.Processing,
                $"Processing {assessments.Count} areas",
                currentStep: "Processing Data",
                progressPercentage: 30,
                areasProcessed: 0,
                totalAreas: assessments.Count,
                cancellationToken: cancellationToken);

            // Single batch query for both location names and municipality/province details
            var allLocationIds = assessments.Select(a => a.LocationId).ToList();
            var barangayLocationData = allLocationIds.Count > 0
                ? await _barangayRepository.ToListAsync(
                    _barangayRepository.QueryAsNoTracking()
                        .Where(b => allLocationIds.Contains(b.Id))
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

            // Build ALL areas from the full (unfiltered) assessment list
            var allAreas = new List<ConnectivityAreaDetailDto>(assessments.Count);
            for (int i = 0; i < assessments.Count; i++)
            {
                var assessment = assessments[i];
                var locationName = locationNames.GetValueOrDefault(assessment.LocationId, $"Location-{assessment.LocationId}");
                var (municipalityName, provinceName) = barangayDetails.GetValueOrDefault(assessment.LocationId);

                allAreas.Add(new ConnectivityAreaDetailDto
                {
                    LocationId = assessment.LocationId,
                    LocationName = locationName,
                    MunicipalityName = municipalityName,
                    ProvinceName = provinceName,
                    ConnectivityStatus = assessment.ConnectivityStatus,
                    Population = assessment.Population,
                    FiberCoverageScore = assessment.FiberCoverageScore,
                    InternetAccessScore = assessment.InternetAccessScore,
                    TechnologyTypeScore = assessment.TechnologyTypeScore,
                    QualityOfServiceScore = assessment.QualityOfServiceScore,
                    CoverageAvailabilityScore = assessment.CoverageAvailabilityScore,
                    TotalScore = assessment.TotalScore,
                    CellTowerCount = assessment.CellTowerCount,
                    FiberNAPCount = assessment.FiberNAPCount,
                    WifiHotspotCount = assessment.WifiHotspotCount,
                    ServiceProviderCount = assessment.ServiceProviderCount,
                    DominantServiceProvider = assessment.DominantServiceProvider,
                    AverageDownloadSpeedMbps = assessment.AverageDownloadSpeedMbps,
                    AverageUploadSpeedMbps = assessment.AverageUploadSpeedMbps,
                    SpeedTestCount = assessment.SpeedTestCount,
                    LastAssessed = assessment.AssessmentDate
                });

                // Update progress every 10 areas
                if ((i + 1) % 10 == 0 || i == assessments.Count - 1)
                {
                    var progress = 30 + (10 * (i + 1) / assessments.Count);
                    await UpdateJobStatusAsync(jobId, ConnectivityReportJobStatusEnum.Processing,
                        $"Processed {i + 1} of {assessments.Count} areas",
                        currentStep: "Processing Data",
                        progressPercentage: progress,
                        areasProcessed: i + 1,
                        totalAreas: assessments.Count,
                        cancellationToken: cancellationToken);
                }
            }

            // Apply optional status filter to the Areas list; summary always reflects the full dataset
            var areas = jobRequest.FilterByStatus.HasValue
                ? allAreas.Where(a => a.ConnectivityStatus == jobRequest.FilterByStatus.Value).ToList()
                : allAreas;

            // Summary statistics always come from the FULL unfiltered dataset
            var scoreSummary = new ConnectivityScoreSummaryDto
            {
                AverageFiberCoverageScore = assessments.Any() ? assessments.Average(a => a.FiberCoverageScore) : 0,
                AverageInternetAccessScore = assessments.Any() ? assessments.Average(a => a.InternetAccessScore) : 0,
                AverageTechnologyTypeScore = assessments.Any() ? assessments.Average(a => a.TechnologyTypeScore) : 0,
                AverageQualityOfServiceScore = assessments.Any() ? assessments.Average(a => a.QualityOfServiceScore) : 0,
                AverageCoverageAvailabilityScore = assessments.Any() ? assessments.Average(a => a.CoverageAvailabilityScore) : 0,
                AverageTotalScore = assessments.Any() ? assessments.Average(a => a.TotalScore) : 0,
                ExcellentCount = assessments.Count(a => a.TotalScore >= 13),
                GoodCount = assessments.Count(a => a.TotalScore >= 11 && a.TotalScore <= 12),
                ModerateCount = assessments.Count(a => a.TotalScore >= 8 && a.TotalScore <= 10),
                FairCount = assessments.Count(a => a.TotalScore >= 6 && a.TotalScore <= 7),
                TotalCellTowers = assessments.Sum(a => a.CellTowerCount),
                TotalFiberNAPs = assessments.Sum(a => a.FiberNAPCount),
                TotalWifiHotspots = assessments.Sum(a => a.WifiHotspotCount),
                TotalServiceProviders = assessments.Select(a => a.DominantServiceProvider).Where(p => !string.IsNullOrEmpty(p)).Distinct().Count()
            };

            // Create report DTO: status-specific totals/populations from full dataset, Areas from filtered list
            var reportData = new ConnectivityReportDto
            {
                TotalServedAreas = assessments.Count(a => a.ConnectivityStatus == ConnectivityStatusEnum.Served),
                TotalUnderservedAreas = assessments.Count(a => a.ConnectivityStatus == ConnectivityStatusEnum.Underserved),
                TotalUnservedAreas = assessments.Count(a => a.ConnectivityStatus == ConnectivityStatusEnum.Unserved),
                ServedPopulation = assessments.Where(a => a.ConnectivityStatus == ConnectivityStatusEnum.Served).Sum(a => a.Population ?? 0),
                UnderservedPopulation = assessments.Where(a => a.ConnectivityStatus == ConnectivityStatusEnum.Underserved).Sum(a => a.Population ?? 0),
                UnservedPopulation = assessments.Where(a => a.ConnectivityStatus == ConnectivityStatusEnum.Unserved).Sum(a => a.Population ?? 0),
                Areas = areas,
                ScoreSummary = scoreSummary,
                MunicipalitySummaries = ConnectivityReportSummaryHelper.BuildMunicipalitySummaries(allAreas),
                ProvinceSummaries = ConnectivityReportSummaryHelper.BuildProvinceSummaries(allAreas),
                RegionSummary = ConnectivityReportSummaryHelper.BuildRegionSummary(allAreas),
                TotalAreas = areas.Count,
                PageNumber = 1,
                PageSize = areas.Count,
                TotalPages = 1,
                GeneratedAt = DateTime.UtcNow
            };

            _logger.LogInformation(
                "Job {JobId}: Report data generated. Total Areas: {TotalCount}, Served: {ServedCount}, Underserved: {UnderservedCount}, Unserved: {UnservedCount}, Average Total Score: {AvgScore:F2}",
                jobId, assessments.Count, reportData.TotalServedAreas, reportData.TotalUnderservedAreas, reportData.TotalUnservedAreas, scoreSummary.AverageTotalScore);

            await UpdateJobStatusAsync(jobId, ConnectivityReportJobStatusEnum.Processing,
                $"Data generated: {assessments.Count} areas with average score {scoreSummary.AverageTotalScore:F2}",
                currentStep: "Generating Report",
                progressPercentage: 40,
                areasProcessed: assessments.Count,
                totalAreas: assessments.Count,
                cancellationToken: cancellationToken);

            // Step 3: Generate report file
            _logger.LogInformation("Job {JobId}: Generating {Format} report file", jobId, jobRequest.Format);

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
                reportBytes = await _reportGenerationService.GenerateConnectivityPdfAsync(
                    reportData,
                    reportTitle,
                    jobRequest.FilterByStatus,
                    cancellationToken);
                contentType = "application/pdf";
                fileExtension = "pdf";
            }
            else
            {
                reportBytes = await _reportGenerationService.GenerateConnectivityExcelAsync(
                    reportData,
                    reportTitle,
                    cancellationToken);
                contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                fileExtension = "xlsx";
            }

            var fileSizeBytes = reportBytes.Length;
            _logger.LogInformation("Job {JobId}: Report file generated. Size: {FileSize} bytes", jobId, fileSizeBytes);

            await UpdateJobStatusAsync(jobId, ConnectivityReportJobStatusEnum.Processing,
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
            var downloadUrl = $"/api/reports/connectivity/job/{jobId}/download";

            await UpdateJobStatusAsync(jobId, ConnectivityReportJobStatusEnum.Completed,
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

            await UpdateJobStatusAsync(jobId, ConnectivityReportJobStatusEnum.Failed,
                $"Report generation failed: {ex.Message}",
                startTime: startTime,
                endTime: DateTime.UtcNow,
                currentStep: "Failed",
                cancellationToken: cancellationToken);

            throw;
        }
    }

    private async Task UpdateJobStatusAsync(
        string jobId,
        ConnectivityReportJobStatusEnum status,
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

        var statusDto = _memoryCache.GetOrCreate(cacheKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(CACHE_RETENTION_HOURS);
            return new ConnectivityReportJobStatusDto
            {
                JobId = jobId,
                Status = status,
                Message = message,
                LastUpdated = DateTime.UtcNow
            };
        });

        if (statusDto != null)
        {
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

            _memoryCache.Set(cacheKey, statusDto, TimeSpan.FromHours(CACHE_RETENTION_HOURS));
        }

        await Task.CompletedTask;
    }

    private async Task<string> GenerateReportTitleAsync(
        Guid? regionId,
        Guid? provinceId,
        Guid? municipalityId,
        CancellationToken cancellationToken)
    {
        var titleParts = new List<string> { "Connectivity Report" };

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
    /// Example: development/region-PH01/Connectivity_RegionI_YYYYMMDD_HHMMSS.xlsx
    /// </summary>
    private async Task<(string fileName, string blobPath)> GenerateFileNameAndBlobPathAsync(
        Guid? regionId,
        Guid? provinceId,
        Guid? municipalityId,
        string extension,
        CancellationToken cancellationToken)
    {
        var nameParts = new List<string> { "Connectivity" };
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

    private string SanitizeFileName(string fileName)
    {
        var invalids = System.IO.Path.GetInvalidFileNameChars();
        return string.Join("_", fileName.Split(invalids, StringSplitOptions.RemoveEmptyEntries)).TrimEnd('.');
    }

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

    // ── Geographic summary builders ──────────────────────────────────────────
    // Shared logic lives in ConnectivityReportSummaryHelper (NTC.QoS.Domain.Helper).
}
