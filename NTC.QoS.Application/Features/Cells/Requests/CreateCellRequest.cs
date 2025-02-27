using MediatR;
using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Cell;

namespace NTC.QoS.Application.Features.Cells.Requests;

public class CreateCellRequest : IRequest<ApiResponse<CellDto>>
{
    public required CreateCellDto CreateCell { get; set; }

}
