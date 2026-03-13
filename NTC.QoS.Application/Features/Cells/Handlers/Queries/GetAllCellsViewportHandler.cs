using MediatR;
using NTC.QoS.Application.Features.Cells.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.Cell;
using NTC.QoS.Infrastructure.Helper;

namespace NTC.QoS.Application.Features.Cells.Handlers.Queries;

/// <summary>
/// Handler for viewport-based Cell queries (v2 API).
/// OPTIMIZED FOR MILLIONS OF DATA POINTS WITH LIMITED RESOURCES.
/// </summary>
public class GetAllCellsViewportHandler : IRequestHandler<GetAllCellsViewportRequest, PaginatedResponse<CellViewportDto>>
{
    private readonly ICellRepository _cellRepository;
    private readonly IRepository<Domain.Entities.Barangay> _barangayRepository;
    private readonly IRepository<Domain.Entities.Municipality> _municipalityRepository;
    private readonly IRepository<Domain.Entities.Province> _provinceRepository;
    private readonly IRepository<Domain.Entities.Region> _regionRepository;
    private readonly IRepository<Domain.Entities.Cellular> _cellularRepository;

    public GetAllCellsViewportHandler(
        ICellRepository cellRepository,
        IRepository<Domain.Entities.Barangay> barangayRepository,
        IRepository<Domain.Entities.Municipality> municipalityRepository,
        IRepository<Domain.Entities.Province> provinceRepository,
        IRepository<Domain.Entities.Region> regionRepository,
        IRepository<Domain.Entities.Cellular> cellularRepository)
    {
        _cellRepository = cellRepository;
        _barangayRepository = barangayRepository;
        _municipalityRepository = municipalityRepository;
        _provinceRepository = provinceRepository;
        _regionRepository = regionRepository;
        _cellularRepository = cellularRepository;
    }

    public async Task<PaginatedResponse<CellViewportDto>> Handle(GetAllCellsViewportRequest request, CancellationToken cancellationToken)
    {
        if (!request.Viewport.IsValidForLimitedResources(out var validationError))
        {
            return new PaginatedResponse<CellViewportDto>([], request.PageNumber, request.PageSize, 0)
            {
                Succeeded = false,
                Message = validationError ?? "Invalid viewport bounds"
            };
        }

        var maxAllowedPageSize = request.Viewport.GetRecommendedMaxResults();
        var effectivePageSize = Math.Min(request.PageSize, maxAllowedPageSize);

        var cellQuery = _cellRepository.QueryAsNoTracking();
        cellQuery = cellQuery.ApplyViewportFilter(request.Viewport);

        if (request.ServiceProviderIds != null && request.ServiceProviderIds.Count > 0)
        {
            cellQuery = cellQuery.Where(c => request.ServiceProviderIds.Contains(c.ServiceProviderId));
        }

        if (request.IsVerified.HasValue)
        {
            cellQuery = cellQuery.Where(c => c.IsVerified == request.IsVerified.Value);
        }

        if (request.GenerationIds is { Count: > 0 })
        {
            // Query upward from Cellulars → CellSector.CellId — mirrors the pattern used
            // in WirelessCoverageComputationService / ProviderTechnologyService, which is
            // proven to work. The inverse downward path (Cell.CellSectors.Any(...)) returns
            // empty because most Cell rows have no CellSector children in the DB.
            var cellIdsWithGeneration = _cellularRepository.QueryAsNoTracking()
                .Where(cellular => cellular.Technology != null && request.GenerationIds.Contains(cellular.Technology!.GenerationId))
                .Select(cellular => cellular.CellSector!.CellId)
                .Distinct();

            cellQuery = cellQuery.Where(c => cellIdsWithGeneration.Contains(c.Id));
        }

        if (request.BarangayIds is { Count: > 0 })
            cellQuery = await GeoQueryHelper.ApplyBoundaryFilterAsync(request.BarangayIds, _barangayRepository, cellQuery, cancellationToken);
        else if (request.MunicipalityIds is { Count: > 0 })
            cellQuery = await GeoQueryHelper.ApplyBoundaryFilterAsync(request.MunicipalityIds, _municipalityRepository, cellQuery, cancellationToken);
        else if (request.ProvinceIds is { Count: > 0 })
            cellQuery = await GeoQueryHelper.ApplyBoundaryFilterAsync(request.ProvinceIds, _provinceRepository, cellQuery, cancellationToken);
        else if (request.RegionIds is { Count: > 0 })
            cellQuery = await GeoQueryHelper.ApplyBoundaryFilterAsync(request.RegionIds, _regionRepository, cellQuery, cancellationToken);

        cellQuery = cellQuery.OrderBy(c => c.Id);
    

        var cellViewportDtoQuery = cellQuery.Select(c => new CellViewportDto
        {
            Id = c.Id.ToString(),
            ISP = c.ServiceProvider.Provider.Name,
            ISPColor = c.ServiceProvider.Provider.Color,
            Lat = c.Location.Coordinate.Y,
            Lng = c.Location.Coordinate.X,
            Name = c.Name,
            Status = c.Status.ToString()
        });

        return await _cellRepository.PaginateAsync(cellViewportDtoQuery, request.PageNumber, effectivePageSize, cancellationToken);
    }
}
