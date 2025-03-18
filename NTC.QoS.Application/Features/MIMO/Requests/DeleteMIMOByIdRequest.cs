using MediatR;
using NTC.QoS.Domain.Models.DTOs;
namespace NTC.QoS.Application.Features.MIMO.Requests;

public class DeleteMIMOByIdRequest : IRequest<ApiResponse<bool>>
{
    public DeleteMIMOByIdRequest(Guid id)
    {
        Id = id;
    }
    public Guid Id { get; set; }
}
