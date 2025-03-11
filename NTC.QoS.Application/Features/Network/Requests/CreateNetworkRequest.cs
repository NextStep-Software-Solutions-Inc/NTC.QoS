using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Network;

namespace NTC.QoS.Application.Features.Network.Requests;

public class CreateNetworkRequest(CreateNetworkDto createNetwork) : IRequest<ApiResponse<NetworkDto>>
{
    public CreateNetworkDto CreateNetwork { get; set; } = createNetwork;
}
