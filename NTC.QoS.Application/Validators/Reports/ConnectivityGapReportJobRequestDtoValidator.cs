using FluentValidation;
using NTC.QoS.Domain.Models.DTOs.Reports;

namespace NTC.QoS.Application.Validators.Reports;

/// <summary>
/// Validator for connectivity gap report job requests.
/// Ensures at least one geographic filter is provided to scope the report.
/// </summary>
public class ConnectivityGapReportJobRequestDtoValidator : AbstractValidator<ConnectivityGapReportJobRequestDto>
{
    public ConnectivityGapReportJobRequestDtoValidator()
    {
        RuleFor(x => x.JobId)
            .NotEmpty()
            .WithMessage("JobId is required.");

        RuleFor(x => x.Format)
            .IsInEnum()
            .WithMessage("Invalid report format.");

        RuleFor(x => x.ReportTitle)
            .MaximumLength(200)
            .When(x => !string.IsNullOrEmpty(x.ReportTitle))
            .WithMessage("Report title cannot exceed 200 characters.");

        RuleFor(x => x)
            .Must(HaveAtLeastOneGeographicFilter)
            .WithMessage("At least one geographic filter (RegionId, ProvinceId, or MunicipalityId) must be provided.");
    }

    private bool HaveAtLeastOneGeographicFilter(ConnectivityGapReportJobRequestDto dto)
    {
        return dto.RegionId.HasValue || dto.ProvinceId.HasValue || dto.MunicipalityId.HasValue;
    }
}
