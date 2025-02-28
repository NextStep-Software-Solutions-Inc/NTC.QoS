using MediatR;
using NTC.QoS.Application.Features.Network.Requests;
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

        }
        var allNetwork = allNetworks.Select(n => new NetworkDto
        {
            Id = n.Id,
            Name = n.Name,
            NetworkTemplateId = n.NetworkTemplateId,
            SystemType = n.Type,
            UplinkId = n.UpLinkId,
            DownLinkId = n.DownLinkId
        }).ToList();
        return new PaginatedResponse<NetworkDto>(allNetwork, request.PageNumber, request.PageSize);
    }

}
