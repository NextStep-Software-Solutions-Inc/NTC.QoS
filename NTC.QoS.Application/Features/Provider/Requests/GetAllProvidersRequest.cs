using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Provider;

namespace NTC.QoS.Application.Features.Provider.Requests;

public class GetAllProvidersRequest : IRequest<PaginatedResponse<ProviderDto>>
{
    public GetAllProvidersRequest(int pageSize, int pageNumber)
    {
        PageSize = pageSize;
        PageNumber = pageNumber;
    }
    public GetAllProvidersRequest() { }

    public int PageSize { get; set; }
    public int PageNumber { get; set; }
}


