using MediatR;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.NAP.Requests;

public class DeleteNAPByIdRequest : IRequest<ApiResponse<bool>>
{
    public Guid Id { get; set; }
    public DeleteNAPByIdRequest(Guid id)
    {
        Id = id;
    }
}