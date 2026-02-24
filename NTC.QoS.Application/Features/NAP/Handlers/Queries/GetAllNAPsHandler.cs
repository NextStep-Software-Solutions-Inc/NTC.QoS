using MediatR;
using NetTopologySuite.Geometries;
using NTC.QoS.Application.Features.NAP.Requests;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.NAP;
using NTC.QoS.Infrastructure.Helper;

namespace NTC.QoS.Application.Features.NAP.Handlers.Queries;

public class GetAllNAPsHandler(INAPRepository NAPRepository,
    IRepository<Domain.Entities.Barangay> barangayRepository,
    IRepository<Domain.Entities.Municipality> municipalityRepository,
    IRepository<Domain.Entities.Province> provinceRepository,
    IRepository<Domain.Entities.Region> regionRepository) : IRequestHandler<GetAllNAPsRequest, PaginatedResponse<NAPDto>>
{
    private readonly INAPRepository _NAPRepository = NAPRepository;
    private readonly IRepository<Domain.Entities.Barangay> _barangayRepository = barangayRepository;
    private readonly IRepository<Domain.Entities.Municipality> _municipalityRepository = municipalityRepository;
    private readonly IRepository<Domain.Entities.Province> _provinceRepository = provinceRepository;
    private readonly IRepository<Domain.Entities.Region> _regionRepository = regionRepository;

    public async Task<PaginatedResponse<NAPDto>> Handle(GetAllNAPsRequest request, CancellationToken cancellationToken)
    {
        var napQuery = _NAPRepository.Query();

        Point locationPoint = GeometryHelper.CreatePoint(request.Coordinate);

        napQuery = napQuery
            .Where(n => request.ServiceProviderIds == null || request.ServiceProviderIds.Contains(n.ServiceProviderId))
            .Where(n => locationPoint.IsValid && n.Location.IsWithinDistance(locationPoint, request.Distance));

        if (request.BarangayIds is { Count: > 0 })
        {
            napQuery = await GeoQueryHelper.ApplyBoundaryFilterAsync(request.BarangayIds, _barangayRepository, napQuery, cancellationToken);
        }
        else if (request.MunicipalityIds is { Count: > 0 })
        {
            napQuery = await GeoQueryHelper.ApplyBoundaryFilterAsync(request.MunicipalityIds, _municipalityRepository, napQuery, cancellationToken);
        }
        else if (request.ProvinceIds is { Count: > 0 })
        {
            napQuery = await GeoQueryHelper.ApplyBoundaryFilterAsync(request.ProvinceIds, _provinceRepository, napQuery, cancellationToken);
        }
        else if (request.RegionIds is { Count: > 0 })
        {
            napQuery = await GeoQueryHelper.ApplyBoundaryFilterAsync(request.RegionIds, _regionRepository, napQuery, cancellationToken);
        }
        napQuery = napQuery.OrderBy(n => n.Location.Distance(locationPoint));

        // Filter out NAPs with missing Provider data to ensure data integrity
        napQuery = napQuery.Where(n => n.ServiceProvider != null && n.ServiceProvider.Provider != null);

        var NAPsDtoQuery = napQuery
        .Select(n => new NAPDto
        {
            Id = n.Id,
            ISP = n.ServiceProvider.Provider.Name,
            ISPColor = n.ServiceProvider.Provider.Color,
            Lat = n.Location.Coordinate.Y,
            Lng = n.Location.Coordinate.X,
            Dist = Math.Round(n.Location.Distance(locationPoint), 2)
        });

        return await _NAPRepository.PaginateAsync(NAPsDtoQuery, request.PageNumber, request.PageSize, cancellationToken);
    }
}
