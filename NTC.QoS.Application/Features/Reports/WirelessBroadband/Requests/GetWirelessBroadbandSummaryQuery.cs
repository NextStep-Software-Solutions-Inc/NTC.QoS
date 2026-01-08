using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Reports;

namespace NTC.QoS.Application.Features.Reports.WirelessBroadband.Requests;

/// <summary>
/// Query to get wireless broadband summary statistics (executive summary).
/// Returns high-level metrics including service status counts, average scores, and technology distribution.
/// </summary>
public class GetWirelessBroadbandSummaryQuery : IRequest<ApiResponse<WirelessBroadbandScoredSummaryDto>>
{
    public Guid? MunicipalityId { get; set; }
    public Guid? ProvinceId { get; set; }
    public Guid? RegionId { get; set; }
}
