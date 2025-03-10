using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Link;
namespace NTC.QoS.Application.Features.Link.Requests;

public class GetLinkByIdRequest : IRequest<ApiResponse<LinkDto>>
{
    public GetLinkByIdRequest(Guid id)
    {
        Id = id;
    }
    public Guid Id { get; set; }
}


