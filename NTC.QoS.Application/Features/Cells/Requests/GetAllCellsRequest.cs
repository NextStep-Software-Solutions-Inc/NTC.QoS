using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Cell;

namespace NTC.QoS.Application.Features.Cells.Requests;

public class GetAllCellsRequest(int pageNumber,
                                int pageSize,
                                string? search,
                                string? status,
                                List<Guid>? serviceProviderIds,
                                List<Guid>? providerIds,
                                List<Guid>? serviceIds) : IRequest<PaginatedResponse<CellDto>>
{
    public int PageNumber { get; set; } = pageNumber;
    public int PageSize { get; set; } = pageSize;
    public string? Search { get; set; } = search;
    public string? Status { get; set; } = status;
    public List<Guid>? ServiceProviderIds { get; set; } = serviceProviderIds;
    public List<Guid>? ProviderIds { get; set; } = providerIds;
    public List<Guid>? ServiceIds { get; set; } = serviceIds;
}
