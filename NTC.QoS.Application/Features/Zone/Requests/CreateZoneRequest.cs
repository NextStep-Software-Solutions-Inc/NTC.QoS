using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Zone;

namespace NTC.QoS.Application.Features.Zone.Requests;

public class CreateZoneRequest : IRequest<ApiResponse<ZoneDto>>
{
    public required CreateZoneDto Zone { get; set; }
}
