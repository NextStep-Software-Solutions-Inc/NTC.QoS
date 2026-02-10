using MediatR;
using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Cell;

namespace NTC.QoS.Application.Features.Cells.Requests;

public class CreateCellRequest(CreateCellDto cell) : IRequest<ApiResponse<CellDto>>
{
    public CreateCellDto Cell { get; set; } = cell;
    
    /// <summary>
    /// Optional region ID for assessment triggering. If not provided, will attempt spatial lookup.
    /// </summary>
    public Guid? RegionId { get; set; }
}
