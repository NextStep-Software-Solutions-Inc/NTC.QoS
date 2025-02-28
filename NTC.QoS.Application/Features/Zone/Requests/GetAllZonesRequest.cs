using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Zone;

namespace NTC.QoS.Application.Features.Zone.Requests;

public class GetAllZonesRequest : IRequest<PaginatedResponse<ZoneDto>>
{
    public GetAllZonesRequest(int pageSize, int pageNumber)
    {
        PageSize = pageSize;
        PageNumber = pageNumber;
    }
    public GetAllZonesRequest() { }
    public int PageSize { get; set; }
    public int PageNumber { get; set; }
    public Guid? BarangayId { get; set; }
}
