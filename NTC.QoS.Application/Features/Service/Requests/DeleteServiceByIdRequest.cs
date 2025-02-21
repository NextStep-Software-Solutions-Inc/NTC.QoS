using MediatR;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.Service.Requests;

public class DeleteServiceByIdRequest : IRequest<ApiResponse<bool>>
{
    public DeleteServiceByIdRequest(Guid id)
    {
        Id = id;
    }
    public Guid Id { get; set; }
}
