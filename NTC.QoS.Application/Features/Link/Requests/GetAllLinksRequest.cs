using MediatR;
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
    public List<Guid>? ServiceProviderIds { get; set; }
    public List<Guid>? BandIds { get; set; }
    public string? Search { get; set; }
}