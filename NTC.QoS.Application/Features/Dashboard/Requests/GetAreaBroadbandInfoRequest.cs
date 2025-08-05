using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Dashboard;

namespace NTC.QoS.Application.Features.Dashboard.Requests;

public record GetAreaBroadbandInfoRequest : IRequest<ApiResponse<AreaBroadbandInfoDto>>
{
    public required AreaBroadbandInfoRequestDto AreaEnvelope { get; set; }
}