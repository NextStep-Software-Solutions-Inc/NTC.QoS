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
                Lat = cc.Location.Coordinate.Y,
                Lng = cc.Location.Coordinate.X,
                Strength = cc.Strength,
                Band = cc.Network.UpLink != null ? cc.Network.UpLink.Band.Name : string.Empty,
                Gen = cc.Network.SystemType != null && cc.Network.SystemType.Technology != null ? cc.Network.SystemType.Technology.Generation.Name : string.Empty,
                Std = cc.Network.SystemType != null && cc.Network.SystemType.Technology != null ? cc.Network.SystemType.Technology.Standard : string.Empty,
                Provider = cc.Network.UpLink != null ? new CCProviderDto
                {
                    Name = cc.Network.UpLink.ServiceProvider.Provider.Name,
                    Color = cc.Network.UpLink.ServiceProvider.Provider.Color
                } : null
            });

        return await _cellularCoverageRepository.PaginateAsync(cellularCoveragesDtosQuery, request.PageNumber, request.PageSize, cancellationToken);
    }
}
