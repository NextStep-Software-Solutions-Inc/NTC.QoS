using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Cell;

namespace NTC.QoS.Application.Features.Cells.Requests;

public class GetAllCellsRequest : IRequest<PaginatedResponse<CellDto>>
{
    public GetAllCellsRequest(int pageNumber, int pageSize, string? search)
    {
        PageNumber = pageNumber;
        PageSize = pageSize;
        Search = search;
    }

    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public string? Search { get; set; }
}
