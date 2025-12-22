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
        // MANDATORY: Require at least one geographic filter to prevent unscoped queries
        RuleFor(x => x)
            .Must(HaveAtLeastOneGeographicFilter)
            .WithMessage("At least one geographic filter (RegionId, ProvinceId, or MunicipalityId) is required to generate the report. This prevents performance issues from processing all municipalities.")
            .WithName("GeographicFilters");

        // Note: Geographic filter precedence is handled in the query handler.
        // Lower admin levels take precedence: Municipality > Province > Region
        // Multiple filters can be provided, but the most specific will be used.

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
    private static bool HaveAtLeastOneGeographicFilter(GetWirelessBroadbandScoredReportQuery query)
    {
        return query.RegionId.HasValue ||
               query.ProvinceId.HasValue ||
               query.MunicipalityId.HasValue;
    }
}