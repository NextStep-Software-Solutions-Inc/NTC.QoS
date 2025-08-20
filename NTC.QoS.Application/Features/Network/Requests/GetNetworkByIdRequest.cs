using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Network;

namespace NTC.QoS.Application.Features.Network.Requests;

public class GetNetworkByIdRequest(Guid id) : IRequest<ApiResponse<NetworkDto>>
{
    public Guid Id { get; set; } = id;
}
