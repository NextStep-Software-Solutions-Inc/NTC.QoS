using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Network;

namespace NTC.QoS.Application.Features.Network.Requests;

public class CreateNetworkRequest(CreateNetworkDto network) : IRequest<ApiResponse<CreateNetworkDto>>
{
    public CreateNetworkDto Network { get; set; } = network;
}
