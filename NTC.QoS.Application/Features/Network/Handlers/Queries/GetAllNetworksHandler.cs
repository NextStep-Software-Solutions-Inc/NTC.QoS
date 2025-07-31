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
        var networksQuery = _networkRepository.Query();

        networksQuery = networksQuery
            .Where(n => string.IsNullOrEmpty(request.Search) || (n.Name != null && n.Name.Contains(request.Search, StringComparison.OrdinalIgnoreCase)))
            .Where(n => !request.NetworkTemplateId.HasValue || n.NetworkTemplate.Id == request.NetworkTemplateId)
            .OrderBy(n => n.Name);

        var networkDtos = networksQuery.Select(n => new NetworkDto
        {
            Id = n.Id,
            Name = n.Name,
            NetworkTemplate = new()
            {
                Id = n.NetworkTemplateId,
                Name = n.NetworkTemplate.Name,
                Description = n.NetworkTemplate.Description,
                URL = n.NetworkTemplate.URL
            },
            SystemTypeName = n.SystemType.Name,
            UpLink = new()
            {
                Id = n.UpLinkId,
                Type = n.UpLink.Type.ToString(),
                CenterFrequency = n.UpLink.CenterFrequency
            },
            DownLink = new()
            {
                Id = n.DownLinkId,
                Type = n.DownLink.Type.ToString(),
                CenterFrequency = n.DownLink.CenterFrequency,
            },
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
        });

        return await _networkRepository.PaginateAsync(networkDtos, request.PageNumber, request.PageSize, cancellationToken);
    }
}
