using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
    private readonly INAPRepository _napRepository;
    private readonly ISpeedtestRepository _speedtestRepository;
    private readonly ILogger<GetWiredBroadbandReportQueryHandler> _logger;

    // Known wired broadband service providers based on the image
    private static readonly Dictionary<string, string> WiredProviderNames = new()
    {
        { "PLDT", "pldt" },
        { "Globe", "globe" },
        { "Converge", "converge" },
        { "Parasat", "parasat" },
        { "DCTech", "dctech" },
        { "Southern Cablevision", "southern" }
    };

    public GetWiredBroadbandReportQueryHandler(
        IMunicipalityRepository municipalityRepository,
        IBarangayRepository barangayRepository,
        INAPRepository napRepository,
        ISpeedtestRepository speedtestRepository,
        ILogger<GetWiredBroadbandReportQueryHandler> logger)
    {
        _municipalityRepository = municipalityRepository;
        _barangayRepository = barangayRepository;
        _napRepository = napRepository;
        _speedtestRepository = speedtestRepository;
        _logger = logger;
    }

    public async Task<ApiResponse<WiredBroadbandReportDto>> Handle(GetWiredBroadbandReportQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Generating wired broadband report with filters: RegionId={RegionId}, ProvinceId={ProvinceId}, MunicipalityId={MunicipalityId}",
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

            var municipalityIds = municipalities.Select(m => m.Id).ToList();
            var municipalityBroadbandData = new List<MunicipalityBroadbandDto>();

            // Process each municipality
            foreach (var municipality in municipalities)
            {
                var municipalityDto = await ProcessMunicipalityAsync(municipality, request, cancellationToken);
                
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
            var summary = GenerateSummary(municipalityBroadbandData);

            var result = new WiredBroadbandReportDto
            {
                Municipalities = municipalityBroadbandData,
                Summary = summary
            };

            _logger.LogInformation("Wired broadband report generated successfully. {TotalMunicipalities} municipalities, {TotalBarangays} barangays processed",
                summary.TotalMunicipalities, summary.TotalBarangays);

            return new ApiResponse<WiredBroadbandReportDto>
            {
                Succeeded = true,
                Message = $"Wired broadband report generated successfully for {summary.TotalMunicipalities} municipalities and {summary.TotalBarangays} barangays.",
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

    private async Task<MunicipalityBroadbandDto> ProcessMunicipalityAsync(
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
            var barangayDto = await ProcessBarangayAsync(barangay, request, cancellationToken);
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

    private async Task<BarangayBroadbandDto> ProcessBarangayAsync(
        Domain.Entities.Barangay barangay,
        GetWiredBroadbandReportQuery request,
        CancellationToken cancellationToken)
    {
        // Get NAP data (Network Access Points) for wired broadband services
        var napQuery = _napRepository.Query()
            .Include(n => n.ServiceProvider)
            .ThenInclude(sp => sp.Provider)
            .Include(n => n.ServiceProvider)
            .ThenInclude(sp => sp.Service)
            .Where(n => n.ServiceProvider.Service.Type == ServiceTypeEnum.Wired && 
                       barangay.Boundaries != null && 
                       n.Location != null &&
                       barangay.Boundaries.Intersects(n.Location));

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

        // Get fixed broadband speedtest data as additional indicator
        var speedtestQuery = _speedtestRepository.Query()
            .Include(s => s.ServiceProvider)
            .ThenInclude(sp => sp.Provider)
            .Include(s => s.ServiceProvider)
            .ThenInclude(sp => sp.Service)
            .Where(s => s.IsFixedBroadband && 
                       s.ServiceProvider.Service.Type == ServiceTypeEnum.Wired &&
                       barangay.Boundaries != null && 
                       s.Location != null &&
                       barangay.Boundaries.Intersects(s.Location));

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

        // Determine provider availability
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

    private ServiceProviderAvailabilityDto DetermineProviderAvailability(
        IEnumerable<Domain.Aggregates.NAP> naps, 
        IEnumerable<Domain.Entities.Speedtest> speedtests)
    {
        var availability = new ServiceProviderAvailabilityDto();

        // Combine data sources for provider detection
        var allProviders = naps.Select(n => n.ServiceProvider.Provider.Name.ToLowerInvariant())
            .Concat(speedtests.Select(s => s.ServiceProvider.Provider.Name.ToLowerInvariant()))
            .Distinct()
            .ToList();

        // Check for specific providers (case-insensitive matching)
        foreach (var provider in allProviders)
        {
            if (provider.Contains(WiredProviderNames["PLDT"]))
                availability.PLDT = true;
            else if (provider.Contains(WiredProviderNames["Globe"]))
                availability.Globe = true;
            else if (provider.Contains(WiredProviderNames["Converge"]))
                availability.Converge = true;
            else if (provider.Contains(WiredProviderNames["Parasat"]))
                availability.Parasat = true;
            else if (provider.Contains(WiredProviderNames["DCTech"]))
                availability.DCTech = true;
            else if (provider.Contains(WiredProviderNames["Southern Cablevision"]))
                availability.SouthernCablevision = true;
            else
                availability.Others = true; // Any other wired provider
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

    private static WiredBroadbandSummaryDto GenerateSummary(IEnumerable<MunicipalityBroadbandDto> municipalities)
    {
        var allBarangays = municipalities.SelectMany(m => m.Barangays).ToList();

        return new WiredBroadbandSummaryDto
        {
            TotalMunicipalities = municipalities.Count(),
            TotalBarangays = allBarangays.Count,
            ServedBarangays = allBarangays.Count(b => b.ServiceStatus == ServiceStatusEnum.Served),
            UnderservedBarangays = allBarangays.Count(b => b.ServiceStatus == ServiceStatusEnum.Underserved),
            UnservedBarangays = allBarangays.Count(b => b.ServiceStatus == ServiceStatusEnum.Unserved),
            GeneratedAt = DateTime.UtcNow
        };
    }
}