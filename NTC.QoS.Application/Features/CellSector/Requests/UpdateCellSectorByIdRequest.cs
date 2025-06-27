using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.CellSector;

namespace NTC.QoS.Application.Features.CellSector.Requests;

public class UpdateCellSectorByIdRequest() : IRequest<ApiResponse<UpdateCellSectorDto>>
{
    public Guid Id { get; set; }
    public required UpdateCellSectorDto CellSector { get; set; }
}
