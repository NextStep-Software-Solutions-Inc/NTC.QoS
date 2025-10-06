using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NTC.QoS.Application.Features.Reports.WiredBroadband.Requests;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Reports;
using NTC.QoS.Infrastructure.Helper;

namespace NTC.QoS.Application.Features.Reports.WiredBroadband.Handlers.Queries;

public class GetWiredBroadbandReportQueryHandler : IRequestHandler<GetWiredBroadbandReportQuery, ApiResponse<WiredBroadbandReportDto>>
{
    private readonly IMunicipalityRepository _municipalityRepository;
    private readonly IBarangayRepository _barangayRepository;
    private readonly INAPRepository _napRepository;
    private readonly ISpeedtestRepository _speedtestRepository;
    private readonly IConnectivityAssessmentRepository _connectivityAssessmentRepository;
    private readonly ILogger<GetWiredBroadbandReportQueryHandler> _logger;

    public GetWiredBroadbandReportQueryHandler(
        IMunicipalityRepository municipalityRepository,
        IBarangayRepository barangayRepository,
        INAPRepository napRepository,
        ISpeedtestRepository speedtestRepository,
        IConnectivityAssessmentRepository connectivityAssessmentRepository,
        ILogger<GetWiredBroadbandReportQueryHandler> logger)
    {
        _municipalityRepository = municipalityRepository;
        _barangayRepository = barangayRepository;
        _napRepository = napRepository;
        _speedtestRepository = speedtestRepository;
        _connectivityAssessmentRepository = connectivityAssessmentRepository;
        _logger = logger;
    }

    public async Task<ApiResponse<WiredBroadbandReportDto>> Handle(GetWiredBroadbandReportQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Generating wired broadband report with filters: RegionId={RegionId}, ProvinceId={ProvinceId}, MunicipalityId={MunicipalityId} (Enhanced with Connectivity Assessments)",
                request.RegionId, request.ProvinceId, request.MunicipalityId);

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

            var municipalities = await municipalityQuery
                .OrderBy(m => m.Name)
                .ToListAsync(cancellationToken);

            _logger.LogInformation("Found {Count} municipalities matching filters", municipalities.Count);

            if (!municipalities.Any())
            {
                return new ApiResponse<WiredBroadbandReportDto>
                {
                    Succeeded = true,
                    Message = "No municipalities found matching the specified criteria.",
                    Data = new WiredBroadbandReportDto
                    {
                        Municipalities = [],
                        Summary = new WiredBroadbandSummaryDto()
                    }
                };
            }

            var municipalityBroadbandData = new List<MunicipalityBroadbandDto>();
            var allProvidersFound = new HashSet<string>();

            // Process each municipality using enhanced connectivity assessment approach
            foreach (var municipality in municipalities)
            {
                var municipalityDto = await ProcessMunicipalityEnhancedAsync(municipality, request, cancellationToken);
                
                // Collect all providers found
                foreach (var barangay in municipalityDto.Barangays)
                {
                    foreach (var provider in barangay.ProviderAvailability.AvailableProviders)
                    {
                        allProvidersFound.Add(provider);
                    }
                }
                
                // Apply filtering if requested
                if (request.IncludeOnlyServedAreas)
                {
                    municipalityDto.Barangays = municipalityDto.Barangays
                        .Where(b => b.ServiceStatus == ServiceStatusEnum.Served)
                        .ToList();
                }

                // Only include municipality if it has barangays (after filtering)
                if (municipalityDto.Barangays.Any())
                {
                    municipalityBroadbandData.Add(municipalityDto);
                }
            }

            // Generate summary
            var summary = GenerateSummary(municipalityBroadbandData, allProvidersFound.ToList());

            var result = new WiredBroadbandReportDto
            {
                Municipalities = municipalityBroadbandData,
                Summary = summary
            };

            _logger.LogInformation("Enhanced wired broadband report generated successfully. {TotalMunicipalities} municipalities, {TotalBarangays} barangays processed (leveraged connectivity assessments where available)",
                summary.TotalMunicipalities, summary.TotalBarangays);

