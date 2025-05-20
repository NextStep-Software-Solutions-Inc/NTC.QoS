using MediatR;
using NetTopologySuite.Geometries;
using NTC.QoS.Application.Features.NAP.Requests;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.NAP;

namespace NTC.QoS.Application.Features.NAP.Handlers.Queries;

public class GetAllNAPsHandler(INAPRepository NAPRepository) : IRequestHandler<GetAllNAPsRequest, PaginatedResponse<NAPDto>>
{
    private readonly INAPRepository _NAPRepository = NAPRepository;

    public async Task<PaginatedResponse<NAPDto>> Handle(GetAllNAPsRequest request, CancellationToken cancellationToken)
    {
        var allNAPsQuery = _NAPRepository.Query(n => !n.IsDeleted);

        Point locationPoint = GeometryHelper.CreatePoint(request.Coordinate);

        allNAPsQuery = allNAPsQuery
            .Where(n => !request.ServiceProviderId.HasValue || n.ServiceProvider.Id == request.ServiceProviderId)
            .Where(n => locationPoint.IsValid && n.Location.IsWithinDistance(locationPoint, request.Distance))
            .OrderBy(n => n.Location.Distance(locationPoint));

        var NAPsDtoQuery = allNAPsQuery
        .Select(n => new NAPDto
        {
            Id = n.Id,
            ISP = n.ServiceProvider.Provider.Name,
            ISPColor = n.ServiceProvider.Provider.Color,
            Lat = n.Location.Coordinate.Y,
            Lng = n.Location.Coordinate.X,
            Dist = 0
        });
        return await _NAPRepository.PaginateAsync(NAPsDtoQuery, request.PageNumber, request.PageSize, cancellationToken);
    
    }
}
