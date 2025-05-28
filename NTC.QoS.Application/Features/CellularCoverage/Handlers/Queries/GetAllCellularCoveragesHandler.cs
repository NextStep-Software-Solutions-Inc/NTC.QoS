using MediatR;
using NetTopologySuite.Geometries;
using NTC.QoS.Application.Features.Cellular.Requests;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Cellular;

namespace NTC.QoS.Application.Features.Cellular.Handlers.Queries;

public class GetAllCellularCoveragesHandler(ICellularCoverageRepository cellularCoverageRepository) : IRequestHandler<GetAllCellularCoveragesRequest, PaginatedResponse<CellularCoverageDto>>
{
    private readonly ICellularCoverageRepository _cellularCoverageRepository = cellularCoverageRepository;

    public async Task<PaginatedResponse<CellularCoverageDto>> Handle(GetAllCellularCoveragesRequest request, CancellationToken cancellationToken)
    {
        var allCellularCoveragesQuery = _cellularCoverageRepository.Query(cc => !cc.IsDeleted);

        Point locationPoint = GeometryHelper.CreatePoint(request.Coordinate);

        allCellularCoveragesQuery = allCellularCoveragesQuery
            .Where(cc => request.NetworkIds == null || (request.NetworkIds.Count > 0 && request.NetworkIds.Contains(cc.NetworkId)))
            .Where(cc => locationPoint.IsValid && cc.Location.IsWithinDistance(locationPoint, request.Distance))
            .OrderBy(cc => cc.Location.Distance(locationPoint));

        var cellularCoveragesDtosQuery = allCellularCoveragesQuery
            .Select(cc => new CellularCoverageDto
            {
                Id = cc.Id,
                Latitude = cc.Location.Coordinate.Y,
                Longitude = cc.Location.Coordinate.X,
                Strength = cc.Strength,
                Address = cc.Address,
                Band = cc.Network.UpLink != null ? cc.Network.UpLink.Band.Name : string.Empty,
                Generation = cc.Network.UpLink != null && cc.Network.UpLink.Band != null &&
                             cc.Network.UpLink.Band.GenerationBands != null
                    ? cc.Network.UpLink.Band.GenerationBands
                        .Where(gb => gb.BandId == cc.Network.UpLink.BandId && gb.Generation != null)
                        .Select(gb => gb.Generation.Name)
                        .FirstOrDefault() ?? string.Empty
                    : string.Empty,
                Standard = cc.Network.UpLink != null && cc.Network.UpLink.Band != null &&
                           cc.Network.UpLink.Band.GenerationBands != null
                    ? cc.Network.UpLink.Band.GenerationBands
                        .Where(gb => gb.Generation != null && gb.Generation.Technologies.Any())
                        .Select(gb => gb.Generation.Technologies
                            .Select(t => t.Standard)
                            .FirstOrDefault())
                        .FirstOrDefault() ?? string.Empty
                    : string.Empty,
                Provider = cc.Network.UpLink != null ? new CCProviderDto
                {
                    Id = cc.Network.UpLink.ServiceProvider.Provider.Id,
                    Name = cc.Network.UpLink.ServiceProvider.Provider.Name,
                    Color = cc.Network.UpLink.ServiceProvider.Provider.Color
                } : null
            });

        return await _cellularCoverageRepository.PaginateAsync(cellularCoveragesDtosQuery, request.PageNumber, request.PageSize, cancellationToken);
    }
}
