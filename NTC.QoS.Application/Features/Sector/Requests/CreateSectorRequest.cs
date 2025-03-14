using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Sector;

namespace NTC.QoS.Application.Features.Sector.Requests;

public class CreateSectorRequest(CreateSectorDto sector) : IRequest<ApiResponse<CreateSectorDto>>
{
    public CreateSectorDto Sector { get; set; } = sector;
}
