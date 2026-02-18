using MediatR;
using NTC.QoS.Application.Features.Speedtest.Requests;
using NTC.QoS.Domain.Constants;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.Speedtest;
using NTC.QoS.Infrastructure.Helper;

namespace NTC.QoS.Application.Features.Speedtest.Handlers.Queries;

/// <summary>
/// Handler for viewport-based Speedtest queries (v2 API).
/// OPTIMIZED FOR MILLIONS OF DATA POINTS WITH LIMITED RESOURCES.
/// 
/// Key optimizations for resource-constrained environments:
/// - AsNoTracking for read-only queries (no change tracking overhead)
/// - Viewport bounding box instead of distance calculations (spatial index friendly)
/// - Minimal column projection (only 7 fields: ISP, ISPColor, Lat, Lng, DownloadSpeed, UploadSpeed, SignalQuality - NO Guid ID)
/// - Smart pagination limits based on viewport size
/// - Query timeout protection (30 seconds max)
/// - Spatial index utilization via PostGIS && operator
/// - No distance ordering (not needed for viewport queries, saves computation)
/// - Cancellation token support for request cancellation
/// </summary>
public class GetAllSpeedtestsViewportHandler : IRequestHandler<GetAllSpeedtestsViewportRequest, PaginatedResponse<SpeedtestViewportDto>>
{
    private readonly ISpeedtestRepository _speedtestRepository;
    private readonly IRepository<Domain.Entities.Barangay> _barangayRepository;
    private readonly IRepository<Domain.Entities.Municipality> _municipalityRepository;
    private readonly IRepository<Domain.Entities.Province> _provinceRepository;
    private readonly IRepository<Domain.Entities.Region> _regionRepository;

    public GetAllSpeedtestsViewportHandler(
        ISpeedtestRepository speedtestRepository,
        IRepository<Domain.Entities.Barangay> barangayRepository,
        IRepository<Domain.Entities.Municipality> municipalityRepository,
        IRepository<Domain.Entities.Province> provinceRepository,
        IRepository<Domain.Entities.Region> regionRepository)
    {
        _speedtestRepository = speedtestRepository;
        _barangayRepository = barangayRepository;
        _municipalityRepository = municipalityRepository;
        _provinceRepository = provinceRepository;
        _regionRepository = regionRepository;
    }

    public async Task<PaginatedResponse<SpeedtestViewportDto>> Handle(GetAllSpeedtestsViewportRequest request, CancellationToken cancellationToken)
    {
        // Validate viewport for resource limits (millions of data points protection)
        if (!request.Viewport.IsValidForLimitedResources(out var validationError))
        {
            return new PaginatedResponse<SpeedtestViewportDto>(
                [],
                request.PageNumber,
                request.PageSize,
                0)
            {
                Succeeded = false,
                Message = validationError ?? "Invalid viewport bounds for limited resource environment"
            };
        }

        // Enforce viewport-size-aware pagination limits
        var maxAllowedPageSize = request.Viewport.GetRecommendedMaxResults();
        var effectivePageSize = Math.Min(request.PageSize, maxAllowedPageSize);

        // Start with base query using AsNoTracking for read-only optimization
        var speedtestQuery = _speedtestRepository.QueryAsNoTracking();

        // Apply viewport spatial filter (leverages PostGIS spatial index)
        speedtestQuery = speedtestQuery.ApplyViewportFilter(request.Viewport);

        // Apply service provider filter if specified
        if (request.ServiceProviderIds != null && request.ServiceProviderIds.Count > 0)
        {
            speedtestQuery = speedtestQuery.Where(s => request.ServiceProviderIds.Contains(s.ServiceProviderId));
        }

        // Apply speed range and date filters if specified
        if (request.MinDownloadSpeed.HasValue)
        {
            speedtestQuery = speedtestQuery.Where(s => s.DownloadSpeed >= request.MinDownloadSpeed.Value);
        }
        if (request.MaxDownloadSpeed.HasValue)
        {
            speedtestQuery = speedtestQuery.Where(s => s.DownloadSpeed <= request.MaxDownloadSpeed.Value);
        }
        if (request.MinUploadSpeed.HasValue)
        {
            speedtestQuery = speedtestQuery.Where(s => s.UploadSpeed >= request.MinUploadSpeed.Value);
        }
        if (request.MaxUploadSpeed.HasValue)
        {
            speedtestQuery = speedtestQuery.Where(s => s.UploadSpeed <= request.MaxUploadSpeed.Value);
        }
        if (request.StartDate.HasValue)
        {
            speedtestQuery = speedtestQuery.Where(s => s.DateTime >= request.StartDate.Value);
        }
        if (request.EndDate.HasValue)
        {
            speedtestQuery = speedtestQuery.Where(s => s.DateTime <= request.EndDate.Value);
        }

        // Apply administrative boundary filters (hierarchical)
        if (request.BarangayIds is { Count: > 0 })
        {
            speedtestQuery = await GeoQueryHelper.ApplyBoundaryFilterAsync(
                request.BarangayIds,
                _barangayRepository,
                speedtestQuery,
                cancellationToken);
        }
        else if (request.MunicipalityIds is { Count: > 0 })
        {
            speedtestQuery = await GeoQueryHelper.ApplyBoundaryFilterAsync(
                request.MunicipalityIds,
                _municipalityRepository,
                speedtestQuery,
                cancellationToken);
        }
        else if (request.ProvinceIds is { Count: > 0 })
        {
            speedtestQuery = await GeoQueryHelper.ApplyBoundaryFilterAsync(
                request.ProvinceIds,
                _provinceRepository,
                speedtestQuery,
                cancellationToken);
        }
        else if (request.RegionIds is { Count: > 0 })
        {
            speedtestQuery = await GeoQueryHelper.ApplyBoundaryFilterAsync(
                request.RegionIds,
                _regionRepository,
                speedtestQuery,
                cancellationToken);
        }

        // Order by ID for consistent pagination (fast with primary key index)
        speedtestQuery = speedtestQuery.OrderBy(s => s.Id);

        // CRITICAL: Project to lightweight DTO early to minimize memory footprint
        // Only select 7 fields needed for map rendering - NO Guid ID
        var speedtestViewportDtoQuery = speedtestQuery.Select(s => new SpeedtestViewportDto
        {
            ISP = s.ServiceProvider.Provider.Name,
            ISPColor = s.ServiceProvider.Provider.Color,
            Lat = s.Location.Coordinate.Y,
            Lng = s.Location.Coordinate.X,
            DownloadSpeed = s.DownloadSpeed,
            UploadSpeed = s.UploadSpeed,
            SignalQuality = s.SignalQuality
        });

        // Execute paginated query with resource-aware page size
        return await _speedtestRepository.PaginateAsync(
            speedtestViewportDtoQuery,
            request.PageNumber,
            effectivePageSize,
            cancellationToken);
    }
}
