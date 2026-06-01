using MediatR;
using Microsoft.Extensions.Logging;
using NTC.QoS.Application.Features.Reports.ConnectivityGap.Requests;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Reports;
using System.Collections.Concurrent;

namespace NTC.QoS.Application.Features.Reports.ConnectivityGap.Handlers.Queries;

public class GetConnectivityGapReportQueryHandler : IRequestHandler<GetConnectivityGapReportQuery, ApiResponse<ConnectivityGapReportDto>>
{
    private readonly IConnectivityAssessmentRepository _assessmentRepository;
    private readonly IBarangayRepository _barangayRepository;
    private readonly IMunicipalityRepository _municipalityRepository;
    private readonly IProvinceRepository _provinceRepository;
    private readonly IRegionRepository _regionRepository;
    private readonly IBarangayCoverageRepository _barangayCoverageRepository;
    private readonly ILogger<GetConnectivityGapReportQueryHandler> _logger;

    // Cache for location names during the request lifetime
    private readonly ConcurrentDictionary<(Guid, LocationTypeEnum), string> _locationNameCache = new();

    public GetConnectivityGapReportQueryHandler(
        IConnectivityAssessmentRepository assessmentRepository,
        IBarangayRepository barangayRepository,
        IMunicipalityRepository municipalityRepository,
        IProvinceRepository provinceRepository,
        IRegionRepository regionRepository,
        IBarangayCoverageRepository barangayCoverageRepository,
        ILogger<GetConnectivityGapReportQueryHandler> logger)
    {
        _assessmentRepository = assessmentRepository;
        _barangayRepository = barangayRepository;
        _municipalityRepository = municipalityRepository;
        _provinceRepository = provinceRepository;
        _regionRepository = regionRepository;
        _barangayCoverageRepository = barangayCoverageRepository;
        _logger = logger;
    }

    public async Task<ApiResponse<ConnectivityGapReportDto>> Handle(GetConnectivityGapReportQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Generating connectivity gap report with filters: RegionId={RegionId}, ProvinceId={ProvinceId}, MunicipalityId={MunicipalityId}, FilterByStatus={FilterByStatus}, PageNumber={PageNumber}, PageSize={PageSize}",
                request.RegionId, request.ProvinceId, request.MunicipalityId, request.FilterByStatus, request.PageNumber, request.PageSize);

            // Validate pagination parameters
            if (request.PageNumber < 1)
            {
                return new ApiResponse<ConnectivityGapReportDto>
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
                return new ApiResponse<ConnectivityGapReportDto>
                {
                    Succeeded = false,
                    Message = "PageSize must be between 1 and 100.",
                    Errors = new Dictionary<string, string[]>
                    {
                        { "PageSize", ["PageSize must be between 1 and 100."] }
                    }
                };
            }

            if (request.FilterByStatus.HasValue)
            {
                var filterByStatus = request.FilterByStatus.Value;

                if (!Enum.IsDefined(typeof(ConnectivityStatusEnum), filterByStatus) ||
                    (filterByStatus != ConnectivityStatusEnum.Unserved &&
                     filterByStatus != ConnectivityStatusEnum.Underserved))
                {
                    return new ApiResponse<ConnectivityGapReportDto>
                    {
                        Succeeded = false,
                        Message = "FilterByStatus must be either 'Unserved' or 'Underserved' for the gap report.",
                        Errors = new Dictionary<string, string[]>
                        {
                            { "FilterByStatus", ["FilterByStatus must be either 'Unserved' or 'Underserved' for the gap report."] }
                        }
                    };
                }
            }

            // Generate report specifically focusing on unserved/underserved areas
            var assessments = await GetFilteredAssessmentsAsync(
                request.RegionId, request.ProvinceId, request.MunicipalityId, LocationTypeEnum.Barangay, cancellationToken);

            var gaps = assessments
                .Where(a => a.ConnectivityStatus != ConnectivityStatusEnum.Served)
                .ToList();

