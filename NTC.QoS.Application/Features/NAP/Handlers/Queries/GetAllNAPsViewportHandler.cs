using MediatR;
using NTC.QoS.Application.Features.NAP.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.NAP;
using NTC.QoS.Infrastructure.Helper;

namespace NTC.QoS.Application.Features.NAP.Handlers.Queries;

/// <summary>
/// Handler for viewport-based NAP queries (v2 API).
/// OPTIMIZED FOR MILLIONS OF DATA POINTS WITH LIMITED RESOURCES.
/// 
/// Key optimizations for resource-constrained environments:
/// - AsNoTracking for read-only queries (no change tracking overhead)
/// - Viewport bounding box instead of distance calculations (spatial index friendly)
/// - Minimal column projection (only 4 fields: ISP, ISPColor, Lat, Lng - NO Guid ID)
/// - Smart pagination limits based on viewport size
/// - Query timeout protection (30 seconds max)
/// - Spatial index utilization via PostGIS && operator
/// - No distance ordering (not needed for viewport queries, saves computation)
/// - Cancellation token support for request cancellation
/// </summary>
public class GetAllNAPsViewportHandler : IRequestHandler<GetAllNAPsViewportRequest, PaginatedResponse<NAPViewportDto>>
{
    private readonly INAPRepository _napRepository;
    private readonly IRepository<Domain.Entities.Barangay> _barangayRepository;
    private readonly IRepository<Domain.Entities.Municipality> _municipalityRepository;
    private readonly IRepository<Domain.Entities.Province> _provinceRepository;
    private readonly IRepository<Domain.Entities.Region> _regionRepository;

    public GetAllNAPsViewportHandler(
        INAPRepository napRepository,
        IRepository<Domain.Entities.Barangay> barangayRepository,
        IRepository<Domain.Entities.Municipality> municipalityRepository,
        IRepository<Domain.Entities.Province> provinceRepository,
        IRepository<Domain.Entities.Region> regionRepository)
    {
        _napRepository = napRepository;
        _barangayRepository = barangayRepository;
        _municipalityRepository = municipalityRepository;
        _provinceRepository = provinceRepository;
        _regionRepository = regionRepository;
    }

    public async Task<PaginatedResponse<NAPViewportDto>> Handle(GetAllNAPsViewportRequest request, CancellationToken cancellationToken)
    {
        // Validate viewport for resource limits (millions of data points protection)
        if (!request.Viewport.IsValidForLimitedResources(out var validationError))
        {
            return new PaginatedResponse<NAPViewportDto>(
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

        if (effectivePageSize < request.PageSize)
        {
            // Log or track that page size was reduced due to viewport size
            // This prevents excessive memory consumption on large viewports
        }

        // Start with base query using AsNoTracking for read-only optimization`
        // This eliminates change tracking overhead for millions of entities
        var napQuery = _napRepository.QueryAsNoTracking();

        // Apply service provider filter if specified
        if (request.ServiceProviderIds != null && request.ServiceProviderIds.Count > 0)
        {
            napQuery = napQuery.Where(n => request.ServiceProviderIds.Contains(n.ServiceProviderId));
        }

        // Apply administrative boundary filters (hierarchical - only one level is applied)
        napQuery = _napRepository.GetEntitiesWithinBoundaries(
            napQuery,
            viewport: request.Viewport,
            regionIds: request.RegionIds,
            provinceIds: request.ProvinceIds,
            municipalityIds: request.MunicipalityIds,
            barangayIds: request.BarangayIds);


        // Order by ID for consistent pagination (no distance ordering needed in viewport)
        // ID ordering is fast with primary key index
        napQuery = napQuery.OrderBy(n => n.Id);

        // CRITICAL: Project to lightweight DTO early to minimize memory footprint
        // Only select 4 fields needed for map rendering - NO Guid ID (saves ~16 bytes per record)
        // This is crucial when dealing with millions of rows on limited resources
        var napViewportDtoQuery = napQuery.Select(n => new NAPViewportDto
        {
            ISP = n.ServiceProvider.Provider.Name,
            ISPColor = n.ServiceProvider.Provider.Color,
            Lat = n.Location.Coordinate.Y,  // Extract Y coordinate directly
            Lng = n.Location.Coordinate.X   // Extract X coordinate directly
        });

        // Execute paginated query with resource-aware page size
        // Query timeout is handled at the DbContext level (configured in infrastructure)
        return await _napRepository.PaginateAsync(
            napViewportDtoQuery,
            request.PageNumber,
            effectivePageSize,  // Use resource-aware page size, not the requested one
            cancellationToken);
    }
}
