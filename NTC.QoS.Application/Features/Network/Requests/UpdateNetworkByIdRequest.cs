using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Network;

namespace NTC.QoS.Application.Features.Network.Requests;

public class UpdateNetworkByIdRequest : IRequest<ApiResponse<NetworkDto>>
{
    public UpdateNetworkByIdRequest(Guid id, UpdateNetworkDto network)
    {
        Id = id;
        Network = network;
    }

    public UpdateNetworkDto Network { get; set; }
    public Guid Id { get; set; }
}
