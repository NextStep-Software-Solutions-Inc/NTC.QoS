using FluentValidation;
using NTC.QoS.Application.Features.Analytics.Requests;
using NTC.QoS.Domain.Enums;

namespace NTC.QoS.Application.Features.Analytics.Validators;

public class GenerateConnectivityAssessmentRequestValidator : AbstractValidator<GenerateConnectivityAssessmentRequest>
{
    private const int MAX_LOCATIONS_PER_REQUEST = 50;
    private const int MAX_LOCATIONS_WITH_FORCE_RECALCULATE = 20;
    private const int MAX_REGIONAL_LOCATIONS = 5;

    public GenerateConnectivityAssessmentRequestValidator()
    {
        // Location IDs validation
        RuleFor(x => x.LocationIds)
            .NotNull()
            .WithMessage("LocationIds is required.")
            .NotEmpty()
            .WithMessage("At least one LocationId must be provided.")
            .Must(ids => ids.Count <= MAX_LOCATIONS_PER_REQUEST)
            .WithMessage($"Cannot generate assessments for more than {MAX_LOCATIONS_PER_REQUEST} locations at once due to processing complexity.")
            .Must(ids => ids.All(id => id != Guid.Empty))
            .WithMessage("All LocationIds must be valid (non-empty) GUIDs.")
            .Must(ids => ids.Distinct().Count() == ids.Count)
            .WithMessage("Duplicate LocationIds are not allowed.");

        // Location type validation
        RuleFor(x => x.LocationType)
            .IsInEnum()
            .WithMessage("LocationType must be a valid enum value (Barangay, Municipality, Provincial, or Regional).");

        // Business rule validations
        RuleFor(x => x)
            .Must(x => !x.ForceRecalculate || x.LocationIds.Count <= MAX_LOCATIONS_WITH_FORCE_RECALCULATE)
            .WithMessage($"When ForceRecalculate is true, cannot process more than {MAX_LOCATIONS_WITH_FORCE_RECALCULATE} locations at once due to computational intensity.")

            .Must(x => x.LocationType != LocationTypeEnum.Regional || x.LocationIds.Count <= MAX_REGIONAL_LOCATIONS)
            .WithMessage($"Regional-level assessment generation is limited to {MAX_REGIONAL_LOCATIONS} locations at once due to data volume.");
    }
}