using FluentValidation;
using NTC.QoS.Application.Features.Reports.WirelessBroadband.Requests;

namespace NTC.QoS.Application.Features.Reports.WirelessBroadband.Validators;

public class GetWirelessBroadbandReportQueryValidator : AbstractValidator<GetWirelessBroadbandReportQuery>
{
    public GetWirelessBroadbandReportQueryValidator()
    {
        // MANDATORY: Require at least one geographic filter to prevent unscoped queries
        RuleFor(x => x)
            .Must(x => x.RegionId.HasValue || x.ProvinceId.HasValue || x.MunicipalityId.HasValue)
            .WithMessage("At least one geographic filter (RegionId, ProvinceId, or MunicipalityId) is required to generate the report. This prevents performance issues from processing all municipalities.");
    }
}
