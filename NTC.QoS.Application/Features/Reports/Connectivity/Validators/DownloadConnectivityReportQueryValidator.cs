using FluentValidation;
using NTC.QoS.Application.Features.Reports.Connectivity.Requests;

namespace NTC.QoS.Application.Features.Reports.Connectivity.Validators;

public class DownloadConnectivityReportQueryValidator : AbstractValidator<DownloadConnectivityReportQuery>
{
    public DownloadConnectivityReportQueryValidator()
    {
        // MANDATORY: Require at least one geographic filter to prevent unscoped queries
        RuleFor(x => x)
            .Must(HaveAtLeastOneGeographicFilter)
            .WithMessage("At least one geographic filter (RegionId, ProvinceId, or MunicipalityId) is required to generate the report. This prevents performance issues from processing all locations.")
            .WithName("GeographicFilters");

        // Validate Format is a valid enum value
        RuleFor(x => x.Format)
            .IsInEnum()
            .WithMessage("Format must be either Excel (0) or Pdf (1).");

        // Validate ReportTitle length if provided
        RuleFor(x => x.ReportTitle)
            .MaximumLength(200)
            .WithMessage("Report title cannot exceed 200 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.ReportTitle));

        // Note: Geographic filter precedence is handled in the query handler.
        // Lower admin levels take precedence: Municipality > Province > Region
    }

    /// <summary>
    /// Validates that at least one geographic filter is provided to prevent unscoped queries
    /// </summary>
    private static bool HaveAtLeastOneGeographicFilter(DownloadConnectivityReportQuery query)
    {
        return query.RegionId.HasValue || 
               query.ProvinceId.HasValue || 
               query.MunicipalityId.HasValue;
    }
}
