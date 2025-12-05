using MediatR;
using NTC.QoS.Application.Features.Network.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.Network;

namespace NTC.QoS.Application.Features.Network.Handlers.Queries;

public class GetAllNetworksHandler(INetworkRepository networkRepository) : IRequestHandler<GetAllNetworksRequest, PaginatedResponse<NetworkDto>>
{
    private readonly INetworkRepository _networkRepository = networkRepository;

    public async Task<PaginatedResponse<NetworkDto>> Handle(GetAllNetworksRequest request, CancellationToken cancellationToken)
    {
        var networksQuery = _networkRepository.QueryAsNoTracking();

        if (!string.IsNullOrEmpty(request.Search))
        {
            networksQuery = _networkRepository.SearchByName(networksQuery, request.Search);
        }

        networksQuery = networksQuery
            .Where(n => !request.NetworkTemplateId.HasValue || (n.NetworkTemplate != null && n.NetworkTemplate.Id == request.NetworkTemplateId))
            .Where(n => !request.SystemTypeId.HasValue || n.SystemTypeId == request.SystemTypeId)
            .Where(n => !request.TechnologyId.HasValue || n.SystemType.TechnologyId == request.TechnologyId)
            .Where(n => !request.BandId.HasValue || n.BandId == request.BandId)
            .Where(n => !request.ServiceProviderId.HasValue || n.ServiceProviderId == request.ServiceProviderId)
            .Where(n => !request.CellId.HasValue || n.Sectors.Any(s => s.CellSector != null && s.CellSector.CellId == request.CellId));

        // Location filtering based on location type
        if (request.LocationId.HasValue && !string.IsNullOrEmpty(request.LocationType))
        {
            networksQuery = request.LocationType.ToLower() switch
            {
                "municipality" => networksQuery.Where(n => n.MunicipalityId == request.LocationId),
                "province" => networksQuery.Where(n => n.Municipality != null && n.Municipality.ProvinceId == request.LocationId),
                "region" => networksQuery.Where(n => n.Municipality != null && n.Municipality.Province != null && n.Municipality.Province.RegionId == request.LocationId),
                _ => networksQuery
            };
        }

        networksQuery = networksQuery.OrderBy(n => n.Name);

        var networkDtos = networksQuery.Select(n => new NetworkDto
        {
            Id = n.Id,
            Name = n.Name,
            NetworkTemplate = n.NetworkTemplate == null ? null : new()
            {
                Id = n.NetworkTemplate.Id,
                Name = n.NetworkTemplate.Name,
                Description = n.NetworkTemplate.Description,
                URL = n.NetworkTemplate.URL
            },
            SystemTypeName = n.SystemType.Name,
            ServiceProvider = new()
            {
                Id = n.ServiceProvider.Id,
                MNC = n.ServiceProvider.MNC,
                MCC = n.ServiceProvider.MCC,
                Provider = new()
                {
                    Id = n.ServiceProvider.ProviderId,
                    Name = n.ServiceProvider.Provider.Name,
                    Description = n.ServiceProvider.Provider.Description,
                    Color = n.ServiceProvider.Provider.Color
                },
                Service = new()
                {
                    Id = n.ServiceProvider.ServiceId,
                    Name = n.ServiceProvider.Service.Name,
                    Type = n.ServiceProvider.Service.Type.ToString()
                }
            },
            Band = new()
            {
                Id = n.Band.Id,
                Name = n.Band.Name,
                Description = n.Band.Description
            },
            Municipality = n.Municipality == null ? null : new()
            {
                Id = n.Municipality.Id,
                Name = n.Municipality.Name,
                Description = n.Municipality.Description,
                IsCity = n.Municipality.IsCity,
                Code = n.Municipality.Code,
                Area = n.Municipality.Area,
                Province = n.Municipality.Province == null ? null : new()
                {
                    Id = n.Municipality.Province.Id,
                    Name = n.Municipality.Province.Name,
                    Description = n.Municipality.Province.Description,
                    Code = n.Municipality.Province.Code
                }
            },
            ULCenterFreq = n.ULCenterFreq,
            DLCenterFreq = n.DLCenterFreq,
            Bandwidth = n.Bandwidth
        });

        return await _networkRepository.PaginateAsync(networkDtos, request.PageNumber, request.PageSize, cancellationToken);
    }
}
