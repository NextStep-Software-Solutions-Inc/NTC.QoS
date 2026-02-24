using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.CellSector;

namespace NTC.QoS.Application.Features.CellSector.Requests;

public class GetAllCellSectorsRequest(int pageNumber, int pageSize, string? search = null) : IRequest<PaginatedResponse<CellSectorDto>>
{
    public int PageNumber { get; set; } = pageNumber;
    public int PageSize { get; set; } = pageSize;
    public List<Guid>? CellIds { get; set; }
    public string? Search { get; set; } = search;
}
