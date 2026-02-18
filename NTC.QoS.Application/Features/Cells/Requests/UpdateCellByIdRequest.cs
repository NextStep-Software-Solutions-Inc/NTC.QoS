using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Cell;

namespace NTC.QoS.Application.Features.Cells.Requests;

public class UpdateCellByIdRequest(Guid id, UpdateCellDto cell) : IRequest<ApiResponse<CellDto>>
{
    public Guid Id { get; set; } = id;
    public UpdateCellDto Cell { get; set; } = cell;
    
    /// <summary>
    /// Optional region ID for assessment triggering. If not provided, will attempt spatial lookup.
    /// </summary>
    public Guid? RegionId { get; set; }
}
