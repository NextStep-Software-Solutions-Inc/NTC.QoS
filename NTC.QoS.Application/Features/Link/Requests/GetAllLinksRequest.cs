using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Link;

namespace NTC.QoS.Application.Features.Link.Requests;

public class GetAllLinksRequest : IRequest<PaginatedResponse<LinkDto>>
{
    public GetAllLinksRequest(int pageSize, int pageNumber)
    {
        PageSize = pageSize;
        PageNumber = pageNumber;
    }
    public GetAllLinksRequest() { }

    public int PageSize { get; set; }
    public int PageNumber { get; set; }
    public Guid? ServiceProviderId { get; set; }
    public Guid? TechnologyBandId { get; set; }
}