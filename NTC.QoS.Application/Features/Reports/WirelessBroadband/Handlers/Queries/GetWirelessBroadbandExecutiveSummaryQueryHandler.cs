using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NTC.QoS.Application.Features.Analytics.Requests;
using NTC.QoS.Application.Features.Reports.WirelessBroadband.Requests;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Reports;
using NTC.QoS.Infrastructure.Services;
using System.Text.Json;

namespace NTC.QoS.Application.Features.Reports.WirelessBroadband.Handlers.Queries;

/// <summary>
/// Handler for generating wireless broadband executive summary report.
/// Provides municipality and barangay-level wireless coverage breakdown with connectivity scores.
/// Uses pre-computed wireless coverage data from connectivity assessments for optimal performance.
/// </summary>
public class GetWirelessBroadbandExecutiveSummaryQueryHandler 
    : IRequestHandler<GetWirelessBroadbandExecutiveSummaryQuery, ApiResponse<WirelessBroadbandExecutiveSummaryDto>>
{
    private readonly IMunicipalityRepository _municipalityRepository;
    private readonly IBarangayRepository _barangayRepository;
    private readonly IBarangayCoverageRepository _barangayCoverageRepository;
    private readonly IConnectivityAssessmentRepository _connectivityAssessmentRepository;
    private readonly IWirelessScoringService _scoringService;
    private readonly IMediator _mediator;
    private readonly ILogger<GetWirelessBroadbandExecutiveSummaryQueryHandler> _logger;

    public GetWirelessBroadbandExecutiveSummaryQueryHandler(
        IMunicipalityRepository municipalityRepository,
        IBarangayRepository barangayRepository,
        IBarangayCoverageRepository barangayCoverageRepository,
        IConnectivityAssessmentRepository connectivityAssessmentRepository,
        IWirelessScoringService scoringService,
        IMediator mediator,
        ILogger<GetWirelessBroadbandExecutiveSummaryQueryHandler> logger)
    {
        _municipalityRepository = municipalityRepository;
        _barangayRepository = barangayRepository;
        _barangayCoverageRepository = barangayCoverageRepository;
        _connectivityAssessmentRepository = connectivityAssessmentRepository;
        _scoringService = scoringService;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<ApiResponse<WirelessBroadbandExecutiveSummaryDto>> Handle(
        GetWirelessBroadbandExecutiveSummaryQuery request, 
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Generating wireless broadband executive summary with filters: RegionId={RegionId}, ProvinceId={ProvinceId}, MunicipalityId={MunicipalityId}, ForceAssessment={ForceAssessment}, DisablePagination={DisablePagination}, PageNumber={PageNumber}, PageSize={PageSize}",
                request.RegionId, request.ProvinceId, request.MunicipalityId, 
                request.ForceGenerateConnectivityAssessment, request.DisablePagination, 
                request.PageNumber, request.PageSize);

            // Validate pagination parameters (only when pagination is enabled)
            if (!request.DisablePagination)
            {
                if (request.PageNumber < 1)
                {
                    return new ApiResponse<WirelessBroadbandExecutiveSummaryDto>
                    {
                        Succeeded = false,
                        Message = "PageNumber must be at least 1.",
                        Errors = new Dictionary<string, string[]>
                        {
                            { "PageNumber", ["PageNumber must be at least 1."] }
                        }
                    };
                }

                if (request.PageSize < 1 || request.PageSize > 100)
                {
                    return new ApiResponse<WirelessBroadbandExecutiveSummaryDto>
                    {
                        Succeeded = false,
                        Message = "PageSize must be between 1 and 100.",
                        Errors = new Dictionary<string, string[]>
                        {
                            { "PageSize", ["PageSize must be between 1 and 100."] }
                        }
                    };
                }
            }

            // Build municipality query with geographic filters
            var municipalityQuery = _municipalityRepository.QueryAsNoTracking();

            // Apply filters using precedence model: Municipality > Province > Region
            if (request.MunicipalityId.HasValue)
            {
                municipalityQuery = municipalityQuery.Where(m => m.Id == request.MunicipalityId.Value);
                _logger.LogDebug("Applying Municipality filter (ID: {MunicipalityId})", request.MunicipalityId.Value);
            }
            else if (request.ProvinceId.HasValue)
            {
                municipalityQuery = municipalityQuery.Where(m => m.ProvinceId == request.ProvinceId.Value);
                _logger.LogDebug("Applying Province filter (ID: {ProvinceId})", request.ProvinceId.Value);
            }
            else if (request.RegionId.HasValue)
            {
                municipalityQuery = municipalityQuery.Where(m => m.Province.RegionId == request.RegionId.Value);
                _logger.LogDebug("Applying Region filter (ID: {RegionId})", request.RegionId.Value);
            }

            // Get total count before pagination
            var totalMunicipalities = await _municipalityRepository.CountAsync(municipalityQuery, cancellationToken);

            _logger.LogInformation("Found {Count} municipalities matching filters", totalMunicipalities);

            if (totalMunicipalities == 0)
            {
                return new ApiResponse<WirelessBroadbandExecutiveSummaryDto>
                {
                    Succeeded = true,
                    Message = "No municipalities found matching the specified criteria.",
                    Data = new WirelessBroadbandExecutiveSummaryDto
                    {
                        Municipalities = [],
                        Summary = new WirelessBroadbandOverallSummaryDto(),
                        PageNumber = request.DisablePagination ? 1 : request.PageNumber,
                        PageSize = request.DisablePagination ? totalMunicipalities : request.PageSize,
                        TotalMunicipalities = 0,
                        TotalPages = 0
                    }
                };
            }

            // Fetch ALL municipalities for summary calculation
            var allMunicipalities = await _municipalityRepository.ToListAsync(
                municipalityQuery
                    .Select(m => new { m.Id, m.Name, m.Code })
                    .OrderBy(m => m.Name),
                cancellationToken);

            if (request.DisablePagination)
            {
                _logger.LogInformation("Pagination disabled - processing all {Count} municipalities", totalMunicipalities);
            }
            else
            {
                _logger.LogInformation(
                    "Pagination enabled - processing all {TotalCount} municipalities for summary, returning page {PageNumber} (size: {PageSize})", 
                    totalMunicipalities, request.PageNumber, request.PageSize);
            }

            // If force recalculation is requested, trigger background job
            if (request.ForceGenerateConnectivityAssessment)
            {
                _logger.LogInformation(
                    "Force recalculation requested. Triggering connectivity assessment background job for {Count} municipalities", 
                    allMunicipalities.Count);
                
                var municipalityIds = allMunicipalities.Select(m => m.Id).ToList();
                
                var jobRequest = new StartConnectivityAssessmentJobRequest
                {
                    MunicipalityIds = municipalityIds,
                    ForceRecalculate = true,
                    GenerateHierarchicalAssessments = true,
                    HierarchicalDepth = 1 // Generate barangay assessments
                };

                var jobResponse = await _mediator.Send(jobRequest, cancellationToken);
                
                if (!jobResponse.Succeeded)
                {
                    _logger.LogError("Failed to start connectivity assessment background job: {Message}", jobResponse.Message);
                    return new ApiResponse<WirelessBroadbandExecutiveSummaryDto>
                    {
                        Succeeded = false,
                        Message = "Failed to start connectivity assessment background job. Please try again.",
                        Errors = jobResponse.Errors ?? new Dictionary<string, string[]>
                        {
                            { "JobStartError", ["Failed to start background job for assessment generation"] }
                        }
                    };
                }

                _logger.LogInformation(
                    "Connectivity assessment background job started with JobId: {JobId}. Please wait for assessments to complete.", 
                    jobResponse.Data?.JobId);

                return new ApiResponse<WirelessBroadbandExecutiveSummaryDto>
                {
                    Succeeded = false,
                    Message = $"Connectivity assessments are being recalculated in background (Job ID: {jobResponse.Data?.JobId}). Please wait a few minutes and try generating the report again. Estimated time: {jobResponse.Data?.EstimatedDurationMinutes} minutes.",
                    Errors = new Dictionary<string, string[]>
                    {
                        { "AssessmentInProgress", 
                        [$"Background job {jobResponse.Data?.JobId} is generating connectivity assessments. Check job status endpoint or try again in {jobResponse.Data?.EstimatedDurationMinutes} minutes."] }
                    }
                };
            }

            // OPTIMIZATION: Load ALL barangay wireless coverage data ONCE (same pattern as GetWirelessBroadbandReportHandler)
            _logger.LogInformation("Loading wireless coverage from assessments for all municipalities...");
            var allBarangayIds = await _barangayCoverageRepository
                .ToListAsync(
                    _barangayCoverageRepository.Query()
                        .Where(bc => allMunicipalities.Select(m => m.Id).Contains(bc.Barangay.MunicipalityId))
                        .Select(bc => bc.BarangayId),
                    cancellationToken);
            
            var wirelessCoverageByBarangay = await LoadWirelessCoverageFromAssessmentsAsync(
                allBarangayIds,
                cancellationToken);
            
            _logger.LogInformation("Loaded wireless coverage for {Count}/{Total} barangays", 
                wirelessCoverageByBarangay.Count, allBarangayIds.Count);

            // Process ALL municipalities for accurate summary calculation
            var allMunicipalityBroadbandData = new List<MunicipalityWirelessBroadbandSummaryDto>();
            var allProvidersFound = new HashSet<string>();
            var technologyDistribution = new Dictionary<string, int>
            {
                ["5G"] = 0,
                ["4G"] = 0,
                ["3G"] = 0,
                ["2G"] = 0,
                ["None"] = 0
            };
            var barangaysWithoutAssessments = new List<string>();
            var totalConnectivityScore = 0.0;
            var totalBarangaysForScoring = 0;

            // Prefilter summary counters — always reflect the full dataset regardless of FilterByStatus
            var summaryTotalMunicipalities = 0;
            var summaryTotalBarangays = 0;
            var summaryServedBarangays = 0;
            var summaryUnderservedBarangays = 0;
            var summaryUnservedBarangays = 0;
            var summaryProviderCoverage = new Dictionary<string, int>();

            _logger.LogInformation("Processing all {Count} municipalities for summary calculation", allMunicipalities.Count);

            foreach (var municipality in allMunicipalities)
            {
                var municipalityDto = await ProcessMunicipalityWithPreloadedDataAsync(
                    municipality.Id, 
                    municipality.Name, 
                    municipality.Code, 
                    request,
                    wirelessCoverageByBarangay,
                    barangaysWithoutAssessments, 
                    cancellationToken);
                
                if (municipalityDto == null || !municipalityDto.Barangays.Any())
                {
                    continue; // Municipality has no barangays with coverage
                }
                
                // Collect statistics from all barangays BEFORE filtering
                foreach (var barangay in municipalityDto.Barangays)
                {
                    // Collect providers
                    foreach (var provider in barangay.ProviderAvailability.AvailableProviders)
                    {
                        allProvidersFound.Add(provider);
                        summaryProviderCoverage.TryGetValue(provider, out var existingCount);
                        summaryProviderCoverage[provider] = existingCount + 1;
                    }
                    
                    // Track technology distribution
                    if (!string.IsNullOrEmpty(barangay.HighestTechnology))
                    {
                        if (technologyDistribution.TryGetValue(barangay.HighestTechnology, out var currentCount))
                        {
                            technologyDistribution[barangay.HighestTechnology] = currentCount + 1;
                        }
                    }
                    
                    // Aggregate connectivity scores
                    totalConnectivityScore += barangay.ConnectivityScore;
                    totalBarangaysForScoring++;
                }

                // Accumulate prefilter summary counts
                summaryTotalMunicipalities++;
                summaryTotalBarangays += municipalityDto.Barangays.Count;
                summaryServedBarangays += municipalityDto.ServedBarangays;
                summaryUnderservedBarangays += municipalityDto.UnderservedBarangays;
                summaryUnservedBarangays += municipalityDto.UnservedBarangays;
                
                // Apply filtering if requested
                if (request.FilterByStatus.HasValue)
                {
                    municipalityDto.Barangays = municipalityDto.Barangays
                        .Where(b => b.ServiceStatus == request.FilterByStatus.Value)
                        .ToList();
                }

                // Only include municipality if it has barangays (after filtering)
                if (municipalityDto.Barangays.Any())
                {
                    allMunicipalityBroadbandData.Add(municipalityDto);
                }
            }

            // Generate summary from prefilter totals so it always reflects the full geographic dataset
            var summary = GenerateSummary(
                allProvidersFound.ToList(), 
                technologyDistribution,
                totalBarangaysForScoring > 0 ? totalConnectivityScore / totalBarangaysForScoring : 0,
                summaryTotalMunicipalities,
                summaryTotalBarangays,
                summaryServedBarangays,
                summaryUnderservedBarangays,
                summaryUnservedBarangays,
                summaryProviderCoverage);

            // IMPORTANT: Pagination must be based on municipalities that actually have report data.
            // totalMunicipalities includes municipalities that match geographic filters but have no valid assessments
            // (or are filtered out), which previously caused blank pages.
            var processedMunicipalityCount = allMunicipalityBroadbandData.Count;

            // Calculate pagination metadata based on PROCESSED municipalities
            int totalPages;
            int pageNumber;
            int pageSize;

            if (request.DisablePagination)
            {
                totalPages = 1;
                pageNumber = 1;
                pageSize = processedMunicipalityCount;
            }
            else
            {
                totalPages = processedMunicipalityCount > 0
                    ? (int)Math.Ceiling((double)processedMunicipalityCount / request.PageSize)
                    : 0;
                pageNumber = request.PageNumber;
                pageSize = request.PageSize;

                if (totalPages > 0 && pageNumber > totalPages)
                {
                    return new ApiResponse<WirelessBroadbandExecutiveSummaryDto>
                    {
                        Succeeded = false,
                        Message = $"PageNumber must be between 1 and {totalPages}.",
                        Errors = new Dictionary<string, string[]>
                        {
                            { "PageNumber", [$"PageNumber must be between 1 and {totalPages}."] }
                        }
                    };
                }
            }

            // Apply pagination to the processed municipalities list for the response
            var paginatedMunicipalityBroadbandData = request.DisablePagination
                ? allMunicipalityBroadbandData
                : allMunicipalityBroadbandData
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

            var result = new WirelessBroadbandExecutiveSummaryDto
            {
                Municipalities = paginatedMunicipalityBroadbandData,
                Summary = summary,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalMunicipalities = processedMunicipalityCount,
                TotalPages = totalPages
            };

            // Build informational message
            var messageBuilder = new System.Text.StringBuilder();
            
            if (request.DisablePagination)
            {
                messageBuilder.Append($"Wireless broadband report generated successfully with all data. ");
                messageBuilder.Append($"Showing {paginatedMunicipalityBroadbandData.Count} municipalities with {summary.TotalBarangays} barangays using connectivity assessments. ");
            }
            else
            {
                messageBuilder.Append($"Wireless broadband report generated successfully for page {pageNumber} of {totalPages}. ");
                messageBuilder.Append($"Showing {paginatedMunicipalityBroadbandData.Count} municipalities on this page. ");
                messageBuilder.Append($"Summary statistics reflect all {summary.TotalMunicipalities} municipalities ({summary.TotalBarangays} barangays) matching filters.");
            }

            if (barangaysWithoutAssessments.Count > 0)
            {
                messageBuilder.Append($" Note: {barangaysWithoutAssessments.Count} barangays are missing wireless coverage data and were excluded.");
            }

            _logger.LogInformation(
                "Wireless broadband report generated successfully. Pagination: {PaginationStatus}, Page {PageNumber}/{TotalPages}, {TotalMunicipalities} municipalities, {TotalBarangays} barangays processed. Missing coverage: {MissingBarangays} barangays",
                request.DisablePagination ? "Disabled" : "Enabled", 
                pageNumber, 
                totalPages, 
                summary.TotalMunicipalities, 
                summary.TotalBarangays, 
                barangaysWithoutAssessments.Count);

            return new ApiResponse<WirelessBroadbandExecutiveSummaryDto>
            {
                Succeeded = true,
                Message = messageBuilder.ToString(),
                Data = result
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while generating wireless broadband executive summary");
            return new ApiResponse<WirelessBroadbandExecutiveSummaryDto>
            {
                Succeeded = false,
                Message = "An error occurred while generating the wireless broadband report.",
                Errors = new Dictionary<string, string[]>
                {
                    { "ReportGenerationError", [ex.Message] }
                }
            };
        }
    }

    /// <summary>
    /// Process municipality using preloaded wireless coverage data.
    /// OPTIMIZED: Uses preloaded dictionary instead of querying for each barangay.
    /// </summary>
    private async Task<MunicipalityWirelessBroadbandSummaryDto?> ProcessMunicipalityWithPreloadedDataAsync(
        Guid municipalityId,
        string municipalityName,
        string municipalityCode,
        GetWirelessBroadbandExecutiveSummaryQuery request,
        Dictionary<Guid, BarangayWirelessSummaryDto> wirelessCoverageByBarangay,
        List<string> barangaysWithoutAssessments,
        CancellationToken cancellationToken)
    {
        // Get barangays for this municipality using BarangayCoverage to filter scope
        var coveredBarangayIds = _barangayCoverageRepository.Query()
            .Where(bc => bc.Barangay.MunicipalityId == municipalityId)
            .Select(bc => bc.BarangayId);
        
        var barangayQuery = _barangayRepository.QueryAsNoTracking()
            .Where(b => coveredBarangayIds.Contains(b.Id))
            .OrderBy(b => b.Name)
            .Select(b => new { b.Id, b.Name, b.Code });

        var barangays = await _barangayRepository.ToListAsync(barangayQuery, cancellationToken);

        if (!barangays.Any())
        {
            return null; // No barangays in coverage scope
        }

        var barangayDtos = new List<BarangayWirelessBroadbandSummaryDto>();

        // Process each barangay using preloaded data
        foreach (var barangay in barangays)
        {
            // Check if we have wireless coverage data for this barangay
            if (!wirelessCoverageByBarangay.TryGetValue(barangay.Id, out var wirelessSummary))
            {
                barangaysWithoutAssessments.Add($"{barangay.Name}");
                continue; // Skip barangays without coverage data
            }

            // Process barangay with actual wireless coverage data
            var barangayDto = ProcessBarangayWithWirelessData(
                barangay.Id,
                barangay.Name,
                barangay.Code,
                wirelessSummary);

            barangayDtos.Add(barangayDto);
        }

        if (!barangayDtos.Any())
        {
            return null; // No barangays with wireless coverage data
        }

        return new MunicipalityWirelessBroadbandSummaryDto
        {
            Id = municipalityId,
            Name = municipalityName,
            Code = municipalityCode,
            Barangays = barangayDtos
        };
    }

    /// <summary>
    /// Process barangay using actual wireless coverage data from assessment.
    /// Builds complete provider availability and calculates accurate connectivity scores.
    /// </summary>
    private BarangayWirelessBroadbandSummaryDto ProcessBarangayWithWirelessData(
        Guid barangayId,
        string barangayName,
        string barangayCode,
        BarangayWirelessSummaryDto wirelessSummary)
    {
        // Build provider availability with all providers and their technologies
        var providerAvailability = new WirelessProviderTechnologyAvailabilityDto();
        
        foreach (var provider in wirelessSummary.Providers)
        {
            var techInfo = new WirelessTechnologyDetailsDto
            {
                Has2G = provider.Has2G,
                Has3G = provider.Has3G,
                Has4G = provider.Has4G,
                Has5G = provider.Has5G,
                HighestGeneration = DetermineHighestGeneration(provider),
                AvailableGenerations = BuildAvailableGenerationsString(provider),
                AverageDownloadSpeedMbps = (double?)(provider.AverageDownloadSpeedMbps ?? 0),
                AverageUploadSpeedMbps = (double?)(provider.AverageUploadSpeedMbps ?? 0)
            };

            providerAvailability.Providers[provider.ProviderName] = techInfo;
        }

        // Calculate connectivity score using wireless scoring service
        var techBreakdown = BuildTechnologyBreakdownFromSummary(wirelessSummary);
        var speedStats = BuildSpeedStatsFromSummary(wirelessSummary);
        
        var technologyScore = _scoringService.CalculateTechnologyTypeScore(techBreakdown);
        var internetScore = _scoringService.CalculateInternetAccessScore(speedStats);
        var totalScore = technologyScore + internetScore;
        var serviceStatus = _scoringService.DetermineServiceStatus(totalScore);
        var highestTech = _scoringService.GetHighestTechnology(techBreakdown) ?? "None";

        // Build remarks
        var remarks = GenerateRemarks(serviceStatus, totalScore, highestTech, speedStats);

        // Calculate average speeds across all providers
        double? avgDownload = null;
        double? avgUpload = null;
        
        if (wirelessSummary.TotalSpeedtestCount > 0)
        {
            var providersWithSpeed = wirelessSummary.Providers
                .Where(p => p.SpeedtestCount > 0)
                .ToList();

            if (providersWithSpeed.Any())
            {
                avgDownload = (double)providersWithSpeed
                    .Average(p => p.AverageDownloadSpeedMbps ?? 0);
                avgUpload = (double)providersWithSpeed
                    .Average(p => p.AverageUploadSpeedMbps ?? 0);
            }
        }

        return new BarangayWirelessBroadbandSummaryDto
        {
            Id = barangayId,
            Name = barangayName,
            Code = barangayCode,
            ProviderAvailability = providerAvailability,
            ConnectivityScore = totalScore,
            ServiceStatus = serviceStatus,
            HighestTechnology = highestTech,
            AverageDownloadSpeedMbps = avgDownload,
            AverageUploadSpeedMbps = avgUpload,
            Remarks = remarks
        };
    }

    /// <summary>
    /// Load wireless coverage from connectivity assessments for the specified barangays.
    /// OPTIMIZATION: Reads pre-computed wireless coverage data from ConnectivityAssessment.WirelessCoverageJson
    /// Same pattern as GetWirelessBroadbandReportHandler for consistency.
    /// </summary>
    private async Task<Dictionary<Guid, BarangayWirelessSummaryDto>> LoadWirelessCoverageFromAssessmentsAsync(
        List<Guid> barangayIds,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<Guid, BarangayWirelessSummaryDto>();
        
        if (!barangayIds.Any())
            return result;

        // Query assessments for all barangays at once
        var assessments = await _connectivityAssessmentRepository.Query()
            .Where(a => barangayIds.Contains(a.LocationId) && a.LocationType == LocationTypeEnum.Barangay)
            .Where(a => !string.IsNullOrEmpty(a.WirelessCoverageJson))
            .Select(a => new
            {
                a.LocationId,
                a.WirelessCoverageJson
            })
            .ToListAsync(cancellationToken);

        _logger.LogInformation(
            "Found {AssessmentCount} assessments with wireless coverage data out of {TotalCount} barangays",
            assessments.Count, barangayIds.Count);

        // Deserialize wireless coverage JSON for each assessment
        var successCount = 0;
        var failCount = 0;
        foreach (var assessment in assessments)
        {
            try
            {
                if (!string.IsNullOrEmpty(assessment.WirelessCoverageJson))
                {
                    var wirelessSummary = JsonSerializer.Deserialize<BarangayWirelessSummaryDto>(
                        assessment.WirelessCoverageJson,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (wirelessSummary != null)
                    {
                        result[assessment.LocationId] = wirelessSummary;
                        successCount++;
                    }
                    else
                    {
                        failCount++;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, 
                    "Failed to deserialize wireless coverage JSON for barangay {BarangayId}", 
                    assessment.LocationId);
                failCount++;
            }
        }

        _logger.LogInformation(
            "Successfully loaded wireless coverage for {SuccessCount} barangays, {FailCount} failures",
            successCount, failCount);

        return result;
    }

    /// <summary>
    /// OLD METHOD - Kept for reference but no longer used.
    /// Replaced by ProcessMunicipalityWithPreloadedDataAsync for better performance.
    /// </summary>
    [Obsolete("Use ProcessMunicipalityWithPreloadedDataAsync instead")]
    private async Task<MunicipalityWirelessBroadbandSummaryDto?> ProcessMunicipalityFromAssessmentAsync(
        Guid municipalityId,
        string municipalityName,
        string municipalityCode,
        GetWirelessBroadbandExecutiveSummaryQuery request,
        List<string> municipalitiesWithoutAssessments,
        List<string> barangaysWithoutAssessments,
        CancellationToken cancellationToken)
    {
        // Check if connectivity assessment exists for this municipality
        // NOTE: No staleness check - use assessment if it exists (matches Excel report behavior)
        var municipalityAssessment = await _connectivityAssessmentRepository
            .GetByLocationAsync(municipalityId, LocationTypeEnum.Municipality, cancellationToken);

        if (municipalityAssessment == null)
        {
            _logger.LogWarning(
                "Connectivity assessment for municipality {MunicipalityName} (ID: {MunicipalityId}) not found. Skipping municipality.",
                municipalityName, municipalityId);
            
            municipalitiesWithoutAssessments.Add(municipalityName);
            return null;
        }

        _logger.LogInformation("Using connectivity assessment for municipality {MunicipalityName} (last updated: {LastUpdated})",
            municipalityName, municipalityAssessment.LastUpdated);

        // Get barangays for this municipality using BarangayCoverage to filter scope
        // CRITICAL: Only include barangays with BarangayCoverage records (same pattern as wired broadband)
        var coveredBarangayIds = _barangayCoverageRepository.Query()
            .Select(bc => bc.BarangayId);
        
        var barangayQuery = _barangayRepository.QueryAsNoTracking()
            .Where(b => b.MunicipalityId == municipalityId && coveredBarangayIds.Contains(b.Id))
            .OrderBy(b => b.Name)
            .Select(b => new { b.Id, b.Name, b.Code });

        var barangays = await _barangayRepository.ToListAsync(barangayQuery, cancellationToken);

        var barangayDtos = new List<BarangayWirelessBroadbandSummaryDto>();

        // Process each barangay - always returns a DTO (uses municipality fallback if needed)
        foreach (var barangay in barangays)
        {
            var barangayDto = await ProcessBarangayFromAssessmentAsync(
                barangay.Id,
                barangay.Name,
                barangay.Code,
                municipalityAssessment,
                barangaysWithoutAssessments,
                cancellationToken);

            // Always add - ProcessBarangayFromAssessmentAsync never returns null (uses fallback)
            barangayDtos.Add(barangayDto);
        }

        return new MunicipalityWirelessBroadbandSummaryDto
        {
            Id = municipalityId,
            Name = municipalityName,
            Code = municipalityCode,
            Barangays = barangayDtos
        };
    }

    /// <summary>
    /// Process barangay using connectivity assessment wireless coverage data.
    /// Uses barangay-level assessment if available, otherwise falls back to municipality assessment.
    /// Always returns a valid DTO (never null) - uses municipality fallback when barangay data is unavailable.
    /// </summary>
    private async Task<BarangayWirelessBroadbandSummaryDto> ProcessBarangayFromAssessmentAsync(
        Guid barangayId,
        string barangayName,
        string barangayCode,
        Domain.Entities.ConnectivityAssessment municipalityAssessment,
        List<string> barangaysWithoutAssessments,
        CancellationToken cancellationToken)
    {
        var barangayAssessment = await _connectivityAssessmentRepository
            .GetByLocationAsync(barangayId, LocationTypeEnum.Barangay, cancellationToken);

        // Check if we should use barangay assessment or fallback to municipality
        // NOTE: Unlike wired broadband, we don't enforce 30-day staleness for wireless coverage
        // because the Excel report (ProcessWirelessBroadbandReportJob) doesn't have this restriction
        // and we need to match those results
        var useBarangayAssessment = barangayAssessment != null &&
            !string.IsNullOrEmpty(barangayAssessment.WirelessCoverageJson);

        if (!useBarangayAssessment)
        {
            // Use municipality assessment as approximation when no wireless coverage data exists
            if (barangayAssessment == null || string.IsNullOrEmpty(barangayAssessment.WirelessCoverageJson))
            {
                barangaysWithoutAssessments.Add($"{barangayName} (no wireless coverage data)");
            }
            
            _logger.LogDebug(
                "Barangay {BarangayName}: No wireless coverage data available, using municipality assessment as estimate",
                barangayName);
            
            return CreateBarangayDtoFromMunicipalityAssessment(
                barangayId, barangayName, barangayCode, municipalityAssessment);
        }

        // Deserialize wireless coverage summary from barangay assessment
        // At this point, barangayAssessment and WirelessCoverageJson are guaranteed non-null
        if (string.IsNullOrEmpty(barangayAssessment!.WirelessCoverageJson))
        {
            // Should not reach here due to check above, but safety guard
            return CreateBarangayDtoFromMunicipalityAssessment(
                barangayId, barangayName, barangayCode, municipalityAssessment);
        }

        BarangayWirelessSummaryDto? wirelessSummary;
        try
        {
            wirelessSummary = JsonSerializer.Deserialize<BarangayWirelessSummaryDto>(
                barangayAssessment.WirelessCoverageJson);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Failed to deserialize wireless coverage JSON for barangay {BarangayName} (ID: {BarangayId}). Using municipality estimate.",
                barangayName, barangayId);
            
            barangaysWithoutAssessments.Add($"{barangayName} (deserialization failed)");
            // Use municipality assessment as fallback instead of returning null
            return CreateBarangayDtoFromMunicipalityAssessment(
                barangayId, barangayName, barangayCode, municipalityAssessment);
        }

        if (wirelessSummary == null || !wirelessSummary.Providers.Any())
        {
            _logger.LogDebug(
                "No wireless providers found for barangay {BarangayName} (ID: {BarangayId}). Using municipality estimate.",
                barangayName, barangayId);
            
            barangaysWithoutAssessments.Add($"{barangayName} (no providers in assessment)");
            // Use municipality assessment as fallback instead of returning null
            return CreateBarangayDtoFromMunicipalityAssessment(
                barangayId, barangayName, barangayCode, municipalityAssessment);
        }

        // Build provider availability
        var providerAvailability = new WirelessProviderTechnologyAvailabilityDto();
        
        foreach (var provider in wirelessSummary.Providers)
        {
            var techInfo = new WirelessTechnologyDetailsDto
            {
                Has2G = provider.Has2G,
                Has3G = provider.Has3G,
                Has4G = provider.Has4G,
                Has5G = provider.Has5G,
                HighestGeneration = DetermineHighestGeneration(provider),
                AvailableGenerations = BuildAvailableGenerationsString(provider),
                AverageDownloadSpeedMbps = (double?)(provider.AverageDownloadSpeedMbps ?? 0),
                AverageUploadSpeedMbps = (double?)(provider.AverageUploadSpeedMbps ?? 0)
            };

            providerAvailability.Providers[provider.ProviderName] = techInfo;
        }

        // Calculate connectivity score using wireless scoring service
        var techBreakdown = BuildTechnologyBreakdownFromSummary(wirelessSummary);
        var speedStats = BuildSpeedStatsFromSummary(wirelessSummary);
        
        var technologyScore = _scoringService.CalculateTechnologyTypeScore(techBreakdown);
        var internetScore = _scoringService.CalculateInternetAccessScore(speedStats);
        var totalScore = technologyScore + internetScore;
        var serviceStatus = _scoringService.DetermineServiceStatus(totalScore);
        var highestTech = _scoringService.GetHighestTechnology(techBreakdown) ?? "None";

        // Build remarks
        var remarks = GenerateRemarks(serviceStatus, totalScore, highestTech, speedStats);

        // Calculate average speeds across all providers
        double? avgDownload = null;
        double? avgUpload = null;
        
        if (wirelessSummary.TotalSpeedtestCount > 0)
        {
            var providersWithSpeed = wirelessSummary.Providers
                .Where(p => p.SpeedtestCount > 0)
                .ToList();

            if (providersWithSpeed.Any())
            {
                avgDownload = (double)providersWithSpeed
                    .Average(p => p.AverageDownloadSpeedMbps ?? 0);
                avgUpload = (double)providersWithSpeed
                    .Average(p => p.AverageUploadSpeedMbps ?? 0);
            }
        }

        return new BarangayWirelessBroadbandSummaryDto
        {
            Id = barangayId,
            Name = barangayName,
            Code = barangayCode,
            ProviderAvailability = providerAvailability,
            ConnectivityScore = totalScore,
            ServiceStatus = serviceStatus,
            HighestTechnology = highestTech,
            AverageDownloadSpeedMbps = avgDownload,
            AverageUploadSpeedMbps = avgUpload,
            Remarks = remarks
        };
    }

    /// <summary>
    /// Generate summary statistics from all municipality data
    /// </summary>
    private static WirelessBroadbandOverallSummaryDto GenerateSummary(
        List<string> allProviders,
        Dictionary<string, int> technologyDistribution,
        double averageConnectivityScore,
        int totalMunicipalities,
        int totalBarangays,
        int servedBarangays,
        int underservedBarangays,
        int unservedBarangays,
        Dictionary<string, int> preFilterProviderCoverage)
    {
        var summary = new WirelessBroadbandOverallSummaryDto
        {
            TotalMunicipalities = totalMunicipalities,
            TotalBarangays = totalBarangays,
            ServedBarangays = servedBarangays,
            UnderservedBarangays = underservedBarangays,
            UnservedBarangays = unservedBarangays,
            AllProviders = allProviders.OrderBy(p => p).ToList(),
            TechnologyDistribution = technologyDistribution,
            AverageConnectivityScore = Math.Round(averageConnectivityScore, 2)
        };

        // Use pre-filter provider coverage so it stays consistent with the pre-filter summary totals
        summary.ProviderCoverage = preFilterProviderCoverage;

        return summary;
    }

    /// <summary>
    /// Determine highest generation from provider coverage data
    /// </summary>
    private static string DetermineHighestGeneration(BarangayWirelessCoverageDto provider)
    {
        if (provider.Has5G) return "5G";
        if (provider.Has4G) return "4G";
        if (provider.Has3G) return "3G";
        if (provider.Has2G) return "2G";
        return "None";
    }

    /// <summary>
    /// Build available generations string (e.g., "3G, 4G, 5G")
    /// </summary>
    private static string BuildAvailableGenerationsString(BarangayWirelessCoverageDto provider)
    {
        var generations = new List<string>();
        if (provider.Has2G) generations.Add("2G");
        if (provider.Has3G) generations.Add("3G");
        if (provider.Has4G) generations.Add("4G");
        if (provider.Has5G) generations.Add("5G");
        return string.Join(", ", generations);
    }

    /// <summary>
    /// Create BarangayWirelessBroadbandSummaryDto using municipality-level assessment as approximation.
    /// This provides estimated coverage when barangay-specific assessment is unavailable.
    /// </summary>
    private BarangayWirelessBroadbandSummaryDto CreateBarangayDtoFromMunicipalityAssessment(
        Guid barangayId,
        string barangayName,
        string barangayCode,
        Domain.Entities.ConnectivityAssessment municipalityAssessment)
    {
        // For municipality approximation, we don't have detailed provider/technology data
        // Use simplified approach based on municipality's dominant provider and overall connectivity
        var providerAvailability = new WirelessProviderTechnologyAvailabilityDto();
        
        // Add dominant provider if available
        if (!string.IsNullOrEmpty(municipalityAssessment.DominantServiceProvider))
        {
            var techInfo = new WirelessTechnologyDetailsDto
            {
                // Assume modern technology if municipality is served
                Has4G = municipalityAssessment.ConnectivityStatus == ConnectivityStatusEnum.Served,
                HighestGeneration = municipalityAssessment.ConnectivityStatus == ConnectivityStatusEnum.Served ? "4G" : "3G",
                AvailableGenerations = municipalityAssessment.ConnectivityStatus == ConnectivityStatusEnum.Served ? "4G" : "3G"
            };
            providerAvailability.Providers[municipalityAssessment.DominantServiceProvider] = techInfo;
        }

        // Use municipality's connectivity status as baseline
        var serviceStatus = municipalityAssessment.ConnectivityStatus switch
        {
            ConnectivityStatusEnum.Served => ServiceStatusEnum.Served,
            ConnectivityStatusEnum.Underserved => ServiceStatusEnum.Underserved,
            _ => ServiceStatusEnum.Unserved
        };

        // Conservative scoring for municipality estimates
        var connectivityScore = serviceStatus switch
        {
            ServiceStatusEnum.Served => 5,
            ServiceStatusEnum.Underserved => 3,
            _ => 1
        };

        var remarks = $"{serviceStatus} (Municipality estimate - {municipalityAssessment.LastUpdated:MMM dd})";
        var highestTech = municipalityAssessment.ConnectivityStatus == ConnectivityStatusEnum.Served ? "4G" : "3G";

        return new BarangayWirelessBroadbandSummaryDto
        {
            Id = barangayId,
            Name = barangayName,
            Code = barangayCode,
            ProviderAvailability = providerAvailability,
            ConnectivityScore = connectivityScore,
            ServiceStatus = serviceStatus,
            HighestTechnology = highestTech,
            Remarks = remarks
        };
    }

    /// <summary>
    /// Build TechnologyBandBreakdownDto from BarangayWirelessSummaryDto for scoring
    /// </summary>
    private static Domain.Models.DTOs.Analytics.TechnologyBandBreakdownDto BuildTechnologyBreakdownFromSummary(
        BarangayWirelessSummaryDto summary)
    {
        var breakdown = new Domain.Models.DTOs.Analytics.TechnologyBandBreakdownDto();

        // Aggregate counts from all providers
        foreach (var provider in summary.Providers)
        {
            breakdown.Count2G += provider.Count2G;
            breakdown.Count3G += provider.Count3G;
            breakdown.Count4G += provider.Count4G;
            breakdown.Count5G += provider.Count5G;
        }

        breakdown.TotalTechnologyCount = breakdown.Count2G + breakdown.Count3G + 
                                         breakdown.Count4G + breakdown.Count5G;

        breakdown.AvailableGenerations = new List<string>();
        if (breakdown.Has2G) breakdown.AvailableGenerations.Add("2G");
        if (breakdown.Has3G) breakdown.AvailableGenerations.Add("3G");
        if (breakdown.Has4G) breakdown.AvailableGenerations.Add("4G");
        if (breakdown.Has5G) breakdown.AvailableGenerations.Add("5G");

        if (breakdown.TotalTechnologyCount > 0)
        {
            breakdown.ModernCoverageRatio = (double)(breakdown.Count4G + breakdown.Count5G) / 
                                           breakdown.TotalTechnologyCount;
            breakdown.HasModernCoverage = breakdown.ModernCoverageRatio > 0;

            var genCounts = new Dictionary<string, int>
            {
                ["2G"] = breakdown.Count2G,
                ["3G"] = breakdown.Count3G,
                ["4G"] = breakdown.Count4G,
                ["5G"] = breakdown.Count5G
            };
            var maxCount = genCounts.Max(kvp => kvp.Value);
            breakdown.DominantGeneration = genCounts.FirstOrDefault(kvp => kvp.Value == maxCount).Key;
        }

        return breakdown;
    }

    /// <summary>
    /// Build SpeedStatsDto from BarangayWirelessSummaryDto for scoring
    /// </summary>
    private static Domain.Models.DTOs.Analytics.SpeedStatsDto? BuildSpeedStatsFromSummary(
        BarangayWirelessSummaryDto summary)
    {
        if (summary.TotalSpeedtestCount == 0)
            return null;

        int totalTests = 0;
        decimal weightedDownloadSum = 0;
        decimal weightedUploadSum = 0;
        var downloadMedians = new List<decimal>();
        var uploadMedians = new List<decimal>();

        foreach (var provider in summary.Providers)
        {
            if (provider.SpeedtestCount == 0) continue;

            totalTests += provider.SpeedtestCount;

            if (provider.AverageDownloadSpeedMbps.HasValue)
                weightedDownloadSum += provider.AverageDownloadSpeedMbps.Value * provider.SpeedtestCount;
            
            if (provider.AverageUploadSpeedMbps.HasValue)
                weightedUploadSum += provider.AverageUploadSpeedMbps.Value * provider.SpeedtestCount;

            if (provider.MedianDownloadSpeedMbps.HasValue)
                downloadMedians.Add(provider.MedianDownloadSpeedMbps.Value);

            if (provider.MedianUploadSpeedMbps.HasValue)
                uploadMedians.Add(provider.MedianUploadSpeedMbps.Value);
        }

        if (totalTests == 0)
            return null;

        var avgDownload = weightedDownloadSum / totalTests;
        var avgUpload = weightedUploadSum / totalTests;

        downloadMedians.Sort();
        uploadMedians.Sort();

        var medianDownload = downloadMedians.Any() 
            ? CalculateMedian(downloadMedians) 
            : avgDownload;
        
        var medianUpload = uploadMedians.Any() 
            ? CalculateMedian(uploadMedians) 
            : avgUpload;

        return new Domain.Models.DTOs.Analytics.SpeedStatsDto
        {
            SpeedTestCount = totalTests,
            AverageDownloadSpeedMbps = avgDownload,
            AverageUploadSpeedMbps = avgUpload,
            MedianDownloadSpeedMbps = medianDownload,
            MedianUploadSpeedMbps = medianUpload
        };
    }

    /// <summary>
    /// Calculate median from sorted list of values
    /// </summary>
    private static decimal CalculateMedian(List<decimal> sortedValues)
    {
        int count = sortedValues.Count;
        
        return (count % 2 == 0)
            ? (sortedValues[count / 2 - 1] + sortedValues[count / 2]) / 2
            : sortedValues[count / 2];
    }

    /// <summary>
    /// Generate remarks based on service status and connectivity metrics
    /// </summary>
    private static string GenerateRemarks(
        ServiceStatusEnum serviceStatus, 
        int totalScore, 
        string highestTechnology,
        Domain.Models.DTOs.Analytics.SpeedStatsDto? speedStats)
    {
        var status = serviceStatus.ToString().ToUpper();
        var remarks = $"{status} (Score: {totalScore}/6)";

        if (!string.IsNullOrEmpty(highestTechnology) && highestTechnology != "None")
        {
            remarks += $" - {highestTechnology}";
        }

        if (speedStats != null && speedStats.AverageDownloadSpeedMbps.HasValue)
        {
            remarks += $" - Avg: {Math.Round((double)speedStats.AverageDownloadSpeedMbps.Value, 1)} Mbps";
        }

        return remarks;
    }
}
