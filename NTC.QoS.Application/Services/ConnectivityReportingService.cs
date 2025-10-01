using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.Reports;
using Microsoft.Extensions.Logging;

namespace NTC.QoS.Application.Services;

/// <summary>
/// Unified service for generating connectivity reports leveraging assessment data
/// </summary>
public interface IConnectivityReportingService
{
    Task<WiredBroadbandReportDto> GenerateWiredBroadbandReportAsync(
        Guid? regionId = null,
        Guid? provinceId = null, 
        Guid? municipalityId = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        bool includeOnlyServedAreas = false,
        CancellationToken cancellationToken = default);

    Task<WirelessBroadbandReportDto> GenerateWirelessBroadbandReportAsync(
        Guid? regionId = null,
        Guid? provinceId = null,
        Guid? municipalityId = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default);

    Task<ConnectivityGapReportDto> GenerateConnectivityGapReportAsync(
        Guid? regionId = null,
        Guid? provinceId = null,
        CancellationToken cancellationToken = default);
}

public class ConnectivityReportingService : IConnectivityReportingService
{
    private readonly IConnectivityAssessmentRepository _assessmentRepository;
    private readonly IMunicipalityRepository _municipalityRepository;
    private readonly IBarangayRepository _barangayRepository;
    private readonly ILogger<ConnectivityReportingService> _logger;

    public ConnectivityReportingService(
        IConnectivityAssessmentRepository assessmentRepository,
        IMunicipalityRepository municipalityRepository, 
        IBarangayRepository barangayRepository,
        ILogger<ConnectivityReportingService> logger)
    {
        _assessmentRepository = assessmentRepository;
        _municipalityRepository = municipalityRepository;
        _barangayRepository = barangayRepository;
        _logger = logger;
    }

    public async Task<WiredBroadbandReportDto> GenerateWiredBroadbandReportAsync(
        Guid? regionId = null,
        Guid? provinceId = null,
        Guid? municipalityId = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        bool includeOnlyServedAreas = false,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating unified wired broadband report using assessment-based approach");

        // Get relevant assessments based on geographic filters
        var assessments = await GetFilteredAssessmentsAsync(
            regionId, provinceId, municipalityId, LocationTypeEnum.Barangay, cancellationToken);

        // Filter by date if specified
        if (startDate.HasValue || endDate.HasValue)
        {
            assessments = assessments.Where(a => 
                (!startDate.HasValue || a.AssessmentDate >= startDate.Value) &&
                (!endDate.HasValue || a.AssessmentDate <= endDate.Value)).ToList();
        }

        // Transform assessments to report format
        var municipalities = await TransformAssessmentsToMunicipalitiesAsync(assessments, cancellationToken);

        // Apply served-only filter if requested
        if (includeOnlyServedAreas)
        {
            municipalities = municipalities.Select(m => new MunicipalityBroadbandDto
            {
                Id = m.Id,
                Name = m.Name,
                Code = m.Code,
                Barangays = m.Barangays.Where(b => b.ServiceStatus == ServiceStatusEnum.Served).ToList()
            }).Where(m => m.Barangays.Any()).ToList();
        }

        var allProviders = municipalities
            .SelectMany(m => m.Barangays)
            .SelectMany(b => b.ProviderAvailability.AvailableProviders)
            .Distinct()
            .ToList();

        var summary = GenerateWiredSummary(municipalities, allProviders);

        return new WiredBroadbandReportDto
        {
            Municipalities = municipalities,
            Summary = summary
        };
    }

    public async Task<WirelessBroadbandReportDto> GenerateWirelessBroadbandReportAsync(
        Guid? regionId = null,
        Guid? provinceId = null,
        Guid? municipalityId = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        // Similar implementation for wireless using assessment data
        // This would leverage the technology breakdown from connectivity assessments
        throw new NotImplementedException("Wireless report implementation pending");
    }

