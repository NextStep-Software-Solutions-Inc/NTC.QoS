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
/// Background job for generating wired broadband reports with progress tracking.
/// 
/// =============================================================================
/// MAIN RESPONSIBILITIES
/// =============================================================================
/// 1. Generate wired broadband reports (Excel/PDF) as background jobs
/// 2. Track progress through report generation steps
/// 3. Store generated reports in blob storage
/// 4. Provide real-time status updates via in-memory cache
/// 5. Handle errors and job failures gracefully
/// 
/// =============================================================================
/// PROCESS FLOW
/// =============================================================================
/// 1. Update job status to Processing with start time
/// 2. Fetch report data using MediatR query handler
/// 3. Track progress: Fetching data -> Generating report -> Saving file
/// 4. Generate report file (Excel or PDF) with includeRemarks option
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
/// Status cache key: wired_broadband_report_job_{jobId}
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
public class ProcessWiredBroadbandReportJob : IProcessWiredBroadbandReportJob
{
    private readonly ILogger<ProcessWiredBroadbandReportJob> _logger;
    private readonly IReportGenerationService _reportGenerationService;
    private readonly IBlobStorageService _blobStorageService;
    private readonly IMemoryCache _memoryCache;
    private readonly IMunicipalityRepository _municipalityRepository;
    private readonly IBarangayRepository _barangayRepository;
    private readonly IConnectivityAssessmentRepository _connectivityAssessmentRepository;
    private readonly IRepository<Province> _provinceRepository;
    private readonly IRepository<Region> _regionRepository;
    private readonly IConfiguration _configuration;

    private const string CACHE_KEY_PREFIX = "wired_broadband_report_job_";
    private const int CACHE_RETENTION_HOURS = 1;
    private const int RECENT_ASSESSMENT_DAYS = 30;
    private const string BLOB_CONTAINER_NAME = "reports";

    public ProcessWiredBroadbandReportJob(
        ILogger<ProcessWiredBroadbandReportJob> logger,
        IReportGenerationService reportGenerationService,
        IBlobStorageService blobStorageService,
        IMemoryCache memoryCache,
        IMunicipalityRepository municipalityRepository,
        IBarangayRepository barangayRepository,
        IConnectivityAssessmentRepository connectivityAssessmentRepository,
        IRepository<Province> provinceRepository,
        IRepository<Region> regionRepository,
        IConfiguration configuration)
    {
        _logger = logger;
        _reportGenerationService = reportGenerationService;
        _blobStorageService = blobStorageService;
        _memoryCache = memoryCache;
        _municipalityRepository = municipalityRepository;
        _barangayRepository = barangayRepository;
        _connectivityAssessmentRepository = connectivityAssessmentRepository;
        _provinceRepository = provinceRepository;
        _regionRepository = regionRepository;
        _configuration = configuration;
    }

