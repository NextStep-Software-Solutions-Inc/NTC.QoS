using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Sector;

namespace NTC.QoS.Application.Features.Sector.Requests;

public class GetSectorByIdRequest(Guid id) : IRequest<ApiResponse<SectorDto>>
{
    public Guid Id { get; set; } = id;
}
