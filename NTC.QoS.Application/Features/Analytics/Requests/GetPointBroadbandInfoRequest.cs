using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Analytics;

namespace NTC.QoS.Application.Features.Analytics.Requests;

public record GetPointBroadbandInfoRequest : IRequest<ApiResponse<AreaBroadbandInfoDto>>
{
    public required PointRadiusDto PointRadius { get; set; }
}