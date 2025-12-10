using FluentValidation;
using NTC.QoS.Application.Features.Reports.WirelessBroadband.Requests;

namespace NTC.QoS.Application.Features.Reports.WirelessBroadband.Validators;

/// <summary>
/// Validator for wireless broadband scored report query
/// </summary>
public class GetWirelessBroadbandScoredReportQueryValidator : AbstractValidator<GetWirelessBroadbandScoredReportQuery>
{
    public GetWirelessBroadbandScoredReportQueryValidator()
    {
        // Ensure exactly one geographic scope is provided
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

        // Validate MunicipalityId if provided
        RuleFor(x => x.MunicipalityId)
            .GreaterThan(0)
            .When(x => x.MunicipalityId.HasValue)
            .WithMessage("MunicipalityId must be greater than 0.");

        // Validate ProvinceId if provided
        RuleFor(x => x.ProvinceId)
            .GreaterThan(0)
            .When(x => x.ProvinceId.HasValue)
            .WithMessage("ProvinceId must be greater than 0.");

        // Validate RegionId if provided
        RuleFor(x => x.RegionId)
            .GreaterThan(0)
            .When(x => x.RegionId.HasValue)
            .WithMessage("RegionId must be greater than 0.");

        // Validate Format
        RuleFor(x => x.Format)
            .IsInEnum()
            .WithMessage("Invalid report format. Must be Excel or PDF.");

        // Validate FilterByStatus if provided
        RuleFor(x => x.FilterByStatus)
            .IsInEnum()
            .When(x => x.FilterByStatus.HasValue)
            .WithMessage("Invalid service status filter. Must be Served, Underserved, or Unserved.");
    }
}