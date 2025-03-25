using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.NAP;

namespace NTC.QoS.Application.Features.NAP.Requests;

public class GetAllNAPsRequest : IRequest<PaginatedResponse<NAPDto>>
{
    public GetAllNAPsRequest(int pageSize, int pageNumber)
    {
        PageSize = pageSize;
        PageNumber = pageNumber;
    }

    public GetAllNAPsRequest() { }

    public int PageSize { get; set; }
    public int PageNumber { get; set; }
    public Guid? ServiceProviderId { get; set; }
    public Guid? ZoneId { get; set; }
    public Guid? FileId { get; set; }
}