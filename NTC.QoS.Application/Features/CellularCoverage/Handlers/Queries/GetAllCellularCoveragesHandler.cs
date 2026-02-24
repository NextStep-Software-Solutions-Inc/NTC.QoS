using MediatR;
using NetTopologySuite.Geometries;
using NTC.QoS.Application.Features.CellularCoverage.Requests;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.CellularCoverage;
using NTC.QoS.Infrastructure.Helper;

namespace NTC.QoS.Application.Features.CellularCoverage.Handlers.Queries;

public class GetAllCellularCoveragesHandler(ICellularCoverageRepository cellularCoverageRepository,
    IRepository<Domain.Entities.Barangay> barangayRepository,
    IRepository<Domain.Entities.Municipality> municipalityRepository,
    IRepository<Domain.Entities.Province> provinceRepository,
    IRepository<Domain.Entities.Region> regionRepository) : IRequestHandler<GetAllCellularCoveragesRequest, PaginatedResponse<CellularCoverageDto>>
{
    private readonly ICellularCoverageRepository _cellularCoverageRepository = cellularCoverageRepository;
    private readonly IRepository<Domain.Entities.Barangay> _barangayRepository = barangayRepository;
    private readonly IRepository<Domain.Entities.Municipality> _municipalityRepository = municipalityRepository;
    private readonly IRepository<Domain.Entities.Province> _provinceRepository = provinceRepository;
    private readonly IRepository<Domain.Entities.Region> _regionRepository = regionRepository;

    public async Task<PaginatedResponse<CellularCoverageDto>> Handle(GetAllCellularCoveragesRequest request, CancellationToken cancellationToken)
    {
        var cellularCoverageQuery = _cellularCoverageRepository.QueryAsNoTracking();

        Point locationPoint = GeometryHelper.CreatePoint(request.Coordinate);

        cellularCoverageQuery = cellularCoverageQuery
            .Where(cc => request.NetworkIds == null || (request.NetworkIds.Count > 0 && request.NetworkIds.Contains(cc.NetworkId)))
            .Where(cc => request.CellIds == null || request.CellIds.Count == 0 || request.CellIds.Any(cellId => cc.Network.Sectors.Any(s => s.CellSector != null && s.CellSector.CellId == cellId)))
            .Where(cc => locationPoint.IsValid && cc.Location.IsWithinDistance(locationPoint, request.Distance));

        // V2 Filters
        if (request.BandIds is { Count: > 0 })
        {
            cellularCoverageQuery = cellularCoverageQuery.Where(cc => cc.BandId != null && request.BandIds.Contains(cc.BandId.Value));
        }

        if (request.TechnologyIds is { Count: > 0 })
        {
            cellularCoverageQuery = cellularCoverageQuery.Where(cc => cc.TechnologyId != null && request.TechnologyIds.Contains(cc.TechnologyId.Value));
        }

        if (request.ServiceProviderIds is { Count: > 0 })
        {
            cellularCoverageQuery = cellularCoverageQuery.Where(cc => cc.CellSector != null && request.ServiceProviderIds.Contains(cc.CellSector.Cell.ServiceProviderId));
        }

        if (request.CellSectorIds is { Count: > 0 })
        {
            cellularCoverageQuery = cellularCoverageQuery.Where(cc => cc.CellSectorId != null && request.CellSectorIds.Contains(cc.CellSectorId.Value));
        }

        if (request.BarangayIds is { Count: > 0 })
        {
            cellularCoverageQuery = await GeoQueryHelper.ApplyBoundaryFilterAsync(request.BarangayIds, _barangayRepository, cellularCoverageQuery, cancellationToken);
        }
        else if (request.MunicipalityIds is { Count: > 0 })
        {
            cellularCoverageQuery = await GeoQueryHelper.ApplyBoundaryFilterAsync(request.MunicipalityIds, _municipalityRepository, cellularCoverageQuery, cancellationToken);
        }
        else if (request.ProvinceIds is { Count: > 0 })
        {
            cellularCoverageQuery = await GeoQueryHelper.ApplyBoundaryFilterAsync(request.ProvinceIds, _provinceRepository, cellularCoverageQuery, cancellationToken);
        }
        else if (request.RegionIds is { Count: > 0 })
        {
            cellularCoverageQuery = await GeoQueryHelper.ApplyBoundaryFilterAsync(request.RegionIds, _regionRepository, cellularCoverageQuery, cancellationToken);
        }
        cellularCoverageQuery = cellularCoverageQuery.OrderBy(c => c.Location.Distance(locationPoint));

        var cellularCoveragesDtosQuery = cellularCoverageQuery
            .Select(cc => new CellularCoverageDto
            {
                Id = cc.Id,
                NetworkId = cc.NetworkId,
                NetworkName = cc.Network.Name,
                Lat = cc.Location.Coordinate.Y,
                Lng = cc.Location.Coordinate.X,
                SignalStrength = cc.SignalStrength,
                Gen = cc.Network.SystemType != null && cc.Network.SystemType.Technology != null ? cc.Network.SystemType.Technology.Generation.Name : string.Empty,
                Std = cc.Network.SystemType != null && cc.Network.SystemType.Technology != null ? cc.Network.SystemType.Technology.Standard : string.Empty,
                Band = cc.Band != null ? cc.Band.Name : string.Empty,
                Provider = cc.Network.ServiceProvider != null ? new CCProviderDto
                {
                    Name = cc.Network.ServiceProvider.Provider.Name,
                    Color = cc.Network.ServiceProvider.Provider.Color
                } : null,
                // V2 Extended Fields
                BandId = cc.BandId,
                BandName = cc.Band != null ? cc.Band.Name : null,
                TechnologyId = cc.TechnologyId,
                TechnologyName = cc.Technology != null ? cc.Technology.Standard : null,
                CellSectorId = cc.CellSectorId,
                CellSectorName = cc.CellSector != null ? cc.CellSector.Cell.Name : null,
                MunicipalityId = cc.MunicipalityId,
                MunicipalityName = cc.Municipality != null ? cc.Municipality.Name : null,
                Address = cc.Address,
                IsFileUploaded = cc.IsFileUploaded,
                CreatedAt = cc.CreatedAt
            });

        return await _cellularCoverageRepository.PaginateAsync(cellularCoveragesDtosQuery, request.PageNumber, request.PageSize, cancellationToken);
    }
}
