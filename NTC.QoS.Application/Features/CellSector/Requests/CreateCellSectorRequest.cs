using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.CellSector;

namespace NTC.QoS.Application.Features.CellSector.Requests;

public class CreateCellSectorRequest : IRequest<ApiResponse<CellSectorDto>>
{
    public required CreateCellSectorDto CellSector { get; set; }
}