    public async Task<ConnectivityGapReportDto> GenerateConnectivityGapReportAsync(
        Guid? regionId = null,
        Guid? provinceId = null,
        CancellationToken cancellationToken = default)
    {
        // Generate report specifically focusing on unserved/underserved areas
        var assessments = await GetFilteredAssessmentsAsync(regionId, provinceId, null, LocationTypeEnum.Barangay, cancellationToken);
        
        var gaps = assessments
            .Where(a => a.ConnectivityStatus != ConnectivityStatusEnum.Served)
            .ToList();

        return new ConnectivityGapReportDto
        {
            TotalUnservedAreas = gaps.Count(a => a.ConnectivityStatus == ConnectivityStatusEnum.Unserved),
            TotalUnderservedAreas = gaps.Count(a => a.ConnectivityStatus == ConnectivityStatusEnum.Underserved),
            UnservedPopulation = gaps.Where(a => a.ConnectivityStatus == ConnectivityStatusEnum.Unserved)
                                   .Sum(a => a.Population ?? 0),
            UnderservedPopulation = gaps.Where(a => a.ConnectivityStatus == ConnectivityStatusEnum.Underserved)
                                      .Sum(a => a.Population ?? 0),
            PriorityAreas = gaps.OrderByDescending(a => a.Population ?? 0)
                              .Take(20)
                              .Select(a => new PriorityAreaDto
                              {
                                  LocationId = a.LocationId,
                                  LocationName = GetLocationName(a.LocationId, a.LocationType),
                                  ConnectivityStatus = a.ConnectivityStatus,
                                  Population = a.Population,
                                  TotalScore = a.TotalScore,
                                  LastAssessed = a.AssessmentDate
                              }).ToList(),
            GeneratedAt = DateTime.UtcNow
        };
    }

    private async Task<List<Domain.Entities.ConnectivityAssessment>> GetFilteredAssessmentsAsync(
        Guid? regionId, Guid? provinceId, Guid? municipalityId, LocationTypeEnum locationType, CancellationToken cancellationToken)
    {
        var query = _assessmentRepository.Query(a => a.LocationType == locationType);

        if (municipalityId.HasValue)
        {
            // Get all barangays in municipality
            var barangayIds = await _barangayRepository.ToListAsync(
                _barangayRepository.Query(b => b.MunicipalityId == municipalityId.Value).Select(b => b.Id), 
                cancellationToken);
            query = query.Where(a => barangayIds.Contains(a.LocationId));
        }
        else if (provinceId.HasValue)
        {
            // Get all barangays in province
            var barangayIds = await _barangayRepository.ToListAsync(
                _barangayRepository.Query(b => b.Municipality.ProvinceId == provinceId.Value).Select(b => b.Id),
                cancellationToken);
            query = query.Where(a => barangayIds.Contains(a.LocationId));
        }
        else if (regionId.HasValue)
        {
            // Get all barangays in region
            var barangayIds = await _barangayRepository.ToListAsync(
                _barangayRepository.Query(b => b.Municipality.Province.RegionId == regionId.Value).Select(b => b.Id),
                cancellationToken);
            query = query.Where(a => barangayIds.Contains(a.LocationId));
        }

        return await _assessmentRepository.ToListAsync(query, cancellationToken);
    }

    private async Task<List<MunicipalityBroadbandDto>> TransformAssessmentsToMunicipalitiesAsync(
        List<Domain.Entities.ConnectivityAssessment> assessments, CancellationToken cancellationToken)
    {
        var barangayIds = assessments.Select(a => a.LocationId).ToList();
        var barangays = await _barangayRepository.ToListAsync(
            _barangayRepository.Query(b => barangayIds.Contains(b.Id))
                .Select(b => new { b.Id, b.Name, b.Code, b.MunicipalityId, Municipality = new { b.Municipality.Id, b.Municipality.Name, b.Municipality.Code } }),
            cancellationToken);

        var municipalityGroups = barangays.GroupBy(b => b.Municipality.Id).ToList();
        var municipalities = new List<MunicipalityBroadbandDto>();

        foreach (var municipalityGroup in municipalityGroups)
        {
            var municipality = municipalityGroup.First().Municipality;
            var municipalityBarangays = new List<BarangayBroadbandDto>();

            foreach (var barangay in municipalityGroup)
            {
                var assessment = assessments.FirstOrDefault(a => a.LocationId == barangay.Id);
                if (assessment != null)
                {
                    municipalityBarangays.Add(TransformAssessmentToBarangay(barangay, assessment));
                }
            }

            municipalities.Add(new MunicipalityBroadbandDto
            {
                Id = municipality.Id,
                Name = municipality.Name,
                Code = municipality.Code,
                Barangays = municipalityBarangays
            });
        }

        return municipalities;
    }

