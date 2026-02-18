using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Service;

namespace NTC.QoS.Application.Features.Service.Requests;

public class GetAllServicesRequest(int pageSize, int pageNumber, List<Guid>? providerIds, List<string>? types, string? search = null) : IRequest<PaginatedResponse<ServiceDto>>
{
    public int PageSize { get; set; } = pageSize;
    public int PageNumber { get; set; } = pageNumber;
    public List<Guid>? ProviderIds { get; set; } = providerIds;
    public List<string>? Types { get; set; } = types;
    public string? Search { get; set; } = search;
}
