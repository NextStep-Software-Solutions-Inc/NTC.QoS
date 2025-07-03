using MediatR;
using NetTopologySuite.Geometries;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Cell;

namespace NTC.QoS.Application.Features.Cells.Requests;

public class GetAllCellsRequest(int pageNumber, int pageSize) : IRequest<PaginatedResponse<CellDto>>
{
    public int PageNumber { get; set; } = pageNumber;
    public int PageSize { get; set; } = pageSize;
    public string? Search { get; set; }
    public string? Status { get; set; }
    public List<Guid>? ServiceProviderIds { get; set; }
    public List<Guid>? ProviderIds { get; set; }
    public List<Guid>? ServiceIds { get; set; }
    public required Coordinate Coordinate { get; set; }
    public double Distance { get; set; }
    public List<Guid>? BarangayIds { get; set; }
}
