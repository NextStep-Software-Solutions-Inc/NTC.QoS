using MediatR;
using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.Cells.Requests;

public class GetAllCellsRequest : IRequest<PaginatedResponse<Cell>>
{
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
}
