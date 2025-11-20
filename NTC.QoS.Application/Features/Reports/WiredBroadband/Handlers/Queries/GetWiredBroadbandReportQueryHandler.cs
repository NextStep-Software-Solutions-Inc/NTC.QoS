using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NTC.QoS.Application.Features.Analytics.Requests;
using NTC.QoS.Application.Features.Reports.WiredBroadband.Requests;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Reports;

namespace NTC.QoS.Application.Features.Reports.WiredBroadband.Handlers.Queries;

public class GetWiredBroadbandReportQueryHandler : IRequestHandler<GetWiredBroadbandReportQuery, ApiResponse<WiredBroadbandReportDto>>
{
    private readonly IMunicipalityRepository _municipalityRepository;
    private readonly IBarangayRepository _barangayRepository;
    private readonly IConnectivityAssessmentRepository _connectivityAssessmentRepository;
    private readonly IMediator _mediator;
    private readonly ILogger<GetWiredBroadbandReportQueryHandler> _logger;

    public GetWiredBroadbandReportQueryHandler(
        IMunicipalityRepository municipalityRepository,
        IBarangayRepository barangayRepository,
        IConnectivityAssessmentRepository connectivityAssessmentRepository,
        IMediator mediator,
        ILogger<GetWiredBroadbandReportQueryHandler> logger)
    {
        _municipalityRepository = municipalityRepository;
        _barangayRepository = barangayRepository;
        _connectivityAssessmentRepository = connectivityAssessmentRepository;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<ApiResponse<WiredBroadbandReportDto>> Handle(GetWiredBroadbandReportQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Generating wired broadband report with filters: RegionId={RegionId}, ProvinceId={ProvinceId}, MunicipalityId={MunicipalityId}, ForceAssessment={ForceAssessment}, DisablePagination={DisablePagination}, PageNumber={PageNumber}, PageSize={PageSize}",
                request.RegionId, request.ProvinceId, request.MunicipalityId, request.ForceGenerateConnectivityAssessment, request.DisablePagination, request.PageNumber, request.PageSize);

            // Validate pagination parameters (only when pagination is enabled)
            if (!request.DisablePagination)
            {
                if (request.PageNumber < 1)
                {
                    return new ApiResponse<WiredBroadbandReportDto>
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
                    return new ApiResponse<WiredBroadbandReportDto>
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
            var municipalityQuery = _municipalityRepository.Query()
                .Include(m => m.Province)
                .ThenInclude(p => p.Region)
                .AsQueryable();

            // Apply filters
            if (request.RegionId.HasValue)
            {
                municipalityQuery = municipalityQuery.Where(m => m.Province.RegionId == request.RegionId.Value);
            }

            if (request.ProvinceId.HasValue)
            {
                municipalityQuery = municipalityQuery.Where(m => m.ProvinceId == request.ProvinceId.Value);
            }

            if (request.MunicipalityId.HasValue)
            {
                municipalityQuery = municipalityQuery.Where(m => m.Id == request.MunicipalityId.Value);
            }

            // Get total count before pagination
            var totalMunicipalities = await municipalityQuery.CountAsync(cancellationToken);

            _logger.LogInformation("Found {Count} municipalities matching filters", totalMunicipalities);

            if (totalMunicipalities == 0)
            {
                return new ApiResponse<WiredBroadbandReportDto>
                {
                    Succeeded = true,
                    Message = "No municipalities found matching the specified criteria.",
                    Data = new WiredBroadbandReportDto
                    {
                        Municipalities = [],
                        Summary = new WiredBroadbandSummaryDto(),
                        PageNumber = request.DisablePagination ? 1 : request.PageNumber,
                        PageSize = request.DisablePagination ? totalMunicipalities : request.PageSize,
                        TotalMunicipalities = 0,
                        TotalPages = 0
                    }
                };
            }

            // Apply pagination conditionally
            List<Domain.Entities.Municipality> municipalities;
            
            if (request.DisablePagination)
            {
                _logger.LogInformation("Pagination disabled - fetching all {Count} municipalities", totalMunicipalities);
                municipalities = await municipalityQuery
                    .OrderBy(m => m.Name)
                    .ToListAsync(cancellationToken);
            }
            else
            {
                _logger.LogInformation("Pagination enabled - fetching page {PageNumber} (size: {PageSize})", request.PageNumber, request.PageSize);
                municipalities = await municipalityQuery
                    .OrderBy(m => m.Name)
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync(cancellationToken);
            }

            // If force recalculation is requested, trigger background job for assessment generation
            if (request.ForceGenerateConnectivityAssessment)
            {
                _logger.LogInformation("Force recalculation requested. Triggering connectivity assessment background job for {Count} municipalities", municipalities.Count);
                
                var municipalityIds = municipalities.Select(m => m.Id).ToList();
                
                // Trigger background job for municipality assessments with hierarchical barangay generation
                var jobRequest = new StartConnectivityAssessmentJobRequest
                {
                    LocationIds = municipalityIds,
                    LocationType = LocationTypeEnum.Municipality,
                    ForceRecalculate = true,
                    GenerateHierarchicalAssessments = true,
                    HierarchicalDepth = 1 // Generate barangay assessments
                };

                var jobResponse = await _mediator.Send(jobRequest, cancellationToken);
                
                if (!jobResponse.Succeeded)
                {
                    _logger.LogError("Failed to start connectivity assessment background job: {Message}", jobResponse.Message);
                    return new ApiResponse<WiredBroadbandReportDto>
                    {
                        Succeeded = false,
                        Message = "Failed to start connectivity assessment background job. Please try again.",
                        Errors = jobResponse.Errors ?? new Dictionary<string, string[]>
                        {
                            { "JobStartError", ["Failed to start background job for assessment generation"] }
                        }
                    };
                }

                _logger.LogInformation("Connectivity assessment background job started with JobId: {JobId}. Please wait for assessments to complete before generating report.", 
                    jobResponse.Data?.JobId);

                return new ApiResponse<WiredBroadbandReportDto>
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

            var municipalityBroadbandData = new List<MunicipalityBroadbandDto>();
            var allProvidersFound = new HashSet<string>();
            var municipalitiesWithoutAssessments = new List<string>();
            var barangaysWithoutAssessments = new List<string>();

            // Process each municipality using connectivity assessment approach
            foreach (var municipality in municipalities)
            {
                var municipalityDto = await ProcessMunicipalityFromAssessmentAsync(municipality, request, 
                    municipalitiesWithoutAssessments, barangaysWithoutAssessments, cancellationToken);
                
                if (municipalityDto == null)
                {
                    continue; // Municipality has no assessment
                }
                
                // Collect all providers found
                foreach (var barangay in municipalityDto.Barangays)
                {
                    foreach (var provider in barangay.ProviderAvailability.AvailableProviders)
                    {
                        allProvidersFound.Add(provider);
                    }
                }
                
                // Apply filtering if requested
                if (request.FilterByStatus.HasValue)
                {
                    municipalityDto.Barangays = municipalityDto.Barangays
                        .Where(b => b.ServiceStatus == request.FilterByStatus.Value)
                        .ToList();
                }

                // Only include municipality if it has barangays (after filtering)
                if (municipalityDto.Barangays.Count != 0)
                {
                    municipalityBroadbandData.Add(municipalityDto);
                }
            }

            // Generate summary for the current page or all data
            var summary = GenerateSummary(municipalityBroadbandData, allProvidersFound.ToList());

            // Calculate pagination metadata
            int totalPages;
            int pageNumber;
            int pageSize;
            
            if (request.DisablePagination)
            {
                totalPages = 1;
                pageNumber = 1;
                pageSize = totalMunicipalities;
            }
            else
            {
                totalPages = (int)Math.Ceiling((double)totalMunicipalities / request.PageSize);
                pageNumber = request.PageNumber;
                pageSize = request.PageSize;
            }

            var result = new WiredBroadbandReportDto
            {
                Municipalities = municipalityBroadbandData,
                Summary = summary,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalMunicipalities = totalMunicipalities,
                TotalPages = totalPages
            };

            // Build informational message about missing assessments
            var messageBuilder = new System.Text.StringBuilder();
            
            if (request.DisablePagination)
            {
                messageBuilder.Append($"Wired broadband report generated successfully with all data. ");
                messageBuilder.Append($"Showing {municipalityBroadbandData.Count} municipalities with {summary.TotalBarangays} barangays using connectivity assessments. ");
            }
            else
            {
                messageBuilder.Append($"Wired broadband report generated successfully for page {pageNumber} of {totalPages}. ");
                messageBuilder.Append($"Showing {municipalityBroadbandData.Count} municipalities with {summary.TotalBarangays} barangays using connectivity assessments. ");
                messageBuilder.Append($"Total municipalities matching filters: {totalMunicipalities}.");
            }

            if (municipalitiesWithoutAssessments.Count > 0 || barangaysWithoutAssessments.Count > 0)
            {
                messageBuilder.Append($" Note: {municipalitiesWithoutAssessments.Count} municipalities and {barangaysWithoutAssessments.Count} barangays on this page are missing connectivity assessments and were excluded from the report.");
                messageBuilder.Append(" To include them, trigger a connectivity assessment job first.");
            }

            _logger.LogInformation("Wired broadband report generated successfully. Pagination: {PaginationStatus}, Page {PageNumber}/{TotalPages}, {TotalMunicipalities} municipalities, {TotalBarangays} barangays processed. Missing assessments: {MissingMunicipalities} municipalities, {MissingBarangays} barangays",
                request.DisablePagination ? "Disabled" : "Enabled", pageNumber, totalPages, summary.TotalMunicipalities, summary.TotalBarangays, municipalitiesWithoutAssessments.Count, barangaysWithoutAssessments.Count);

            return new ApiResponse<WiredBroadbandReportDto>
            {
                Succeeded = true,
                Message = messageBuilder.ToString(),
                Data = result
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while generating wired broadband report");
            return new ApiResponse<WiredBroadbandReportDto>
            {
                Succeeded = false,
                Message = "An error occurred while generating the wired broadband report.",
                Errors = new Dictionary<string, string[]>
                {
                    { "ReportGenerationError", [ex.Message] }
                }
            };
        }
    }

    /// <summary>
    /// Process municipality using connectivity assessment data only
    /// Returns null if municipality assessment is not available
    /// </summary>
    private async Task<MunicipalityBroadbandDto?> ProcessMunicipalityFromAssessmentAsync(
        Domain.Entities.Municipality municipality,
        GetWiredBroadbandReportQuery request,
        List<string> municipalitiesWithoutAssessments,
        List<string> barangaysWithoutAssessments,
        CancellationToken cancellationToken)
    {
        // Check if connectivity assessment exists for this municipality
        var municipalityAssessment = await _connectivityAssessmentRepository
            .GetByLocationAsync(municipality.Id, LocationTypeEnum.Municipality, cancellationToken);

        if (municipalityAssessment == null || municipalityAssessment.LastUpdated <= DateTime.UtcNow.AddDays(-30))
        {
            var statusMessage = municipalityAssessment == null 
                ? "not found" 
                : $"stale (last updated: {municipalityAssessment.LastUpdated:yyyy-MM-dd})";

            _logger.LogWarning("Connectivity assessment for municipality {MunicipalityName} (ID: {MunicipalityId}) is {Status}. Skipping municipality.",
                municipality.Name, municipality.Id, statusMessage);
            
            municipalitiesWithoutAssessments.Add(municipality.Name);
            return null;
        }

        _logger.LogInformation("Using connectivity assessment for municipality {MunicipalityName} (last updated: {LastUpdated})",
            municipality.Name, municipalityAssessment.LastUpdated);

        // Get barangays for this municipality
        var barangays = await _barangayRepository
            .Query(b => b.MunicipalityId == municipality.Id)
            .OrderBy(b => b.Name)
            .ToListAsync(cancellationToken);

        var barangayBroadbandData = new List<BarangayBroadbandDto>();

        // Process barangays using assessments (prioritize barangay-level, fallback to municipality-level)
        foreach (var barangay in barangays)
        {
            var barangayDto = await ProcessBarangayFromAssessmentAsync(barangay, municipalityAssessment, barangaysWithoutAssessments, cancellationToken);
            barangayBroadbandData.Add(barangayDto);
        }

        return new MunicipalityBroadbandDto
        {
            Id = municipality.Id,
            Name = municipality.Name,
            Code = municipality.Code,
            Barangays = barangayBroadbandData
        };
    }

    /// <summary>
    /// Process barangay using assessment data (check barangay first, fallback to municipality)
    /// </summary>
    private async Task<BarangayBroadbandDto> ProcessBarangayFromAssessmentAsync(
        Domain.Entities.Barangay barangay,
        Domain.Entities.ConnectivityAssessment municipalityAssessment,
        List<string> barangaysWithoutAssessments,
        CancellationToken cancellationToken)
    {
        // Check for barangay-level assessment first (most accurate)
        var barangayAssessment = await _connectivityAssessmentRepository
            .GetByLocationAsync(barangay.Id, LocationTypeEnum.Barangay, cancellationToken);

        var useBarangayAssessment = barangayAssessment != null &&
            barangayAssessment.LastUpdated > DateTime.UtcNow.AddDays(-30);

        if (useBarangayAssessment)
        {
            // Use barangay-level assessment (highest priority)
            return await CreateBarangayDtoFromAssessmentAsync(barangay, barangayAssessment!, isBarangayLevel: true, cancellationToken);
        }
        else
        {
            // Use municipality-level assessment as approximation
            if (barangayAssessment == null)
            {
                barangaysWithoutAssessments.Add($"{barangay.Name} (using municipality estimate)");
            }
            
            return await CreateBarangayDtoFromMunicipalityAssessmentAsync(barangay, municipalityAssessment, cancellationToken);
        }
    }

    /// <summary>
    /// Create BarangayBroadbandDto from barangay-level connectivity assessment data
    /// </summary>
    private async Task<BarangayBroadbandDto> CreateBarangayDtoFromAssessmentAsync(
        Domain.Entities.Barangay barangay,
        Domain.Entities.ConnectivityAssessment assessment,
        bool isBarangayLevel,
        CancellationToken cancellationToken)
    {
        var providers = new List<string>();

        // Extract ALL provider information from assessment (not just dominant + fiber check)
        if (assessment.ServiceProviderCount > 0)
        {
            // Get all providers from the provider breakdown
            var providerBreakdown = await _connectivityAssessmentRepository
                .GetProvidersByAssessmentIdAsync(assessment.Id, cancellationToken);

            _logger.LogDebug("Barangay {BarangayName}: Found {ProviderCount} providers in breakdown, {FiberProviderCount} with fiber NAPs",
                barangay.Name, providerBreakdown.Count, providerBreakdown.Count(p => p.FiberNAPCount > 0));

            // Add all providers that have wired infrastructure (fiber NAPs)
            var fiberProviders = providerBreakdown
                .Where(p => p.FiberNAPCount > 0)
                .Select(p => p.ProviderName)
                .ToList();
        
            providers.AddRange(fiberProviders);

            // Log the providers found
            if (fiberProviders.Count != 0)
            {
                _logger.LogDebug("Barangay {BarangayName}: Fiber providers: {Providers}",
                barangay.Name, string.Join(", ", fiberProviders));
            }

        }

        var providerAvailability = new ServiceProviderAvailabilityDto();
        foreach (var provider in providers.Distinct())
        {
            providerAvailability.Providers[provider] = true;
        }

        // For wired broadband, use fiber coverage score to determine status
        var serviceStatus = DetermineWiredBroadbandStatus(assessment.FiberCoverageScore);
        
        // Pass actual fiber provider count instead of total ServiceProviderCount
        var actualFiberProviderCount = providerAvailability.ProviderCount;
        var remarks = GenerateRemarksFromAssessment(assessment, actualFiberProviderCount, isBarangayLevel);

        return new BarangayBroadbandDto
        {
            Id = barangay.Id,
            Name = barangay.Name,
            Code = barangay.Code,
            ProviderAvailability = providerAvailability,
            ServiceStatus = serviceStatus,
            Remarks = remarks
        };
    }

    /// <summary>
    /// Create BarangayBroadbandDto using municipality-level assessment as approximation
    /// </summary>
    private async Task<BarangayBroadbandDto> CreateBarangayDtoFromMunicipalityAssessmentAsync(
        Domain.Entities.Barangay barangay,
        Domain.Entities.ConnectivityAssessment municipalityAssessment,
        CancellationToken cancellationToken)
    {
        var providers = new List<string>();

        // Use municipality assessment as approximation for barangay
        if (municipalityAssessment.ServiceProviderCount > 0)
        {
            // Get all providers from municipality assessment
            var providerBreakdown = await _connectivityAssessmentRepository
                .GetProvidersByAssessmentIdAsync(municipalityAssessment.Id, cancellationToken);

            _logger.LogDebug("Barangay {BarangayName} (using municipality estimate): Found {ProviderCount} providers in breakdown, {FiberProviderCount} with fiber NAPs",
                barangay.Name, providerBreakdown.Count, providerBreakdown.Count(p => p.FiberNAPCount > 0));

            // Add all providers that have wired infrastructure
            var fiberProviders = providerBreakdown
                .Where(p => p.FiberNAPCount > 0)
                .Select(p => p.ProviderName)
                .ToList();

            providers.AddRange(fiberProviders);

            // Log the providers found
            if (fiberProviders.Count != 0)
            {
                _logger.LogDebug("Barangay {BarangayName} (using municipality estimate): Fiber providers: {Providers}",
                barangay.Name, string.Join(", ", fiberProviders));
            }

            // Fallback to dominant provider
            if (providers.Count == 0 && !string.IsNullOrEmpty(municipalityAssessment.DominantServiceProvider))
            {
                providers.Add(municipalityAssessment.DominantServiceProvider);
                _logger.LogDebug("Barangay {BarangayName} (using municipality estimate): No fiber providers found, using dominant provider: {Provider}",
                    barangay.Name, municipalityAssessment.DominantServiceProvider);
            }
        }

        var providerAvailability = new ServiceProviderAvailabilityDto();
        foreach (var provider in providers.Distinct())
        {
            providerAvailability.Providers[provider] = true;
        }

        // For wired broadband, use fiber coverage score to determine status
        var serviceStatus = DetermineWiredBroadbandStatus(municipalityAssessment.FiberCoverageScore);
        
        // Pass actual fiber provider count instead of total ServiceProviderCount
        var actualFiberProviderCount = providerAvailability.ProviderCount;
        var remarks = GenerateRemarksFromMunicipalityAssessment(municipalityAssessment, actualFiberProviderCount);

        return new BarangayBroadbandDto
        {
            Id = barangay.Id,
            Name = barangay.Name,
            Code = barangay.Code,
            ProviderAvailability = providerAvailability,
            ServiceStatus = serviceStatus,
            Remarks = remarks
        };
    }

    /// <summary>
    /// Map connectivity status from assessment to report service status
    /// For wired broadband, status is determined solely by Fiber Coverage Score
    /// Score 3 = Served, Score 2 = Underserved, Score 1 = Unserved
    /// </summary>
    private static ServiceStatusEnum MapConnectivityStatusToServiceStatus(ConnectivityStatusEnum connectivityStatus)
    {
        return connectivityStatus switch
        {
            ConnectivityStatusEnum.Served => ServiceStatusEnum.Served,
            ConnectivityStatusEnum.Underserved => ServiceStatusEnum.Underserved,
            ConnectivityStatusEnum.Unserved => ServiceStatusEnum.Unserved,
            _ => ServiceStatusEnum.Unserved
        };
    }

    /// <summary>
    /// Determine wired broadband service status based solely on Fiber Coverage Score
    /// This is specific to wired broadband reporting and differs from general connectivity assessment
    /// Score 3 = Served, Score 2 = Underserved, Score 1 = Unserved
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
    /// Generate remarks based on assessment data with level indicator
    /// For wired broadband, remarks emphasize fiber coverage
    /// </summary>
    private static string GenerateRemarksFromAssessment(Domain.Entities.ConnectivityAssessment assessment, int actualFiberProviderCount, bool isBarangayLevel)
    {
        // For wired broadband, status is determined by Fiber Coverage Score
        var remarks = assessment.FiberCoverageScore switch
        {
            3 => "SERVED",
            2 => "UNDERSERVED",
            _ => "UNSERVED"
        };

        // Enhance remarks with assessment details
        if (assessment.FiberNAPCount > 0)
        {
         remarks += $" ({assessment.FiberNAPCount} fiber NAPs)";
        }

        // Use actual fiber provider count from providerAvailability instead of total ServiceProviderCount
        // Show provider count for 1 or more providers (changed from > 1 to >= 1)
        if (actualFiberProviderCount >= 1)
        {
            var providerWord = actualFiberProviderCount == 1 ? "provider" : "providers";
            remarks += $" - {actualFiberProviderCount} {providerWord}";
        }

        if (assessment.MeetsMinimumSpeedRequirement)
        {
            remarks += " - Meets speed requirements";
        }

        var levelIndicator = isBarangayLevel ? "Barangay" : "Municipality";
        remarks += $" ({levelIndicator} Assessment: {assessment.LastUpdated:MMM dd})";

        return remarks;
    }

    /// <summary>
    /// Generate remarks based on municipality-level assessment used as approximation
    /// For wired broadband, remarks emphasize fiber coverage
    /// </summary>
    private static string GenerateRemarksFromMunicipalityAssessment(Domain.Entities.ConnectivityAssessment assessment, int actualFiberProviderCount)
    {
      // For wired broadband, status is determined by Fiber Coverage Score
        var remarks = assessment.FiberCoverageScore switch
        {
            3 => "SERVED",
            2 => "UNDERSERVED",
            _ => "UNSERVED"
        };

        remarks += " (Municipality estimate)";

        // Use actual fiber provider count instead of total ServiceProviderCount
        // Show provider count for 1 or more providers (changed from > 0 to >= 1 for consistency)
        if (actualFiberProviderCount >= 1)
        {
            var providerWord = actualFiberProviderCount == 1 ? "provider" : "providers";
            remarks += $" - {actualFiberProviderCount} {providerWord} in area";
        }

        return remarks;
    }

    private static WiredBroadbandSummaryDto GenerateSummary(IEnumerable<MunicipalityBroadbandDto> municipalities, List<string> allProviders)
    {
        var allBarangays = municipalities.SelectMany(m => m.Barangays).ToList();

        // Calculate provider coverage
        var providerCoverage = new Dictionary<string, int>();
        foreach (var provider in allProviders)
        {
            var coverageCount = allBarangays.Count(b => b.ProviderAvailability.Providers.ContainsKey(provider) &&
                b.ProviderAvailability.Providers[provider]);
            providerCoverage[provider] = coverageCount;
        }

        return new WiredBroadbandSummaryDto
        {
            TotalMunicipalities = municipalities.Count(),
            TotalBarangays = allBarangays.Count,
            ServedBarangays = allBarangays.Count(b => b.ServiceStatus == ServiceStatusEnum.Served),
            UnderservedBarangays = allBarangays.Count(b => b.ServiceStatus == ServiceStatusEnum.Underserved),
            UnservedBarangays = allBarangays.Count(b => b.ServiceStatus == ServiceStatusEnum.Unserved),
            AllProviders = allProviders,
            ProviderCoverage = providerCoverage,
            GeneratedAt = DateTime.UtcNow
        };
    }
}
