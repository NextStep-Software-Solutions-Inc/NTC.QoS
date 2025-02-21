using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Cell;

namespace NTC.QoS.Application.Features.Cells.Requests;

public class CreateCellRequest : IRequest<ApiResponse<CreateCellDto>>
{
    public CreateCellDto CreateCell { get; set; } = new();

}
