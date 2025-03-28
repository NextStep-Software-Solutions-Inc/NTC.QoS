using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.ServiceProvider;

namespace NTC.QoS.Application.Features.ServiceProvider.Requests;

public class GetAllServiceProvidersRequest(int pageSize, int pageNumber, Guid? providerId, Guid? serviceId) : IRequest<PaginatedResponse<ServiceProviderDto>>
{
    public int PageSize { get; set; } = pageSize;
    public int PageNumber { get; set; } = pageNumber;
    public Guid? ProviderId { get; set; } = providerId;
    public Guid? ServiceId { get; set; } = serviceId;
}
