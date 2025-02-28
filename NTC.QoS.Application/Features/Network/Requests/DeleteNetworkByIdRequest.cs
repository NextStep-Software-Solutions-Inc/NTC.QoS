using MediatR;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.Network.Requests;

public class DeleteNetworkByIdRequest : IRequest<ApiResponse<bool>>
{
    public DeleteNetworkByIdRequest(Guid id)
    {
        Id = id;
    }
    public Guid Id { get; set; }

}
