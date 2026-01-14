using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Barangay;

namespace NTC.QoS.Application.Features.Barangay.Requests;

public class GetAllBarangaysRequest : IRequest<PaginatedResponse<BarangayDto>>
{
    public GetAllBarangaysRequest(int pageSize, int pageNumber)
    {
        PageSize = pageSize;
        PageNumber = pageNumber;
    }
    public GetAllBarangaysRequest() { }
    public int PageSize { get; set; }
    public int PageNumber { get; set; }
    public Guid[]? MunicipalityIds { get; set; }
    public Guid[]? RegionIds { get; set; }
    public string? Name { get; set; }
    public string? Municipality { get; set; }
    public string? Province { get; set; }
    public bool? IsWiredCovered { get; set; }
    public bool? IsWirelessCovered { get; set; }
}
