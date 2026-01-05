using FluentValidation;
using NTC.QoS.Application.Features.Reports.WirelessBroadband.Requests;
using NTC.QoS.Domain.Enums;

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

        // Validate Format is a valid enum value
        RuleFor(x => x.Format)
            .IsInEnum()
            .WithMessage("Format must be either Excel (0) or Pdf (1).");

        // Validate FilterByStatus is only used when IncludeScores is true
        RuleFor(x => x.FilterByStatus)
            .Must((query, filterByStatus) => !filterByStatus.HasValue || query.IncludeScores)
            .WithMessage("FilterByStatus parameter can only be used when IncludeScores is true. Service status filtering requires connectivity scores to be calculated.")
            .When(x => x.FilterByStatus.HasValue);

        // Validate FilterByStatus is a valid enum value when provided
        RuleFor(x => x.FilterByStatus)
            .IsInEnum()
            .WithMessage("FilterByStatus must be Served (2), Underserved (1), or Unserved (0).")
            .When(x => x.FilterByStatus.HasValue);

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
