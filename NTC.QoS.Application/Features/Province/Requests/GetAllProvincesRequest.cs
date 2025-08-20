using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Province;
namespace NTC.QoS.Application.Features.Province.Requests;

public class GetAllProvincesRequest : IRequest<PaginatedResponse<ProvinceDto>>
{
    public GetAllProvincesRequest(int pageSize, int pageNumber)
    {
        PageSize = pageSize;
        PageNumber = pageNumber;
    }
    public GetAllProvincesRequest() { }

    public int PageSize { get; set; }
    public int PageNumber { get; set; }
    public Guid? RegionId { get; set; }
}


