using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Technology;
namespace NTC.QoS.Application.Features.Technology.Requests;

public class GetAllTechnologiesRequest : IRequest<PaginatedResponse<TechnologyDto>>
{
    public GetAllTechnologiesRequest() { }

    public int PageSize { get; set; }
    public int PageNumber { get; set; }
    public List<Guid>? GenerationIds { get; set; }
}


