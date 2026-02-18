using MediatR;
using NTC.QoS.Domain.Models.DTOs.ServiceProvider;

namespace NTC.QoS.Application.Features.ServiceProvider.Requests;

public class GetAllServiceProvidersRequest : IRequest<PaginatedResponse<ServiceProviderDto>>
{
    public GetAllServiceProvidersRequest() { }

    public int PageSize { get; set; }
    public int PageNumber { get; set; }
    public List<Guid>? ProviderIds { get; set; }
    public List<Guid>? ServiceIds { get; set; }
    public List<string>? ServiceTypes { get; set; }
    public string? Search { get; set; }
}
