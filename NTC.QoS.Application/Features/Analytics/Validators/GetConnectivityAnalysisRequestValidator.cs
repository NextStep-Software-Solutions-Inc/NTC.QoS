using FluentValidation;
using NTC.QoS.Application.Features.Analytics.Requests;
using NTC.QoS.Domain.Enums;

namespace NTC.QoS.Application.Features.Analytics.Validators;

public class GetConnectivityAnalysisRequestValidator : AbstractValidator<GetConnectivityAnalysisRequest>
{
    public GetConnectivityAnalysisRequestValidator()
    {
        RuleFor(x => x.LocationIds)
            .NotNull()
            .WithMessage("LocationIds is required.")
            .NotEmpty()
            .WithMessage("At least one LocationId must be provided.")
            .Must(ids => ids.Count <= 100)
            .WithMessage("Cannot process more than 100 locations at once for performance reasons.")
            .Must(ids => ids.All(id => id != Guid.Empty))
            .WithMessage("All LocationIds must be valid (non-empty) GUIDs.");

        RuleFor(x => x.LocationType)
            .IsInEnum()
            .WithMessage("LocationType must be a valid enum value (Barangay, Municipality, Provincial, or Regional).");

        RuleFor(x => x.FilterByStatus)
            .IsInEnum()
            .When(x => x.FilterByStatus.HasValue)
            .WithMessage("FilterByStatus must be a valid ConnectivityStatusEnum value (Served, Underserved, or Unserved) when provided.");

        // Conditional validation for meaningful combinations
        RuleFor(x => x)
            .Must(x => x.IncludeSpeedTestData || x.IncludeInfrastructureData)
            .WithMessage("At least one of IncludeSpeedTestData or IncludeInfrastructureData must be true to return meaningful data.");
    }
}