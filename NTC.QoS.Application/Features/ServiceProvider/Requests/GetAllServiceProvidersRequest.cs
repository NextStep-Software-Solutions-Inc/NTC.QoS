using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.ServiceProvider;

namespace NTC.QoS.Application.Features.ServiceProvider.Requests;

public class GetAllServiceProvidersRequest : IRequest<PaginatedResponse<ServiceProviderDto>>
{
    public GetAllServiceProvidersRequest(int pageSize, int pageNumber)
    {
        PageSize = pageSize;
        PageNumber = pageNumber;
    }
    public GetAllServiceProvidersRequest() { }
    public int PageSize { get; set; }
    public int PageNumber { get; set; }
}
