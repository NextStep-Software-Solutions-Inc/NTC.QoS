using MediatR;
using NTC.QoS.Domain.Models.DTOs;
namespace NTC.QoS.Application.Features.Technology.Requests;

public class DeleteTechnologyByIdRequest : IRequest<ApiResponse<bool>>
{
    public DeleteTechnologyByIdRequest(Guid id)
    {
        Id = id;
    }
    public Guid Id { get; set; }
}


