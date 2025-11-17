using MediatR;
using NTC.QoS.Application.Features.Cells.Requests;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.Cell;
using NTC.QoS.Infrastructure.Helper;

namespace NTC.QoS.Application.Features.Cells.Handlers.Queries;

public class GetAllCellsHandler(ICellRepository cellRepository,
    IRepository<Domain.Entities.Barangay> barangayRepository,
    IRepository<Domain.Entities.Municipality> municipalityRepository,
    IRepository<Domain.Entities.Province> provinceRepository,
    IRepository<Domain.Entities.Region> regionRepository) : IRequestHandler<GetAllCellsRequest, PaginatedResponse<CellDto>>
{
    private readonly ICellRepository _cellRepository = cellRepository;
    private readonly IRepository<Domain.Entities.Barangay> _barangayRepository = barangayRepository;
    private readonly IRepository<Domain.Entities.Municipality> _municipalityRepository = municipalityRepository;
    private readonly IRepository<Domain.Entities.Province> _provinceRepository = provinceRepository;
    private readonly IRepository<Domain.Entities.Region> _regionRepository = regionRepository;

    public async Task<PaginatedResponse<CellDto>> Handle(GetAllCellsRequest request, CancellationToken cancellationToken)
    {
        var cellQuery = _cellRepository.Query();

        var locationPoint = GeometryHelper.CreatePoint(request.Coordinate);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var searchExpression = SearchHelper.BuildNullableKeywordSearchExpression<Domain.Aggregates.Cell>(
               request.Search,
               c => c.Name,
               c => c.Description,
               c => c.Group,
               c => c.Address
            );
            cellQuery = cellQuery.Where(searchExpression);
        }

        cellQuery = cellQuery
            .Where(c => string.IsNullOrEmpty(request.Status) || c.Status == EnumHelper<CellStatusEnum>.ParseUserInput(request.Status))
            .Where(c => request.ServiceProviderIds == null || request.ServiceProviderIds.Contains(c.ServiceProviderId))
            .Where(c => request.ProviderIds == null || (c.ServiceProvider != null && request.ProviderIds.Contains(c.ServiceProvider.ProviderId)))
            .Where(c => request.ServiceIds == null || (c.ServiceProvider != null && request.ServiceIds.Contains(c.ServiceProvider.ServiceId)))
            .Where(c => locationPoint.IsValid && c.Location.IsWithinDistance(locationPoint, request.Distance));

        if (request.BarangayIds is { Count: > 0 })
        {
            cellQuery = await GeoQueryHelper.ApplyBoundaryFilterAsync(request.BarangayIds, _barangayRepository, cellQuery, cancellationToken);
        }
        else if (request.MunicipalityIds is { Count: > 0 })
        {
            cellQuery = await GeoQueryHelper.ApplyBoundaryFilterAsync(request.MunicipalityIds, _municipalityRepository, cellQuery, cancellationToken);
        }
        else if (request.ProvinceIds is { Count: > 0 })
        {
            cellQuery = await GeoQueryHelper.ApplyBoundaryFilterAsync(request.ProvinceIds, _provinceRepository, cellQuery, cancellationToken);
        }
        else if (request.RegionIds is { Count: > 0 })
        {
            cellQuery = await GeoQueryHelper.ApplyBoundaryFilterAsync(request.RegionIds, _regionRepository, cellQuery, cancellationToken);
        }

        cellQuery = cellQuery.OrderBy(c => c.Location.Distance(locationPoint));

        var cellDtoQuery = cellQuery
            .Select(cell => new CellDto
            {
                Id = cell.Id,
                Name = cell.Name,
                Group = cell.Group,
                Description = cell.Description ?? string.Empty,
                Address = cell.Address ?? string.Empty,
                ServiceProvider = new()
                {
                    Id = cell.ServiceProviderId,
                    Service = new()
                    {
                        Id = cell.ServiceProvider != null && cell.ServiceProvider.Service != null ? cell.ServiceProvider.Service.Id : Guid.Empty,
                        Name = cell.ServiceProvider != null && cell.ServiceProvider.Service != null ? cell.ServiceProvider.Service.Name : string.Empty,
                        Type = cell.ServiceProvider != null && cell.ServiceProvider.Service != null ? cell.ServiceProvider.Service.Type.ToString() : string.Empty
                    },
                    Provider = new()
                    {
                        Id = cell.ServiceProvider != null && cell.ServiceProvider.Provider != null ? cell.ServiceProvider.Provider.Id : Guid.Empty,
                        Name = cell.ServiceProvider != null && cell.ServiceProvider.Provider != null ? cell.ServiceProvider.Provider.Name : string.Empty,
                        Description = cell.ServiceProvider != null && cell.ServiceProvider.Provider != null ? cell.ServiceProvider.Provider.Description : null,
                        LicenseNumber = cell.ServiceProvider != null && cell.ServiceProvider.Provider != null ? cell.ServiceProvider.Provider.LicenseNumber : null,
                        Color = cell.ServiceProvider != null && cell.ServiceProvider.Provider != null ? cell.ServiceProvider.Provider.Color : string.Empty
                    }
                },
                CID = cell.CID,
                LAC = cell.LAC,
                TAC = cell.TAC,
                SiteId = cell.SiteId,
                TowerType = cell.TowerType,
                Elevation = cell.Elevation,
                Latitude = cell.Location.Coordinate.Y,
                Longitude = cell.Location.Coordinate.X,
                IsVerified = cell.IsVerified,
                Status = EnumHelper<CellStatusEnum>.GetName(cell.Status),
                Distance = Math.Round(cell.Location.Distance(locationPoint), 2)
            });

        return await _cellRepository.PaginateAsync(cellDtoQuery, request.PageNumber, request.PageSize, cancellationToken);
    }
}