            // Calculate summary statistics from ALL gap areas (unserved + underserved) regardless of filter
            var summary = new ConnectivityGapSummaryDto
            {
                TotalUnservedAreas = gaps.Count(a => a.ConnectivityStatus == ConnectivityStatusEnum.Unserved),
                TotalUnderservedAreas = gaps.Count(a => a.ConnectivityStatus == ConnectivityStatusEnum.Underserved),
                UnservedPopulation = gaps.Where(a => a.ConnectivityStatus == ConnectivityStatusEnum.Unserved)
                                       .Sum(a => a.Population ?? 0),
                UnderservedPopulation = gaps.Where(a => a.ConnectivityStatus == ConnectivityStatusEnum.Underserved)
                                          .Sum(a => a.Population ?? 0)
            };

            // Apply optional status filter for the paginated list; default includes all gap areas
            var filteredGaps = request.FilterByStatus.HasValue
                ? gaps.Where(a => a.ConnectivityStatus == request.FilterByStatus.Value).ToList()
                : gaps;

            // Sort priority gaps by population descending
            var allPriorityGaps = filteredGaps.OrderByDescending(a => a.Population ?? 0).ToList();
            var totalPriorityAreas = allPriorityGaps.Count;

            // Calculate pagination
            var totalPages = (int)Math.Ceiling(totalPriorityAreas / (double)request.PageSize);
            var skip = (request.PageNumber - 1) * request.PageSize;
            var paginatedGaps = allPriorityGaps.Skip(skip).Take(request.PageSize).ToList();

            // OPTIMIZED: Batch load location names for paginated results only
            var locationNames = await GetLocationNamesBatchAsync(
                paginatedGaps.Select(a => (a.LocationId, a.LocationType)).ToList(),
                cancellationToken);

            // Fetch barangay details with municipality and province names
            var barangayDetails = await GetBarangayDetailsAsync(
                paginatedGaps.Select(a => a.LocationId).ToList(),
                cancellationToken);

            var priorityAreas = paginatedGaps.Select(a => new PriorityAreaDto
            {
                LocationId = a.LocationId,
                LocationName = locationNames.GetValueOrDefault((a.LocationId, a.LocationType), $"Location-{a.LocationId}"),
                MunicipalityName = barangayDetails.GetValueOrDefault(a.LocationId).MunicipalityName,
                ProvinceName = barangayDetails.GetValueOrDefault(a.LocationId).ProvinceName,
                ConnectivityStatus = a.ConnectivityStatus,
                Population = a.Population,
                TotalScore = a.TotalScore,
                LastAssessed = a.AssessmentDate
            }).ToList();

            var reportDto = new ConnectivityGapReportDto
            {
                Summary = summary,
                PriorityAreas = priorityAreas,
                TotalPriorityAreas = totalPriorityAreas,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalPages = totalPages,
                GeneratedAt = DateTime.UtcNow
            };

