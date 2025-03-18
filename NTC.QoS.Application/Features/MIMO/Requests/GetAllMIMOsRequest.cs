using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.MIMO;

namespace NTC.QoS.Application.Features.MIMO.Requests;

public class GetAllMIMOsRequest : IRequest<PaginatedResponse<MIMODto>>
{
    public GetAllMIMOsRequest(int pageSize, int pageNumber)
    {
        PageSize = pageSize;
        PageNumber = pageNumber;
    }
    public GetAllMIMOsRequest() { }
    public int PageSize { get; set; }
    public int PageNumber { get; set; }
}
