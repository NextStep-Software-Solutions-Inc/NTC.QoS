using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Service;

namespace NTC.QoS.Application.Features.Service.Requests;

public class GetAllServicesRequest : IRequest<PaginatedResponse<ServiceDto>>
{
    public GetAllServicesRequest(int pageSize, int pageNumber)
    {
        PageSize = pageSize;
        PageNumber = pageNumber;
    }
    public GetAllServicesRequest() { }
    public int PageSize { get; set; }
    public int PageNumber { get; set; }
}
