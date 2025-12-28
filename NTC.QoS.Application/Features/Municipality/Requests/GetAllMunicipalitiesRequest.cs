using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Municipality;

namespace NTC.QoS.Application.Features.Municipality.Requests;

public class GetAllMunicipalitiesRequest : IRequest<PaginatedResponse<MunicipalityDto>>
{
    public GetAllMunicipalitiesRequest(int pageSize, int pageNumber)
    {
        PageSize = pageSize;
        PageNumber = pageNumber;
    }
    public GetAllMunicipalitiesRequest() { }
    public int PageSize { get; set; }
    public int PageNumber { get; set; }
    public Guid? ProvinceId { get; set; }
    public bool? IsWiredCovered { get; set; }
    public bool? IsWirelessCovered { get; set; }
}
