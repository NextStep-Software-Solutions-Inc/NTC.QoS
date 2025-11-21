using MediatR;
using NTC.QoS.Application.Features.Reports.WirelessBroadband.DTOs;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.Reports.WirelessBroadband.Requests;

public class GetWirelessBroadbandReportQuery : IRequest<ApiResponse<WirelessBroadbandReportResult>>
{
    public Guid? MunicipalityId { get; set; }
    public Guid? ProvinceId { get; set; }
    public Guid? RegionId { get; set; }

    public GetWirelessBroadbandReportQuery() { }
}
