using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Network;

namespace NTC.QoS.Application.Features.Network.Requests;

public class UpdateNetworkByIdRequest(Guid id, UpdateNetworkDto network) : IRequest<ApiResponse<UpdateNetworkDto>>
{
    public Guid Id { get; set; } = id;
    public UpdateNetworkDto Network { get; set; } = network;
}
