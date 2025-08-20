using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Cell;

namespace NTC.QoS.Application.Features.Cells.Requests;

public class GetCellByIdRequest(Guid id) : IRequest<ApiResponse<CellDto>>
{
    public Guid Id { get; set; } = id;
}
