using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Provider;

namespace NTC.QoS.Application.Features.Provider.Requests;

public class GetAllProvidersRequest(int pageSize, int pageNumber, Guid? serviceId) : IRequest<PaginatedResponse<ProviderDto>>
{
    public int PageSize { get; set; } = pageSize;
    public int PageNumber { get; set; } = pageNumber;
    public Guid? ServiceId { get; set; } = serviceId;
}
