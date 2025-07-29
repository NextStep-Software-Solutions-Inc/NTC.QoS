using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Dashboard;

namespace NTC.QoS.Application.Features.Dashboard.Requests;

public class GetWirelessCoverageRequest : IRequest<ApiResponse<WirelessCoverageResponseDto>>
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public Guid? RegionId { get; set; }
}