            return new ApiResponse<WiredBroadbandReportDto>
            {
                Succeeded = true,
                Message = $"Wired broadband report generated successfully for {summary.TotalMunicipalities} municipalities and {summary.TotalBarangays} barangays (enhanced with connectivity assessments).",
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
    /// Enhanced municipality processing that leverages connectivity assessments where available,
    /// falling back to direct calculation for real-time data or when assessments are unavailable
    /// </summary>
    private async Task<MunicipalityBroadbandDto> ProcessMunicipalityEnhancedAsync(
        Domain.Entities.Municipality municipality, 
        GetWiredBroadbandReportQuery request, 
        CancellationToken cancellationToken)
    {
        // Check if we have recent connectivity assessment for this municipality
        var municipalityAssessment = await _connectivityAssessmentRepository
            .GetByLocationAsync(municipality.Id, LocationTypeEnum.Municipality, cancellationToken);

        var useAssessmentData = municipalityAssessment != null && 
                               municipalityAssessment.LastUpdated > DateTime.UtcNow.AddDays(-30) &&
                               !HasDateFilters(request); // Don't use cached data if user wants specific date range

        if (useAssessmentData)
        {
            _logger.LogInformation("Using connectivity assessment data for municipality {MunicipalityName} (last updated: {LastUpdated})",
                municipality.Name, municipalityAssessment!.LastUpdated);
        }

        // Get barangays for this municipality
        var barangays = await _barangayRepository
            .Query(b => b.MunicipalityId == municipality.Id)
            .OrderBy(b => b.Name)
            .ToListAsync(cancellationToken);

        var barangayBroadbandData = new List<BarangayBroadbandDto>();

        foreach (var barangay in barangays)
        {
            BarangayBroadbandDto barangayDto;

            // Check for barangay-level assessment first
            var barangayAssessment = await _connectivityAssessmentRepository
                .GetByLocationAsync(barangay.Id, LocationTypeEnum.Barangay, cancellationToken);

            var useBarangayAssessment = barangayAssessment != null && 
                                       barangayAssessment.LastUpdated > DateTime.UtcNow.AddDays(-30) &&
                                       !HasDateFilters(request);

            if (useBarangayAssessment)
            {
                // Use pre-calculated connectivity assessment data
                barangayDto = await ProcessBarangayFromAssessmentAsync(barangay, barangayAssessment!, cancellationToken);
                _logger.LogDebug("Used connectivity assessment for barangay {BarangayName}", barangay.Name);
            }
            else
            {
                // Fallback to real-time calculation (original logic)
                barangayDto = await ProcessBarangayDirectAsync(barangay, request, cancellationToken);
                _logger.LogDebug("Used direct calculation for barangay {BarangayName}", barangay.Name);
            }

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
    /// Process barangay using pre-calculated connectivity assessment data (high performance)
    /// </summary>
    private async Task<BarangayBroadbandDto> ProcessBarangayFromAssessmentAsync(
        Domain.Entities.Barangay barangay,
        Domain.Entities.ConnectivityAssessment assessment,
        CancellationToken cancellationToken)
    {
        // Extract wired broadband provider information from assessment
        var providers = new List<string>();
        
        // If the assessment has a dominant provider for fixed broadband, include it
        if (!string.IsNullOrEmpty(assessment.DominantServiceProvider) && assessment.FiberNAPCount > 0)
        {
            providers.Add(assessment.DominantServiceProvider);
        }

        // For more accurate provider data, we could enhance the assessment to store provider breakdown
        // For now, we'll do a quick query for active providers if we need more detail
        if (assessment.FiberNAPCount > 0)
        {
            var activeProviders = await GetActiveWiredProvidersInBarangay(barangay.Id, cancellationToken);
            providers.AddRange(activeProviders);
            providers = providers.Distinct().ToList();
        }

        var providerAvailability = new ServiceProviderAvailabilityDto();
        foreach (var provider in providers)
        {
            providerAvailability.Providers[provider] = true;
        }

        // Map connectivity status from assessment to service status
        var serviceStatus = MapConnectivityStatusToServiceStatus(assessment.ConnectivityStatus);

        // Generate remarks based on assessment data
        var remarks = GenerateRemarksFromAssessment(assessment);

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
    /// Process barangay using direct calculation with enhanced envelope spatial filtering
    /// (aligns with connectivity assessment implementation)
    /// </summary>
    private async Task<BarangayBroadbandDto> ProcessBarangayDirectAsync(
        Domain.Entities.Barangay barangay,
        GetWiredBroadbandReportQuery request,
        CancellationToken cancellationToken)
    {
        // Enhanced NAP data collection with envelope spatial filtering
        var napQuery = _napRepository.Query()
            .Include(n => n.ServiceProvider)
            .ThenInclude(sp => sp.Provider)
            .Include(n => n.ServiceProvider)
            .ThenInclude(sp => sp.Service)
            .Where(n => n.ServiceProvider.Service.Type == ServiceTypeEnum.Wired);

        // Apply envelope spatial filtering (matches connectivity assessment approach)
        if (barangay.Boundaries != null)
        {
            napQuery = GeoQueryHelper.ApplyEnvelopeFilter(barangay.Boundaries, napQuery);
        }

        // Apply date filters if provided
        if (request.StartDate.HasValue)
        {
            napQuery = napQuery.Where(n => n.CreatedAt >= request.StartDate.Value);
        }

        if (request.EndDate.HasValue)
        {
            napQuery = napQuery.Where(n => n.CreatedAt <= request.EndDate.Value);
        }

        var naps = await napQuery.ToListAsync(cancellationToken);

        // Enhanced speedtest data collection with envelope spatial filtering
        var speedtestQuery = _speedtestRepository.Query()
            .Include(s => s.ServiceProvider)
            .ThenInclude(sp => sp.Provider)
            .Include(s => s.ServiceProvider)
            .ThenInclude(sp => sp.Service)
            .Where(s => s.IsFixedBroadband && 
                       s.ServiceProvider.Service.Type == ServiceTypeEnum.Wired);

        // Apply envelope spatial filtering (matches connectivity assessment approach)
        if (barangay.Boundaries != null)
        {
            speedtestQuery = GeoQueryHelper.ApplyEnvelopeFilter(barangay.Boundaries, speedtestQuery);
        }

        // Apply date filters for speedtests
        if (request.StartDate.HasValue)
        {
            speedtestQuery = speedtestQuery.Where(s => s.DateTime >= request.StartDate.Value);
        }

        if (request.EndDate.HasValue)
        {
            speedtestQuery = speedtestQuery.Where(s => s.DateTime <= request.EndDate.Value);
        }

        var speedtests = await speedtestQuery.ToListAsync(cancellationToken);

        // Determine provider availability dynamically
        var providerAvailability = DetermineProviderAvailability(naps, speedtests);
        
        // Determine service status and remarks
        var (serviceStatus, remarks) = DetermineServiceStatus(providerAvailability);

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
    /// Get active wired providers in a barangay for enhanced provider detail
    /// Uses envelope spatial filtering for consistency
    /// </summary>
    private async Task<List<string>> GetActiveWiredProvidersInBarangay(Guid barangayId, CancellationToken cancellationToken)
    {
        var barangayBoundary = await _barangayRepository.FirstOrDefaultAsync(
            _barangayRepository.Query(b => b.Id == barangayId).Select(b => b.Boundaries), 
            cancellationToken);

        if (barangayBoundary == null) return [];

        // Use envelope spatial filtering for consistent approach
        var napQuery = _napRepository.Query()
            .Where(n => n.ServiceProvider.Service.Type == ServiceTypeEnum.Wired);

        napQuery = GeoQueryHelper.ApplyEnvelopeFilter(barangayBoundary, napQuery);

        var providers = await _napRepository.ToListAsync(
            napQuery.Select(n => n.ServiceProvider.Provider.Name).Distinct(),
            cancellationToken);

        return providers.Where(p => !string.IsNullOrEmpty(p)).ToList();
    }

    /// <summary>
    /// Map connectivity status from assessment to report service status
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
    /// Generate remarks based on assessment data
    /// </summary>
    private static string GenerateRemarksFromAssessment(Domain.Entities.ConnectivityAssessment assessment)
    {
        var remarks = assessment.ConnectivityStatus switch
        {
            ConnectivityStatusEnum.Served => "SERVED",
            ConnectivityStatusEnum.Underserved => "UNDERSERVED", 
            ConnectivityStatusEnum.Unserved => "UNSERVED",
            _ => "UNSERVED"
        };

        // Enhance remarks with assessment details
        if (assessment.FiberNAPCount > 0)
        {
            remarks += $" ({assessment.FiberNAPCount} fiber NAPs)";
        }

        if (assessment.ServiceProviderCount > 1)
        {
            remarks += $" - {assessment.ServiceProviderCount} providers";
        }

        if (assessment.MeetsMinimumSpeedRequirement)
        {
            remarks += " - Meets speed requirements";
        }

        return remarks;
    }

    /// <summary>
    /// Check if the request has date filters that would require real-time calculation
    /// </summary>
    private static bool HasDateFilters(GetWiredBroadbandReportQuery request)
    {
        return request.StartDate.HasValue || request.EndDate.HasValue;
    }

    // ... Keep all existing helper methods (DetermineProviderAvailability, DetermineServiceStatus, GenerateSummary)
    
    private ServiceProviderAvailabilityDto DetermineProviderAvailability(
        IEnumerable<Domain.Aggregates.NAP> naps, 
        IEnumerable<Domain.Entities.Speedtest> speedtests)
    {
        var availability = new ServiceProviderAvailabilityDto();

        // Get unique provider names from both data sources
        var providerNames = naps.Select(n => n.ServiceProvider.Provider.Name.Trim())
            .Concat(speedtests.Select(s => s.ServiceProvider.Provider.Name.Trim()))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name)
            .ToList();

        // Set all found providers as available
        foreach (var providerName in providerNames)
        {
            availability.Providers[providerName] = true;
        }

        return availability;
    }

    private static (ServiceStatusEnum Status, string Remarks) DetermineServiceStatus(ServiceProviderAvailabilityDto availability)
    {
        var providerCount = availability.ProviderCount;

        return providerCount switch
        {
            0 => (ServiceStatusEnum.Unserved, "UNSERVED"),
            1 => (ServiceStatusEnum.Underserved, "UNDERSERVED"),
            >= 2 => (ServiceStatusEnum.Served, "SERVED"),
            _ => (ServiceStatusEnum.Unserved, "UNSERVED")
        };
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