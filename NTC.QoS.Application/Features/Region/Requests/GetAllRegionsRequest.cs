using MediatR;
using NTC.QoS.Domain.Models.DTOs;
namespace NTC.QoS.Application.Features.Region.Requests;

public class GetAllRegionsRequest : IRequest<PaginatedResponse<Domain.Entities.Region>>
{
    public GetAllRegionsRequest(int pageSize, int pageNumber)
    {
        PageSize = pageSize;
        PageNumber = pageNumber;
    }
    public GetAllRegionsRequest() { }

    public int PageSize { get; set; }
    public int PageNumber { get; set; }
}