    /// <summary>
    /// Executes the wired broadband report generation job
    /// </summary>
    [AutomaticRetry(Attempts = 2, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
    public async Task ExecuteAsync(WiredBroadbandReportJobRequestDto jobRequest, CancellationToken cancellationToken)
    {
        var jobId = jobRequest.JobId;
        var startTime = DateTime.UtcNow;

        try
        {
            _logger.LogInformation(
                "Starting wired broadband report job {JobId}. Format: {Format}, Region: {Region}, Province: {Province}, Municipality: {Municipality}, Status: {Status}, IncludeRemarks: {IncludeRemarks}, User: {UserId}",
                jobId, jobRequest.Format, jobRequest.RegionId, jobRequest.ProvinceId, jobRequest.MunicipalityId, jobRequest.FilterByStatus, jobRequest.IncludeRemarks, jobRequest.UserId);

            // Step 1: Update status to Processing
            await UpdateJobStatusAsync(jobId, WiredBroadbandReportJobStatusEnum.Processing,
                "Initializing report generation...",
                startTime: startTime,
                currentStep: "Initializing",
                progressPercentage: 0,
                format: jobRequest.Format,
                cancellationToken: cancellationToken);

            // Step 2: Fetch report data using repositories
            await UpdateJobStatusAsync(jobId, WiredBroadbandReportJobStatusEnum.Processing,
                "Fetching report data...",
                currentStep: "Fetching Data",
                progressPercentage: 10,
                cancellationToken: cancellationToken);

            _logger.LogInformation("Job {JobId}: Fetching municipalities with filters", jobId);

            // Build municipality query with geographic filters using subquery projection pattern
            // OPTIMIZATION: Use AsNoTracking + immediate projection to avoid geometry materialization
            var municipalityQuery = _municipalityRepository.QueryAsNoTracking();

            // Apply filters using subquery pattern
            if (jobRequest.RegionId.HasValue)
            {
                municipalityQuery = municipalityQuery.Where(m => m.Province.RegionId == jobRequest.RegionId.Value);
            }

            if (jobRequest.ProvinceId.HasValue)
            {
                municipalityQuery = municipalityQuery.Where(m => m.ProvinceId == jobRequest.ProvinceId.Value);
            }

            if (jobRequest.MunicipalityId.HasValue)
            {
                municipalityQuery = municipalityQuery.Where(m => m.Id == jobRequest.MunicipalityId.Value);
            }

            // Use subquery projection to select only required fields (Id, Name, Code)
            // This prevents loading geometry fields (Boundaries, Center, Area)
            var municipalities = await _municipalityRepository.ToListAsync(
                municipalityQuery
                    .Select(m => new { m.Id, m.Name, m.Code })
                    .OrderBy(m => m.Name),
                cancellationToken);
            var municipalityBroadbandData = new List<MunicipalityBroadbandDto>();

            _logger.LogInformation("Job {JobId}: Found {Count} municipalities, generating report data", jobId, municipalities.Count);

            // Process each municipality
            foreach (var municipality in municipalities)
            {
                var municipalityDto = await ProcessMunicipalityAsync(
                    municipality.Id, municipality.Name, municipality.Code, jobRequest, cancellationToken);
                
                if (municipalityDto != null && municipalityDto.Barangays.Any())
                {
                    municipalityBroadbandData.Add(municipalityDto);
                }
            }

            var totalMunicipalities = municipalityBroadbandData.Count;
            var totalBarangays = municipalityBroadbandData.Sum(m => m.Barangays.Count);

            _logger.LogInformation(
                "Job {JobId}: Report data generated. Municipalities: {MunicipalityCount}, Barangays: {BarangayCount}",
                jobId, totalMunicipalities, totalBarangays);

            // Create report DTO
            var reportData = new WiredBroadbandReportDto
            {
                Municipalities = municipalityBroadbandData,
                Summary = GenerateSummary(municipalityBroadbandData),
                PageNumber = 1,
                PageSize = totalMunicipalities,
                TotalMunicipalities = totalMunicipalities,
                TotalPages = 1
            };

            await UpdateJobStatusAsync(jobId, WiredBroadbandReportJobStatusEnum.Processing,
                $"Data generated: {totalMunicipalities} municipalities, {totalBarangays} barangays",
                currentStep: "Generating Report",
                progressPercentage: 40,
                municipalitiesProcessed: totalMunicipalities,
                totalMunicipalities: totalMunicipalities,
                barangaysProcessed: totalBarangays,
                totalBarangays: totalBarangays,
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
                reportBytes = await _reportGenerationService.GenerateWiredBroadbandPdfAsync(
                    reportData,
                    reportTitle,
                    jobRequest.IncludeRemarks,
                    cancellationToken);
                contentType = "application/pdf";
                fileExtension = "pdf";
            }
            else
            {
                reportBytes = await _reportGenerationService.GenerateWiredBroadbandExcelAsync(
                    reportData,
                    reportTitle,
                    jobRequest.IncludeRemarks,
                    cancellationToken);
                contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                fileExtension = "xlsx";
            }

            var fileSizeBytes = reportBytes.Length;
            _logger.LogInformation("Job {JobId}: Report file generated. Size: {FileSize} bytes", jobId, fileSizeBytes);

            await UpdateJobStatusAsync(jobId, WiredBroadbandReportJobStatusEnum.Processing,
                $"Report file generated ({FormatFileSize(fileSizeBytes)})",
                currentStep: "Uploading File",
                progressPercentage: 80,
                cancellationToken: cancellationToken);

            // Step 4: Upload to blob storage
            _logger.LogInformation("Job {JobId}: Uploading report file to blob storage", jobId);

            var (fileName, blobPath) = await GenerateFileNameAndBlobPathAsync(jobRequest.RegionId, jobRequest.ProvinceId, jobRequest.MunicipalityId, fileExtension, cancellationToken);
            
            using var stream = new MemoryStream(reportBytes);
            await _blobStorageService.UploadAsync(BLOB_CONTAINER_NAME, blobPath, stream, contentType, cancellationToken);
            
            var fullBlobPath = $"{BLOB_CONTAINER_NAME}/{blobPath}";

            _logger.LogInformation("Job {JobId}: Report file uploaded successfully to {Path}", jobId, fullBlobPath);

            // Step 5: Update status to Completed
            var endTime = DateTime.UtcNow;
            var totalTime = endTime - startTime;
            var downloadUrl = $"/api/reports/wired-broadband/job/{jobId}/download";

            await UpdateJobStatusAsync(jobId, WiredBroadbandReportJobStatusEnum.Completed,
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

            await UpdateJobStatusAsync(jobId, WiredBroadbandReportJobStatusEnum.Failed,
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
        WiredBroadbandReportJobStatusEnum status,
        string message,
        DateTime? startTime = null,
        DateTime? endTime = null,
        string? currentStep = null,
        double? progressPercentage = null,
        int? municipalitiesProcessed = null,
        int? totalMunicipalities = null,
        int? barangaysProcessed = null,
        int? totalBarangays = null,
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
            return new WiredBroadbandReportJobStatusDto
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
            if (municipalitiesProcessed.HasValue) statusDto.MunicipalitiesProcessed = municipalitiesProcessed.Value;
            if (totalMunicipalities.HasValue) statusDto.TotalMunicipalities = totalMunicipalities.Value;
            if (barangaysProcessed.HasValue) statusDto.BarangaysProcessed = barangaysProcessed.Value;
            if (totalBarangays.HasValue) statusDto.TotalBarangays = totalBarangays.Value;
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
    /// Process municipality to generate report data using connectivity assessments
    /// </summary>
    private async Task<MunicipalityBroadbandDto?> ProcessMunicipalityAsync(
        Guid municipalityId,
        string municipalityName,
        string municipalityCode,
        WiredBroadbandReportJobRequestDto jobRequest,
        CancellationToken cancellationToken)
    {
        // Check if connectivity assessment exists for this municipality
        var municipalityAssessment = await _connectivityAssessmentRepository
            .GetByLocationAsync(municipalityId, LocationTypeEnum.Municipality, cancellationToken);

        if (municipalityAssessment == null || municipalityAssessment.LastUpdated <= DateTime.UtcNow.AddDays(-RECENT_ASSESSMENT_DAYS))
        {
            _logger.LogWarning("Connectivity assessment for municipality {MunicipalityName} (ID: {MunicipalityId}) is missing or stale. Skipping.",
                municipalityName, municipalityId);
            return null;
        }

        // Get barangays for this municipality using subquery projection
        // OPTIMIZATION: Project only needed fields (Id, Name, Code) to avoid geometry materialization
        var barangays = await _barangayRepository.ToListAsync(
            _barangayRepository.QueryAsNoTracking(b => b.MunicipalityId == municipalityId)
                .Select(b => new { b.Id, b.Name, b.Code })
                .OrderBy(b => b.Name),
            cancellationToken);

        var barangayBroadbandData = new List<BarangayBroadbandDto>();

        // Process barangays
        foreach (var barangay in barangays)
        {
            var barangayDto = await ProcessBarangayAsync(
                barangay.Id, barangay.Name, barangay.Code,
                municipalityAssessment, cancellationToken);
            
            // Apply status filter if specified
            if (!jobRequest.FilterByStatus.HasValue || barangayDto.ServiceStatus == jobRequest.FilterByStatus.Value)
            {
                barangayBroadbandData.Add(barangayDto);
            }
        }

        // Only return municipality if it has barangays (after filtering)
        if (!barangayBroadbandData.Any())
        {
            return null;
        }

        return new MunicipalityBroadbandDto
        {
            Id = municipalityId,
            Name = municipalityName,
            Code = municipalityCode,
            Barangays = barangayBroadbandData
        };
    }

    /// <summary>
    /// Process barangay using assessment data
    /// </summary>
    private async Task<BarangayBroadbandDto> ProcessBarangayAsync(
        Guid barangayId,
        string barangayName,
        string barangayCode,
        ConnectivityAssessment municipalityAssessment,
        CancellationToken cancellationToken)
    {
        // Check for barangay-level assessment first
        var barangayAssessment = await _connectivityAssessmentRepository
            .GetByLocationAsync(barangayId, LocationTypeEnum.Barangay, cancellationToken);

        var useBarangayAssessment = barangayAssessment != null &&
            barangayAssessment.LastUpdated > DateTime.UtcNow.AddDays(-RECENT_ASSESSMENT_DAYS);

        var assessment = useBarangayAssessment ? barangayAssessment! : municipalityAssessment;
        var isBarangayLevel = useBarangayAssessment;

        // Extract providers with wired infrastructure
        var providers = new List<string>();
        if (assessment.ServiceProviderCount > 0)
        {
            var providerBreakdown = await _connectivityAssessmentRepository
                .GetProvidersByAssessmentIdAsync(assessment.Id, cancellationToken);

            // Add providers that have fiber NAPs (wired infrastructure)
            var fiberProviders = providerBreakdown
                .Where(p => p.FiberNAPCount > 0)
                .Select(p => p.ProviderName)
                .ToList();

            providers.AddRange(fiberProviders);
        }

        var providerAvailability = new ServiceProviderAvailabilityDto();
        foreach (var provider in providers.Distinct())
        {
            providerAvailability.Providers[provider] = true;
        }

        // Determine service status from fiber coverage score
        var serviceStatus = DetermineWiredBroadbandStatus(assessment.FiberCoverageScore);
        
        // Generate remarks
        var remarks = GenerateRemarksFromAssessment(assessment, providerAvailability.ProviderCount, isBarangayLevel);

        return new BarangayBroadbandDto
        {
            Id = barangayId,
            Name = barangayName,
            Code = barangayCode,
            ProviderAvailability = providerAvailability,
            ServiceStatus = serviceStatus,
            Remarks = remarks
        };
    }

    /// <summary>
    /// Determine wired broadband service status from fiber coverage score
    /// Score 3 = Served, Score 2 = Underserved, Score 1 or 0 = Unserved
    /// CRITICAL: FiberCoverageScore is an INT (1-3), not a double (0.0-1.0)
    /// </summary>
    private static ServiceStatusEnum DetermineWiredBroadbandStatus(int fiberCoverageScore)
    {
        return fiberCoverageScore switch
        {
            3 => ServiceStatusEnum.Served,
            2 => ServiceStatusEnum.Underserved,
            _ => ServiceStatusEnum.Unserved  // 1 or 0
        };
    }

    /// <summary>
    /// Generate remarks from connectivity assessment
    /// </summary>
    private static string GenerateRemarksFromAssessment(ConnectivityAssessment assessment, int fiberProviderCount, bool isBarangayLevel)
    {
        var remarks = new List<string>();

        if (assessment.FiberCoverageScore >= 0.8)
        {
            remarks.Add($"Strong wired broadband coverage ({fiberProviderCount} provider{(fiberProviderCount != 1 ? "s" : "")})");
        }
        else if (assessment.FiberCoverageScore >= 0.5)
        {
            remarks.Add($"Moderate wired broadband coverage ({fiberProviderCount} provider{(fiberProviderCount != 1 ? "s" : "")})");
        }
        else if (assessment.FiberNAPCount > 0)
        {
            remarks.Add($"Limited wired infrastructure ({assessment.FiberNAPCount} NAP{(assessment.FiberNAPCount != 1 ? "s" : "")})");
        }
        else
        {
            remarks.Add("No wired broadband infrastructure detected");
        }

        if (!isBarangayLevel)
        {
            remarks.Add("Using municipality-level assessment");
        }

        return string.Join(". ", remarks);
    }

    /// <summary>
    /// Generate summary statistics from report data
    /// </summary>
    private static WiredBroadbandSummaryDto GenerateSummary(List<MunicipalityBroadbandDto> municipalities)
    {
        var allBarangays = municipalities.SelectMany(m => m.Barangays).ToList();
        var allProviders = new HashSet<string>();

        // Collect all unique providers from all barangays
        foreach (var barangay in allBarangays)
        {
            foreach (var provider in barangay.ProviderAvailability.AvailableProviders)
            {
                allProviders.Add(provider);
            }
        }

        var allProvidersList = allProviders.OrderBy(p => p).ToList();

        // Calculate provider coverage (how many barangays each provider serves)
        var providerCoverage = new Dictionary<string, int>();
        foreach (var provider in allProvidersList)
        {
            var coverageCount = allBarangays.Count(b => 
                b.ProviderAvailability.Providers.TryGetValue(provider, out var isAvailable) && isAvailable);
            providerCoverage[provider] = coverageCount;
        }

        return new WiredBroadbandSummaryDto
        {
            TotalMunicipalities = municipalities.Count,
            TotalBarangays = allBarangays.Count,
            ServedBarangays = allBarangays.Count(b => b.ServiceStatus == ServiceStatusEnum.Served),
            UnderservedBarangays = allBarangays.Count(b => b.ServiceStatus == ServiceStatusEnum.Underserved),
            UnservedBarangays = allBarangays.Count(b => b.ServiceStatus == ServiceStatusEnum.Unserved),
            AllProviders = allProvidersList,
            ProviderCoverage = providerCoverage,
            GeneratedAt = DateTime.UtcNow
        };
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
        var titleParts = new List<string> { "Wired Broadband Coverage Report" };

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
    /// Example: development/region-PH01/WiredBroadband_RegionI_YYYYMMDD_HHMMSS.xlsx
    /// </summary>
    private async Task<(string fileName, string blobPath)> GenerateFileNameAndBlobPathAsync(
        Guid? regionId,
        Guid? provinceId,
        Guid? municipalityId,
        string extension,
        CancellationToken cancellationToken)
    {
        var nameParts = new List<string> { "WiredBroadband" };
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
