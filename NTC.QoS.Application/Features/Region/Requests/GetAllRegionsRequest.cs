using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Region;

namespace NTC.QoS.Application.Features.Region.Requests;

public class GetAllRegionsRequest : IRequest<PaginatedResponse<RegionDto>>
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