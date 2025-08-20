using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Cell;

namespace NTC.QoS.Application.Features.Cells.Requests;

public class UpdateCellByIdRequest(Guid id, UpdateCellDto cell) : IRequest<ApiResponse<CellDto>>
{
    public Guid Id { get; set; } = id;
    public UpdateCellDto Cell { get; set; } = cell;
}
