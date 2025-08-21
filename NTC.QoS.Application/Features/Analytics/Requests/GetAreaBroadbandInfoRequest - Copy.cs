using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Analytics;

namespace NTC.QoS.Application.Features.Analytics.Requests;

public class GetWirelessCoverageAnalyticsRequest : IRequest<ApiResponse<WirelessCoverageAnalyticsDto>>
{
    public BoundingBoxDto BoundingBox { get; set; }
}
