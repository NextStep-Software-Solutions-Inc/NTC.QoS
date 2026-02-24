using MediatR;
using NTC.QoS.Domain.Models.DTOs.Provider;

namespace NTC.QoS.Application.Features.Provider.Requests;

public class GetAllProvidersRequest(
    int pageSize,
    int pageNumber,
    List<Guid>? serviceIds,
    List<string>? names,
    bool? includeLogoData = null,
    string? search = null) : IRequest<PaginatedResponse<ProviderDto>>
{
    public int PageSize { get; set; } = pageSize;
    public int PageNumber { get; set; } = pageNumber;
    public List<Guid>? ServiceIds { get; set; } = serviceIds;
    public List<string>? Names { get; set; } = names;
    public bool? IncludeLogoData { get; set; } = includeLogoData;
    public string? Search { get; set; } = search;
}
