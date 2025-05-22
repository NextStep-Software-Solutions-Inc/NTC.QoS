using MediatR;
using NetTopologySuite.Geometries;
using NTC.QoS.Application.Features.NAP.Requests;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.NAP;

namespace NTC.QoS.Application.Features.NAP.Handlers.Queries;

public class GetAllNAPsHandler(INAPRepository NAPRepository) : IRequestHandler<GetAllNAPsRequest, PaginatedResponse<ClusteredNapResultDto>>
{
    private readonly INAPRepository _NAPRepository = NAPRepository;

    public async Task<PaginatedResponse<ClusteredNapResultDto>> Handle(GetAllNAPsRequest request, CancellationToken cancellationToken)
    {

        var clusteredNapsQuery = await  _NAPRepository.GetClusteredNapsAsync(request.Distance);

        return new PaginatedResponse<ClusteredNapResultDto>(clusteredNapsQuery, pageNumber: request.PageNumber, pageSize: request.PageSize);

        //return await _NAPRepository.PaginateAsync(clusteredNapsQuery, pageSize: request.PageSize, pageNumber: request.PageNumber);
        //var allNAPsQuery = _NAPRepository.Query(n => !n.IsDeleted);

        //var NAPsDtoQuery = allNAPsQuery
        //.Select(n => new NAPDto
        //{
        //    Id = n.Id,
        //    ISP = n.ServiceProvider.Provider.Name,
        //    ISPColor = n.ServiceProvider.Provider.Color,
        //    Lat = n.Location.Coordinate.Y,
        //    Lng = n.Location.Coordinate.X,
        //    Dist = 0
        //});
        //return await _NAPRepository.PaginateAsync(NAPsDtoQuery, request.PageNumber, request.PageSize, cancellationToken);
    
    }
}
