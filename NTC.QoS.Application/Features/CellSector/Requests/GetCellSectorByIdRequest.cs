using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.CellSector;

namespace NTC.QoS.Application.Features.CellSector.Requests;

public class GetCellSectorByIdRequest(Guid id) : IRequest<ApiResponse<CellSectorDto>>
{
    public Guid Id { get; set; } = id;
}
