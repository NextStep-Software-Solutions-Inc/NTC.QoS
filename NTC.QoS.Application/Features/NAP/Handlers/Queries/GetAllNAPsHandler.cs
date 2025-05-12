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
                ServiceProvider = new()
                {
                    Id = n.ServiceProvider.Id,
                    Service = new()
                    {
                        Id = n.ServiceProvider.Service.Id,
                        Name = n.ServiceProvider.Service.Name,
                        Type = n.ServiceProvider.Service.Type.ToString(),
                    },
                    Provider = new()
                    {
                        Id = n.ServiceProvider.Provider.Id,
                        Name = n.ServiceProvider.Provider.Name,
                        Color = n.ServiceProvider.Provider.Color,
                    }
                },
                Address = n.Address,
                IsFileUploaded = n.IsFileUploaded,
                Latitude = n.Location.X,
                Longitude = n.Location.Y,
                Distance = Math.Round(n.Location.Distance(locationPoint), 2)
            });
        return await _NAPRepository.PaginateAsync(NAPsDtoQuery, request.PageNumber, request.PageSize, cancellationToken);
    
    }
}
