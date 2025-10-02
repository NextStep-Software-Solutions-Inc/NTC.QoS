using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.Reports;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

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

    /// <summary>
    /// Generate wired broadband report as PDF
    /// </summary>
    Task<byte[]> GenerateWiredBroadbandPdfAsync(
        Guid? regionId = null,
        Guid? provinceId = null,
        Guid? municipalityId = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        bool includeOnlyServedAreas = false,
        string reportTitle = "Wired Broadband Report",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate wired broadband report as Excel
    /// </summary>
    Task<byte[]> GenerateWiredBroadbandExcelAsync(
        Guid? regionId = null,
        Guid? provinceId = null,
        Guid? municipalityId = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        bool includeOnlyServedAreas = false,
        string reportTitle = "Wired Broadband Report",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate connectivity gap report as PDF
    /// </summary>
    Task<byte[]> GenerateConnectivityGapPdfAsync(
        Guid? regionId = null,
        Guid? provinceId = null,
        string reportTitle = "Connectivity Gap Analysis Report",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate connectivity gap report as Excel
    /// </summary>
    Task<byte[]> GenerateConnectivityGapExcelAsync(
        Guid? regionId = null,
        Guid? provinceId = null,
        string reportTitle = "Connectivity Gap Analysis Report",
        CancellationToken cancellationToken = default);
}

public class ConnectivityReportingService : IConnectivityReportingService
{
    private readonly IConnectivityAssessmentRepository _assessmentRepository;
    private readonly IMunicipalityRepository _municipalityRepository;
    private readonly IBarangayRepository _barangayRepository;
    private readonly IProvinceRepository _provinceRepository;
    private readonly IRegionRepository _regionRepository;
    private readonly IReportGenerationService _reportGenerationService;
    private readonly ILogger<ConnectivityReportingService> _logger;

    // Cache for location names during the request lifetime
    private readonly ConcurrentDictionary<(Guid, LocationTypeEnum), string> _locationNameCache = new();

    public ConnectivityReportingService(
        IConnectivityAssessmentRepository assessmentRepository,
        IMunicipalityRepository municipalityRepository, 
        IBarangayRepository barangayRepository,
        IProvinceRepository provinceRepository,
        IRegionRepository regionRepository,
        IReportGenerationService reportGenerationService,
        ILogger<ConnectivityReportingService> logger)
    {
        _assessmentRepository = assessmentRepository;
        _municipalityRepository = municipalityRepository;
        _barangayRepository = barangayRepository;
        _provinceRepository = provinceRepository;
        _regionRepository = regionRepository;
        _reportGenerationService = reportGenerationService;
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
            }).Where(m => m.Barangays.Count != 0).ToList();
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
        // Simulate async operation to avoid CS1998 warning
        await Task.Yield();
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

        // OPTIMIZED: Batch load location names instead of individual calls
        var priorityGaps = gaps.OrderByDescending(a => a.Population ?? 0).Take(20).ToList();
        var locationNames = await GetLocationNamesBatchAsync(
            priorityGaps.Select(a => (a.LocationId, a.LocationType)).ToList(), 
            cancellationToken);

        var priorityAreas = priorityGaps.Select(a => new PriorityAreaDto
        {
            LocationId = a.LocationId,
            LocationName = locationNames.GetValueOrDefault((a.LocationId, a.LocationType), $"Location-{a.LocationId}"),
            ConnectivityStatus = a.ConnectivityStatus,
            Population = a.Population,
            TotalScore = a.TotalScore,
            LastAssessed = a.AssessmentDate
        }).ToList();

        return new ConnectivityGapReportDto
        {
            TotalUnservedAreas = gaps.Count(a => a.ConnectivityStatus == ConnectivityStatusEnum.Unserved),
            TotalUnderservedAreas = gaps.Count(a => a.ConnectivityStatus == ConnectivityStatusEnum.Underserved),
            UnservedPopulation = gaps.Where(a => a.ConnectivityStatus == ConnectivityStatusEnum.Unserved)
                                   .Sum(a => a.Population ?? 0),
            UnderservedPopulation = gaps.Where(a => a.ConnectivityStatus == ConnectivityStatusEnum.Underserved)
                                      .Sum(a => a.Population ?? 0),
            PriorityAreas = priorityAreas,
            GeneratedAt = DateTime.UtcNow
        };
    }

    public async Task<byte[]> GenerateWiredBroadbandPdfAsync(
        Guid? regionId = null,
        Guid? provinceId = null,
        Guid? municipalityId = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        bool includeOnlyServedAreas = false,
        string reportTitle = "Wired Broadband Report",
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating wired broadband PDF report");

        var reportData = await GenerateWiredBroadbandReportAsync(
            regionId, provinceId, municipalityId, startDate, endDate, includeOnlyServedAreas, cancellationToken);

        return await _reportGenerationService.GenerateWiredBroadbandPdfAsync(reportData, reportTitle, cancellationToken);
    }

    public async Task<byte[]> GenerateWiredBroadbandExcelAsync(
        Guid? regionId = null,
        Guid? provinceId = null,
        Guid? municipalityId = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        bool includeOnlyServedAreas = false,
        string reportTitle = "Wired Broadband Report",
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating wired broadband Excel report");

        var reportData = await GenerateWiredBroadbandReportAsync(
            regionId, provinceId, municipalityId, startDate, endDate, includeOnlyServedAreas, cancellationToken);

        return await _reportGenerationService.GenerateWiredBroadbandExcelAsync(reportData, reportTitle, cancellationToken);
    }

    public async Task<byte[]> GenerateConnectivityGapPdfAsync(
        Guid? regionId = null,
        Guid? provinceId = null,
        string reportTitle = "Connectivity Gap Analysis Report",
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating connectivity gap PDF report");

        var reportData = await GenerateConnectivityGapReportAsync(regionId, provinceId, cancellationToken);

        return await _reportGenerationService.GenerateConnectivityGapPdfAsync(reportData, reportTitle, cancellationToken);
    }

    public async Task<byte[]> GenerateConnectivityGapExcelAsync(
        Guid? regionId = null,
        Guid? provinceId = null,
        string reportTitle = "Connectivity Gap Analysis Report",
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating connectivity gap Excel report");

        var reportData = await GenerateConnectivityGapReportAsync(regionId, provinceId, cancellationToken);

        return await _reportGenerationService.GenerateConnectivityGapExcelAsync(reportData, reportTitle, cancellationToken);
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

    /// <summary>
    /// Optimized batch method to retrieve multiple location names in minimal database calls
    /// </summary>
    private async Task<Dictionary<(Guid, LocationTypeEnum), string>> GetLocationNamesBatchAsync(
        List<(Guid LocationId, LocationTypeEnum LocationType)> locations, 
        CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<(Guid, LocationTypeEnum), string>();
        
        // Check cache first
        var uncachedLocations = new List<(Guid, LocationTypeEnum)>();
        foreach (var location in locations)
        {
            if (_locationNameCache.TryGetValue(location, out var cachedName))
            {
                result[location] = cachedName;
            }
            else
            {
                uncachedLocations.Add(location);
            }
        }

        if (uncachedLocations.Count == 0)
            return result;

        // Group by location type for efficient batch queries
        var locationGroups = uncachedLocations.GroupBy(l => l.Item2);

        var batchTasks = locationGroups.Select(async group =>
        {
            var locationType = group.Key;
            var locationIds = group.Select(g => g.Item1).ToList();

            try
            {
                var names = await GetLocationNamesByTypeAsync(locationType, locationIds, cancellationToken);
                
                // Cache and add to result
                foreach (var kvp in names)
                {
                    var key = (kvp.Key, locationType);
                    _locationNameCache.TryAdd(key, kvp.Value);
                    result[key] = kvp.Value;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error batch loading location names for type '{LocationType}'", locationType);
                
                // Add fallback names for failed locations
                foreach (var locationId in locationIds)
                {
                    var fallbackName = $"Location-{locationId}";
                    var key = (locationId, locationType);
                    _locationNameCache.TryAdd(key, fallbackName);
                    result[key] = fallbackName;
                }
            }
        });

        await Task.WhenAll(batchTasks);
        return result;
    }

    /// <summary>
    /// Batch load location names by type with a single query per type
    /// </summary>
    private async Task<Dictionary<Guid, string>> GetLocationNamesByTypeAsync(
        LocationTypeEnum locationType, 
        List<Guid> locationIds, 
        CancellationToken cancellationToken = default)
    {
        if (locationIds.Count == 0)
            return new Dictionary<Guid, string>();

        return locationType switch
        {
            LocationTypeEnum.Barangay => await _barangayRepository.ToListAsync(
                _barangayRepository.Query(b => locationIds.Contains(b.Id))
                    .Select(b => new { b.Id, b.Name }), cancellationToken)
                .ContinueWith(task => task.Result.ToDictionary(x => x.Id, x => x.Name), cancellationToken),

            LocationTypeEnum.Municipality => await _municipalityRepository.ToListAsync(
                _municipalityRepository.Query(m => locationIds.Contains(m.Id))
                    .Select(m => new { m.Id, m.Name }), cancellationToken)
                .ContinueWith(task => task.Result.ToDictionary(x => x.Id, x => x.Name), cancellationToken),

            LocationTypeEnum.Provincial => await _provinceRepository.ToListAsync(
                _provinceRepository.Query(p => locationIds.Contains(p.Id))
                    .Select(p => new { p.Id, p.Name }), cancellationToken)
                .ContinueWith(task => task.Result.ToDictionary(x => x.Id, x => x.Name), cancellationToken),

            LocationTypeEnum.Regional => await _regionRepository.ToListAsync(
                _regionRepository.Query(r => locationIds.Contains(r.Id))
                    .Select(r => new { r.Id, r.Name }), cancellationToken)
                .ContinueWith(task => task.Result.ToDictionary(x => x.Id, x => x.Name), cancellationToken),

            _ => new Dictionary<Guid, string>()
        };
    }

    /// <summary>
    /// Optimized single location name lookup with caching (for backward compatibility)
    /// </summary>
    private async Task<string> GetLocationNameAsync(Guid locationId, LocationTypeEnum locationType, CancellationToken cancellationToken = default)
    {
        // Check cache first
        var cacheKey = (locationId, locationType);
        if (_locationNameCache.TryGetValue(cacheKey, out var cachedName))
        {
            return cachedName;
        }

        try
        {
            var names = await GetLocationNamesByTypeAsync(locationType, [locationId], cancellationToken);
            var name = names.GetValueOrDefault(locationId, $"Location-{locationId}");
            
            // Cache the result
            _locationNameCache.TryAdd(cacheKey, name);
            return name;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving location name for ID '{LocationId}' of type '{LocationType}'", locationId, locationType);
            var fallbackName = $"Location-{locationId}";
            _locationNameCache.TryAdd(cacheKey, fallbackName);
            return fallbackName;
        }
    }
}