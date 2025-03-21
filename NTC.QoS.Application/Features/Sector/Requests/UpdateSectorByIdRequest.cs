using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Sector;

namespace NTC.QoS.Application.Features.Sector.Requests;

public class UpdateSectorByIdRequest(Guid id, UpdateSectorDto sector) : IRequest<ApiResponse<SectorDto>>
{
    public Guid Id { get; set; } = id;
    public UpdateSectorDto Sector { get; set; } = sector;
}
