using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Band;

namespace NTC.QoS.Application.Features.Band.Requests;

public class GetAllBandsRequest : IRequest<PaginatedResponse<BandDto>>
{
    public GetAllBandsRequest(int pageSize, int pageNumber)
    {
        PageSize = pageSize;
        PageNumber = pageNumber;
    }

    public GetAllBandsRequest() { }

    public int PageSize { get; set; }
    public int PageNumber { get; set; }
    public List<Guid>? Technologies { get; set; }

}