using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Cell;

namespace NTC.QoS.Application.Features.Cells.Requests;

public class UpdateCellByIdRequest : IRequest<ApiResponse<CellDto>>
{
    public Guid Id { get; set; }
    public UpdateCellDto UpdateCell { get; set; }
    public UpdateCellByIdRequest(Guid id, UpdateCellDto updateCell)
    {
        Id = id;
        UpdateCell = updateCell;
    }
}
