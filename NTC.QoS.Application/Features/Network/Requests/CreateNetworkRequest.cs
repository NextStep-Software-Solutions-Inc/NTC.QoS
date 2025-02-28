using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Network;

namespace NTC.QoS.Application.Features.Network.Requests;

public class CreateNetworkRequest : IRequest<ApiResponse<NetworkDto>>
{
    public CreateNetworkRequest(CreateNetworkDto createNetwork)
    {
        CreateNetwork = createNetwork;
    }
    public CreateNetworkDto CreateNetwork { get; set; }
}
