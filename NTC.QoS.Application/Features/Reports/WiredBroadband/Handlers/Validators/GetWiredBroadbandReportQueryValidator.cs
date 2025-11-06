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

        // Logical validation: if municipality is specified, province and region should not conflict
        When(x => x.MunicipalityId.HasValue, () => {
            RuleFor(x => x.ProvinceId)
                .Empty()
                .WithMessage("When Municipality ID is specified, Province ID should not be provided as it will be inferred.");
            
            RuleFor(x => x.RegionId)
                .Empty()
                .WithMessage("When Municipality ID is specified, Region ID should not be provided as it will be inferred.");
        });

        // Logical validation: if province is specified, region should not conflict  
        When(x => x.ProvinceId.HasValue && !x.MunicipalityId.HasValue, () => {
            RuleFor(x => x.RegionId)
                .Empty()
                .WithMessage("When Province ID is specified, Region ID should not be provided as it will be inferred.");
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