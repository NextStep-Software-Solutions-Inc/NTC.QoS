using FluentValidation;
using NTC.QoS.Application.Features.Reports.WiredBroadband.Requests;

namespace NTC.QoS.Application.Features.Reports.WiredBroadband.Handlers.Validators;

public class GetWiredBroadbandReportQueryValidator : AbstractValidator<GetWiredBroadbandReportQuery>
{
    public GetWiredBroadbandReportQueryValidator()
    {
        // MANDATORY: Require at least one geographic filter to prevent unscoped queries
        RuleFor(x => x)
            .Must(HaveAtLeastOneGeographicFilter)
            .WithMessage("At least one geographic filter (RegionId, ProvinceId, or MunicipalityId) is required to generate the report. This prevents performance issues from processing all municipalities.")
            .WithName("GeographicFilters");

        // Note: Geographic filter precedence is handled in the query handler.
        // Lower admin levels take precedence: Municipality > Province > Region
        // Multiple filters can be provided, but the most specific will be used.

        // Pagination validation (only when pagination is enabled)
        When(x => !x.DisablePagination, () => {
            RuleFor(x => x.PageNumber)
                .GreaterThanOrEqualTo(1)
                .WithMessage("PageNumber must be at least 1.");
            
            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 100)
                .WithMessage("PageSize must be between 1 and 100.");
        });
    }

    /// <summary>
    /// Validates that at least one geographic filter is provided to prevent unscoped queries
    /// that could cause performance issues by processing all municipalities in the database.
    /// </summary>
    /// <param name="query">The wired broadband report query</param>
    /// <returns>True if at least one geographic filter is provided, false otherwise</returns>
    private static bool HaveAtLeastOneGeographicFilter(GetWiredBroadbandReportQuery query)
    {
        return query.RegionId.HasValue || 
               query.ProvinceId.HasValue || 
               query.MunicipalityId.HasValue;
    }
}