            return new ApiResponse<ConnectivityGapReportDto>
            {
                Data = reportDto,
                Message = "Connectivity gap report generated successfully.",
                Succeeded = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate connectivity gap report");
            return new ApiResponse<ConnectivityGapReportDto>
            {
                Message = $"Failed to generate connectivity gap report: {ex.Message}",
                Succeeded = false
            };
        }
    }

    private async Task<List<Domain.Entities.ConnectivityAssessment>> GetFilteredAssessmentsAsync(
        Guid? regionId, Guid? provinceId, Guid? municipalityId, LocationTypeEnum locationType, CancellationToken cancellationToken)
    {
        var query = _assessmentRepository.Query(a => a.LocationType == locationType);

        // Only include barangays with BarangayCoverage records
        var coveredBarangayIds = _barangayCoverageRepository.Query()
            .Select(bc => bc.BarangayId);

        // Apply geographic filter precedence: Municipality > Province > Region
        // Lower admin levels take priority - only the most specific filter is applied
        if (municipalityId.HasValue)
        {
            // Get barangay IDs first, then filter assessments
            var barangayIdsInMunicipality = await _barangayRepository.ToListAsync(
                _barangayRepository.Query(b => b.MunicipalityId == municipalityId.Value && coveredBarangayIds.Contains(b.Id))
                    .Select(b => b.Id),
                cancellationToken);

            if (barangayIdsInMunicipality.Count > 0)
            {
                query = query.Where(a => barangayIdsInMunicipality.Contains(a.LocationId));
            }
            else
            {
                // No barangays found, return empty query
                query = query.Where(a => false);
            }
        }
        else if (provinceId.HasValue)
        {
            // Get barangay IDs in province first, then filter assessments
            var barangayIdsInProvince = await _barangayRepository.ToListAsync(
                _barangayRepository.Query(b => b.Municipality.ProvinceId == provinceId.Value && coveredBarangayIds.Contains(b.Id))
                    .Select(b => b.Id),
                cancellationToken);

            if (barangayIdsInProvince.Count > 0)
            {
                query = query.Where(a => barangayIdsInProvince.Contains(a.LocationId));
            }
            else
            {
                // No barangays found, return empty query
                query = query.Where(a => false);
            }
        }
        else if (regionId.HasValue)
        {
            // Get barangay IDs in region first, then filter assessments
            var barangayIdsInRegion = await _barangayRepository.ToListAsync(
                _barangayRepository.Query(b => b.Municipality.Province.RegionId == regionId.Value && coveredBarangayIds.Contains(b.Id))
                    .Select(b => b.Id),
                cancellationToken);

            if (barangayIdsInRegion.Count > 0)
            {
                query = query.Where(a => barangayIdsInRegion.Contains(a.LocationId));
            }
            else
            {
                // No barangays found, return empty query
                query = query.Where(a => false);
            }
        }
        else
        {
            // No geographic filter, just filter by covered barangays
            var allCoveredBarangayIds = await _barangayRepository.ToListAsync(
                _barangayRepository.Query(b => coveredBarangayIds.Contains(b.Id))
                    .Select(b => b.Id),
                cancellationToken);

            if (allCoveredBarangayIds.Count > 0)
            {
                query = query.Where(a => allCoveredBarangayIds.Contains(a.LocationId));
            }
            else
            {
                query = query.Where(a => false);
            }
        }

        return await _assessmentRepository.ToListAsync(query, cancellationToken);
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
                _barangayRepository.QueryAsNoTracking(b => locationIds.Contains(b.Id))
                    .Select(b => new { b.Id, b.Name }), cancellationToken)
                .ContinueWith(task => task.Result.ToDictionary(x => x.Id, x => x.Name), cancellationToken),

            LocationTypeEnum.Municipality => await _municipalityRepository.ToListAsync(
                _municipalityRepository.QueryAsNoTracking(m => locationIds.Contains(m.Id))
                    .Select(m => new { m.Id, m.Name }), cancellationToken)
                .ContinueWith(task => task.Result.ToDictionary(x => x.Id, x => x.Name), cancellationToken),

            LocationTypeEnum.Provincial => await _provinceRepository.ToListAsync(
                _provinceRepository.QueryAsNoTracking(p => locationIds.Contains(p.Id))
                    .Select(p => new { p.Id, p.Name }), cancellationToken)
                .ContinueWith(task => task.Result.ToDictionary(x => x.Id, x => x.Name), cancellationToken),

            LocationTypeEnum.Regional => await _regionRepository.ToListAsync(
                _regionRepository.QueryAsNoTracking(r => locationIds.Contains(r.Id))
                    .Select(r => new { r.Id, r.Name }), cancellationToken)
                .ContinueWith(task => task.Result.ToDictionary(x => x.Id, x => x.Name), cancellationToken),

            _ => new Dictionary<Guid, string>()
        };
    }

    private async Task<Dictionary<Guid, (string MunicipalityName, string ProvinceName)>> GetBarangayDetailsAsync(
        List<Guid> barangayIds,
        CancellationToken cancellationToken = default)
    {
        if (barangayIds.Count == 0)
            return new Dictionary<Guid, (string, string)>();

        try
        {
            var barangays = await _barangayRepository.ToListAsync(
                _barangayRepository.QueryAsNoTracking(b => barangayIds.Contains(b.Id))
                    .Select(b => new
                    {
                        b.Id,
                        MunicipalityName = b.Municipality.Name,
                        ProvinceName = b.Municipality.Province.Name
                    }),
                cancellationToken);

            return barangays.ToDictionary(
                x => x.Id,
                x => (x.MunicipalityName, x.ProvinceName));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching barangay details with municipality and province names");
            return new Dictionary<Guid, (string, string)>();
        }
    }
}
