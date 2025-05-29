using MediatR;
using NTC.QoS.Application.Features.Network.Requests;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Network;
using NTC.QoS.Domain.Models.DTOs.NetworkTemplate;

namespace NTC.QoS.Application.Features.Network.Handlers.Queries;

public class GetAllNetworksHandler(INetworkRepository networkRepository) : IRequestHandler<GetAllNetworksRequest, PaginatedResponse<NetworkDto>>
{
    private readonly INetworkRepository _networkRepository = networkRepository;
    public async Task<PaginatedResponse<NetworkDto>> Handle(GetAllNetworksRequest request, CancellationToken cancellationToken)
    {
        var networksQuery = _networkRepository.Query(n => !n.IsDeleted);

        var networkDtos = networksQuery.Select(n => new NetworkDto
        {
            Id = n.Id,
            Name = n.Name,
            NetworkTemplate = new NetworkTemplateDto
            {
                Id = n.NetworkTemplateId,
                Name = n.NetworkTemplate.Name,
                Description = n.NetworkTemplate.Description,
                URL = n.NetworkTemplate.URL
            },
            SystemTypeName = n.SystemType.Name,
            Uplink = new LinkNetworkDto
            {
                Id = n.UpLinkId,
                Type = EnumHelper<LinkTypeEnum>.GetName(n.UpLink.Type),
                CenterFrequency = n.UpLink.CenterFrequency
            },
            DownLink = new LinkNetworkDto
            {
                Id = n.DownLinkId,
                Type = EnumHelper<LinkTypeEnum>.GetName(n.DownLink.Type),
                CenterFrequency = n.DownLink.CenterFrequency,
            }
        });

        networkDtos = networkDtos
            .Where(n => string.IsNullOrEmpty(request.Search) || (n.Name != null && n.Name.Contains(request.Search, StringComparison.OrdinalIgnoreCase)))
            .Where(n => !request.NetworkTemplateId.HasValue || n.NetworkTemplate.Id == request.NetworkTemplateId);

        return await _networkRepository.PaginateAsync(networkDtos, request.PageNumber, request.PageSize, cancellationToken);
    }
}
