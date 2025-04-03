using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Cell;

namespace NTC.QoS.Application.Features.Cells.Requests;

public class UpdateCellStatusByIdRequest(Guid id, string status) : IRequest<ApiResponse<CellDto>>
{
    public Guid Id { get; set; } = id;
    public string Status { get; set; } = status;
}