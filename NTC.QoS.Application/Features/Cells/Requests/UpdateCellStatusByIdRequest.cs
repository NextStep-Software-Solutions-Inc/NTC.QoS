using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Cell;

namespace NTC.QoS.Application.Features.Cells.Requests;

public class UpdateCellStatusByIdRequest : IRequest<ApiResponse<CellDto>>
{
    public Guid Id { get; set; }
    public UpdateCellStatusDto UpdateCellStatusDto { get; set; }
    public UpdateCellStatusByIdRequest(Guid id, UpdateCellStatusDto updateCellStatusDto)
    {
        Id = id;
        UpdateCellStatusDto = updateCellStatusDto;
    }
}