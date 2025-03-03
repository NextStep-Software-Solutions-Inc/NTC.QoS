using MediatR;
using NTC.QoS.Application.Features.Network.Requests;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Network;

namespace NTC.QoS.Application.Features.Network.Handlers.Queries;

public class GetAllNetworksHandler(INetworkRepository networkRepository) : IRequestHandler<GetAllNetworksRequest, PaginatedResponse<NetworkDto>>
{
    private readonly INetworkRepository _networkRepository = networkRepository;
    public async Task<PaginatedResponse<NetworkDto>> Handle(GetAllNetworksRequest request, CancellationToken cancellationToken)
    {
        var allNetworks = await _networkRepository.ListAsync(n => !n.IsDeleted, cancellationToken);
        if(request.Search != null)
        {
            allNetworks.Where(network => network.Name.Contains(request.Search, StringComparison.OrdinalIgnoreCase)).ToList();
        }
        var allNetwork = allNetworks.Select(static n => new NetworkDto
        {
            Id = n.Id,
            Name = n.Name,
            NetworkTemplateId = n.NetworkTemplateId,
            NetworkTemplate = n.NetworkTemplate != null ? new NetworkTemplateDto
            {
                Name = n.NetworkTemplate.Name
            } : null!,

            SystemType = EnumHelper<NetworkSystemTypeEnum>.GetName(n.SystemType),

            UplinkId = n.UpLinkId,
            Uplink = n.UpLink != null && n.UpLink.Band != null ? new LinkDto
            {
                Name = n.UpLink.Band.Name
            } : null!,

            DownLinkId = n.DownLinkId,
            DownLink = n.DownLink != null && n.DownLink.Band != null ? new LinkDto
            {
                Name = n.DownLink.Band.Name
            } : null!

        }).ToList();

        return new PaginatedResponse<NetworkDto>(allNetwork, request.PageNumber, request.PageSize);
    }

}
