using FluentValidation;
using NTC.QoS.Application.Features.Reports.WirelessBroadband.Requests;

namespace NTC.QoS.Application.Features.Reports.WirelessBroadband.Validators;

public class GetWirelessBroadbandReportQueryValidator : AbstractValidator<GetWirelessBroadbandReportQuery>
{
    public GetWirelessBroadbandReportQueryValidator()
    {
        RuleFor(x => x)
            .Must(x => 
            {
                int count = 0;
                if (x.MunicipalityId.HasValue) count++;
                if (x.ProvinceId.HasValue) count++;
                if (x.RegionId.HasValue) count++;
                return count == 1;
            })
            .WithMessage("Please provide exactly one of MunicipalityId, ProvinceId, or RegionId.");
    }
}
