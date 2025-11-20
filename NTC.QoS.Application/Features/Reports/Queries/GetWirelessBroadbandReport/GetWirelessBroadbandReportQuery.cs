using MediatR;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.Reports.Queries.GetWirelessBroadbandReport;

public class GetWirelessBroadbandReportQuery : IRequest<ApiResponse<WirelessBroadbandReportDto>>
{
    public Guid MunicipalityId { get; set; }

    public GetWirelessBroadbandReportQuery(Guid municipalityId)
    {
        MunicipalityId = municipalityId;
    }
}
