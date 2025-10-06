using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NTC.QoS.Application.Features.Analytics.Requests;
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
    private readonly IMediator _mediator;
    private readonly ILogger<GetWiredBroadbandReportQueryHandler> _logger;

    public GetWiredBroadbandReportQueryHandler(
        IMunicipalityRepository municipalityRepository,
        IBarangayRepository barangayRepository,
        INAPRepository napRepository,
        ISpeedtestRepository speedtestRepository,
        IConnectivityAssessmentRepository connectivityAssessmentRepository,
        IMediator mediator,
        ILogger<GetWiredBroadbandReportQueryHandler> logger)
    {
        _municipalityRepository = municipalityRepository;
        _barangayRepository = barangayRepository;
        _napRepository = napRepository;
        _speedtestRepository = speedtestRepository;
        _connectivityAssessmentRepository = connectivityAssessmentRepository;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<ApiResponse<WiredBroadbandReportDto>> Handle(GetWiredBroadbandReportQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Generating wired broadband report with filters: RegionId={RegionId}, ProvinceId={ProvinceId}, MunicipalityId={MunicipalityId}, ForceAssessment={ForceAssessment}",
                request.RegionId, request.ProvinceId, request.MunicipalityId, request.ForceGenerateConnectivityAssessment);

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

            if (municipalities.Count == 0)
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

            // Process each municipality using connectivity assessment approach
            foreach (var municipality in municipalities)
            {
                var municipalityDto = await ProcessMunicipalityWithConnectivityAssessmentAsync(municipality, request, cancellationToken);
                
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
                if (municipalityDto.Barangays.Count != 0)
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

            _logger.LogInformation("Wired broadband report generated successfully. {TotalMunicipalities} municipalities, {TotalBarangays} barangays processed using connectivity assessments",
                summary.TotalMunicipalities, summary.TotalBarangays);

            return new ApiResponse<WiredBroadbandReportDto>
            {
                Succeeded = true,
                Message = $"Wired broadband report generated successfully for {summary.TotalMunicipalities} municipalities and {summary.TotalBarangays} barangays using connectivity assessments.",
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
    /// Process municipality using connectivity assessment approach
    /// First checks for existing assessments, generates if needed, then uses assessment data for report
    /// </summary>
    private async Task<MunicipalityBroadbandDto> ProcessMunicipalityWithConnectivityAssessmentAsync(
        Domain.Entities.Municipality municipality, 
        GetWiredBroadbandReportQuery request, 
        CancellationToken cancellationToken)
    {
        // Step 1: Check if connectivity assessment exists for this municipality
        var existingAssessment = await _connectivityAssessmentRepository
            .GetByLocationAsync(municipality.Id, LocationTypeEnum.Municipality, cancellationToken);

        var shouldUseExistingAssessment = existingAssessment != null && 
                                        existingAssessment.LastUpdated > DateTime.UtcNow.AddDays(-30) &&
                                        !request.ForceGenerateConnectivityAssessment &&
                                        !HasDateFilters(request);

        if (!shouldUseExistingAssessment)
        {
            // Step 2: Generate connectivity assessment if not available or if forced
            _logger.LogInformation("Generating connectivity assessment for municipality {MunicipalityName} (ID: {MunicipalityId})",
                municipality.Name, municipality.Id);

            var assessmentRequest = new GenerateConnectivityAssessmentRequest
            {
                LocationIds = [municipality.Id],
                LocationType = LocationTypeEnum.Municipality,
                ForceRecalculate = request.ForceGenerateConnectivityAssessment
            };

            var assessmentResponse = await _mediator.Send(assessmentRequest, cancellationToken);
            
            if (!assessmentResponse.Succeeded)
            {
                _logger.LogWarning("Failed to generate connectivity assessment for municipality {MunicipalityName}: {Error}",
                    municipality.Name, assessmentResponse.Message);
                // Fall back to direct calculation if assessment generation fails
                return await ProcessMunicipalityDirectAsync(municipality, request, cancellationToken);
            }
            
            // Refresh the assessment after generation
            existingAssessment = await _connectivityAssessmentRepository
                .GetByLocationAsync(municipality.Id, LocationTypeEnum.Municipality, cancellationToken);
        }
        else
        {
            if (existingAssessment != null)
            {
                _logger.LogInformation("Using existing connectivity assessment for municipality {MunicipalityName} (last updated: {LastUpdated})",
                    municipality.Name, existingAssessment.LastUpdated);
            }
            else
            {
                _logger.LogInformation("No existing connectivity assessment found for municipality {MunicipalityName}",
                    municipality.Name);
            }
        }

        // Step 3: Get barangays for this municipality
        var barangays = await _barangayRepository
            .Query(b => b.MunicipalityId == municipality.Id)
            .OrderBy(b => b.Name)
            .ToListAsync(cancellationToken);

        var barangayBroadbandData = new List<BarangayBroadbandDto>();

        // Step 4: Process barangays using assessments (prioritize barangay-level, fallback to municipality-level)
        foreach (var barangay in barangays)
        {
            var barangayDto = await ProcessBarangayWithAssessmentAsync(barangay, municipality, existingAssessment, request, cancellationToken);
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
    /// Process barangay using assessment approach (check barangay first, fallback to municipality, then direct calculation)
    /// </summary>
    private async Task<BarangayBroadbandDto> ProcessBarangayWithAssessmentAsync(
        Domain.Entities.Barangay barangay,
        Domain.Entities.Municipality municipality,
        Domain.Entities.ConnectivityAssessment? municipalityAssessment,
        GetWiredBroadbandReportQuery request,
        CancellationToken cancellationToken)
    {
        // Check for barangay-level assessment first (most accurate)
        var barangayAssessment = await _connectivityAssessmentRepository
            .GetByLocationAsync(barangay.Id, LocationTypeEnum.Barangay, cancellationToken);

        var useBarangayAssessment = barangayAssessment != null && 
                                   barangayAssessment.LastUpdated > DateTime.UtcNow.AddDays(-30) &&
                                   !request.ForceGenerateConnectivityAssessment &&
                                   !HasDateFilters(request);

        if (useBarangayAssessment)
        {
            // Use barangay-level assessment (highest priority)
            return ProcessBarangayFromAssessment(barangay, barangayAssessment!);
        }
        else if (municipalityAssessment != null && !HasDateFilters(request))
        {
            // Use municipality-level assessment as approximation
            return ProcessBarangayFromMunicipalityAssessment(barangay, municipalityAssessment);
        }
        else
        {
            // Fallback to direct calculation (for real-time data or when assessments are unavailable)
            return await ProcessBarangayDirectAsync(barangay, request, cancellationToken);
        }
    }

    /// <summary>
    /// Process barangay using barangay-level connectivity assessment data
    /// </summary>
    private BarangayBroadbandDto ProcessBarangayFromAssessment(
        Domain.Entities.Barangay barangay,
        Domain.Entities.ConnectivityAssessment assessment)
    {
        var providers = new List<string>();
        
        // Extract provider information from assessment
        if (!string.IsNullOrEmpty(assessment.DominantServiceProvider) && assessment.FiberNAPCount > 0)
        {
            providers.Add(assessment.DominantServiceProvider);
        }

        var providerAvailability = new ServiceProviderAvailabilityDto();
        foreach (var provider in providers.Distinct())
        {
            providerAvailability.Providers[provider] = true;
        }

        // Map connectivity status to service status
        var serviceStatus = MapConnectivityStatusToServiceStatus(assessment.ConnectivityStatus);
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
    /// Process barangay using municipality-level assessment as approximation
    /// </summary>
    private BarangayBroadbandDto ProcessBarangayFromMunicipalityAssessment(
        Domain.Entities.Barangay barangay,
        Domain.Entities.ConnectivityAssessment municipalityAssessment)
    {
        var providers = new List<string>();
        
        // Use municipality assessment as approximation for barangay
        if (!string.IsNullOrEmpty(municipalityAssessment.DominantServiceProvider))
        {
            providers.Add(municipalityAssessment.DominantServiceProvider);
        }

        var providerAvailability = new ServiceProviderAvailabilityDto();
        foreach (var provider in providers.Distinct())
        {
            providerAvailability.Providers[provider] = true;
        }

        // Use municipality status but potentially downgrade for barangay level
        var serviceStatus = MapConnectivityStatusToServiceStatus(municipalityAssessment.ConnectivityStatus);
        var remarks = GenerateRemarksFromMunicipalityAssessment(municipalityAssessment);

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
    /// Process barangay using direct calculation (fallback method)
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
    /// Check if the request has date filters that would require real-time calculation
    /// </summary>
    private static bool HasDateFilters(GetWiredBroadbandReportQuery request)
    {
        return request.StartDate.HasValue || request.EndDate.HasValue;
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
    /// Generate remarks based on barangay-level assessment data
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

        remarks += $" (Assessment: {assessment.LastUpdated:MMM dd})";

        return remarks;
    }

    /// <summary>
    /// Generate remarks based on municipality-level assessment used as approximation
    /// </summary>
    private static string GenerateRemarksFromMunicipalityAssessment(Domain.Entities.ConnectivityAssessment assessment)
    {
        var remarks = assessment.ConnectivityStatus switch
        {
            ConnectivityStatusEnum.Served => "SERVED",
            ConnectivityStatusEnum.Underserved => "UNDERSERVED", 
            ConnectivityStatusEnum.Unserved => "UNSERVED",
            _ => "UNSERVED"
        };

        remarks += " (Municipality estimate)";

        if (assessment.ServiceProviderCount > 0)
        {
            remarks += $" - {assessment.ServiceProviderCount} providers in area";
        }

        return remarks;
    }

    /// <summary>
    /// Fallback method for direct municipality processing when assessments fail
    /// </summary>
    private async Task<MunicipalityBroadbandDto> ProcessMunicipalityDirectAsync(
        Domain.Entities.Municipality municipality, 
        GetWiredBroadbandReportQuery request, 
        CancellationToken cancellationToken)
    {
        // Get barangays for this municipality
        var barangays = await _barangayRepository
            .Query(b => b.MunicipalityId == municipality.Id)
            .OrderBy(b => b.Name)
            .ToListAsync(cancellationToken);

        var barangayBroadbandData = new List<BarangayBroadbandDto>();

        foreach (var barangay in barangays)
        {
            var barangayDto = await ProcessBarangayDirectAsync(barangay, request, cancellationToken);
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

    // ... Keep all existing helper methods
    
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