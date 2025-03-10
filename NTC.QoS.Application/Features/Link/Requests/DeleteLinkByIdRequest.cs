using MediatR;
using NTC.QoS.Domain.Models.DTOs;
namespace NTC.QoS.Application.Features.Link.Requests;

public class DeleteLinkByIdRequest : IRequest<ApiResponse<bool>>
{
    public DeleteLinkByIdRequest(Guid id)
    {
        Id = id;
    }
    public Guid Id { get; set; }
}


