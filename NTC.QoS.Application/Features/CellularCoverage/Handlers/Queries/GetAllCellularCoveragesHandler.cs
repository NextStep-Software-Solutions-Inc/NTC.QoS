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
        var cellularCoverageQuery = _cellularCoverageRepository.Query();

        Point locationPoint = GeometryHelper.CreatePoint(request.Coordinate);

        cellularCoverageQuery = cellularCoverageQuery
            .Where(cc => request.NetworkIds == null || (request.NetworkIds.Count > 0 && request.NetworkIds.Contains(cc.NetworkId)))
            .Where(cc => locationPoint.IsValid && cc.Location.IsWithinDistance(locationPoint, request.Distance));

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
                Lat = cc.Location.Coordinate.Y,
                Lng = cc.Location.Coordinate.X,
                Strength = cc.Strength,
                Gen = cc.Network.SystemType != null && cc.Network.SystemType.Technology != null ? cc.Network.SystemType.Technology.Generation.Name : string.Empty,
                Std = cc.Network.SystemType != null && cc.Network.SystemType.Technology != null ? cc.Network.SystemType.Technology.Standard : string.Empty,
            });

        return await _cellularCoverageRepository.PaginateAsync(cellularCoveragesDtosQuery, request.PageNumber, request.PageSize, cancellationToken);
    }
}
