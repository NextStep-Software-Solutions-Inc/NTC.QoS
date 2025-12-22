using FluentValidation;
using NTC.QoS.Application.Features.Reports.WirelessBroadband.Requests;

namespace NTC.QoS.Application.Features.Reports.WirelessBroadband.Validators;

public class GetWirelessBroadbandReportQueryValidator : AbstractValidator<GetWirelessBroadbandReportQuery>
{
    public GetWirelessBroadbandReportQueryValidator()
    {
        // MANDATORY: Require at least one geographic filter to prevent unscoped queries
        RuleFor(x => x)
            .Must(HaveAtLeastOneGeographicFilter)
            .WithMessage("At least one geographic filter (RegionId, ProvinceId, or MunicipalityId) is required to generate the report. This prevents performance issues from processing all municipalities.")
            .WithName("GeographicFilters");

        // Note: Geographic filter precedence is handled in the query handler.
        // Lower admin levels take precedence: Municipality > Province > Region
        // Multiple filters can be provided, but the most specific will be used.
    }

    /// <summary>
    /// Validates that at least one geographic filter is provided to prevent unscoped queries
    /// </summary>
    private static bool HaveAtLeastOneGeographicFilter(GetWirelessBroadbandReportQuery query)
    {
        return query.RegionId.HasValue ||
               query.ProvinceId.HasValue ||
               query.MunicipalityId.HasValue;
    }
}