    private BarangayBroadbandDto TransformAssessmentToBarangay(dynamic barangay, Domain.Entities.ConnectivityAssessment assessment)
    {
        var providers = new ServiceProviderAvailabilityDto();
        
        // Extract provider information from assessment
        if (!string.IsNullOrEmpty(assessment.DominantServiceProvider) && assessment.FiberNAPCount > 0)
        {
            providers.Providers[assessment.DominantServiceProvider] = true;
        }

        var serviceStatus = assessment.ConnectivityStatus switch
        {
            ConnectivityStatusEnum.Served => ServiceStatusEnum.Served,
            ConnectivityStatusEnum.Underserved => ServiceStatusEnum.Underserved,
            _ => ServiceStatusEnum.Unserved
        };

        var remarks = $"{serviceStatus.ToString().ToUpper()}";
        if (assessment.FiberNAPCount > 0)
            remarks += $" ({assessment.FiberNAPCount} fiber NAPs)";
        if (assessment.ServiceProviderCount > 1)
            remarks += $" - {assessment.ServiceProviderCount} providers";

        return new BarangayBroadbandDto
        {
            Id = barangay.Id,
            Name = barangay.Name,
            Code = barangay.Code,
            ProviderAvailability = providers,
            ServiceStatus = serviceStatus,
            Remarks = remarks
        };
    }

    private WiredBroadbandSummaryDto GenerateWiredSummary(List<MunicipalityBroadbandDto> municipalities, List<string> allProviders)
    {
        var allBarangays = municipalities.SelectMany(m => m.Barangays).ToList();
        
        var providerCoverage = new Dictionary<string, int>();
        foreach (var provider in allProviders)
        {
            var coverageCount = allBarangays.Count(b => b.ProviderAvailability.Providers.ContainsKey(provider) && 
                                                      b.ProviderAvailability.Providers[provider]);
            providerCoverage[provider] = coverageCount;
        }

        return new WiredBroadbandSummaryDto
        {
            TotalMunicipalities = municipalities.Count,
            TotalBarangays = allBarangays.Count,
            ServedBarangays = allBarangays.Count(b => b.ServiceStatus == ServiceStatusEnum.Served),
            UnderservedBarangays = allBarangays.Count(b => b.ServiceStatus == ServiceStatusEnum.Underserved),
            UnservedBarangays = allBarangays.Count(b => b.ServiceStatus == ServiceStatusEnum.Unserved),
            AllProviders = allProviders,
            ProviderCoverage = providerCoverage,
            GeneratedAt = DateTime.UtcNow
        };
    }

    private string GetLocationName(Guid locationId, LocationTypeEnum locationType)
    {
        // Implementation to get location name - could be cached or looked up
        return $"Location-{locationId}";
    }
}

// Supporting DTOs
public class WirelessBroadbandReportDto
{
    public List<MunicipalityBroadbandDto> Municipalities { get; set; } = [];
    public WirelessBroadbandSummaryDto Summary { get; set; } = new();
}

public class WirelessBroadbandSummaryDto
{
    public int TotalMunicipalities { get; set; }
    public int TotalBarangays { get; set; }
    public int CellCount2G { get; set; }
    public int CellCount3G { get; set; }
    public int CellCount4G { get; set; }
    public int CellCount5G { get; set; }
    public DateTime GeneratedAt { get; set; }
}

public class ConnectivityGapReportDto
{
    public int TotalUnservedAreas { get; set; }
    public int TotalUnderservedAreas { get; set; }
    public int UnservedPopulation { get; set; }
    public int UnderservedPopulation { get; set; }
    public List<PriorityAreaDto> PriorityAreas { get; set; } = [];
    public DateTime GeneratedAt { get; set; }
}

public class PriorityAreaDto
{
    public Guid LocationId { get; set; }
    public string LocationName { get; set; } = string.Empty;
    public ConnectivityStatusEnum ConnectivityStatus { get; set; }
    public int? Population { get; set; }
    public int TotalScore { get; set; }
    public DateTime LastAssessed { get; set; }
}