using FluentValidation;
using NTC.QoS.Application.Features.Analytics.Requests;
using NTC.QoS.Domain.Enums;

namespace NTC.QoS.Application.Features.Analytics.Validators;

public class GenerateConnectivityAssessmentRequestValidator : AbstractValidator<GenerateConnectivityAssessmentRequest>
{
    public GenerateConnectivityAssessmentRequestValidator()
    {
        RuleFor(x => x.LocationIds)
            .NotNull()
            .WithMessage("LocationIds is required.")
            .NotEmpty()
            .WithMessage("At least one LocationId must be provided.")
            .Must(ids => ids.Count <= 50)
            .WithMessage("Cannot generate assessments for more than 50 locations at once due to processing complexity.")
            .Must(ids => ids.All(id => id != Guid.Empty))
            .WithMessage("All LocationIds must be valid (non-empty) GUIDs.");

        RuleFor(x => x.LocationType)
            .IsInEnum()
            .WithMessage("LocationType must be a valid enum value (Barangay, Municipality, Provincial, or Regional).");

        // Business rule validation
        RuleFor(x => x)
            .Must(x => !x.ForceRecalculate || x.LocationIds.Count <= 20)
            .WithMessage("When ForceRecalculate is true, cannot process more than 20 locations at once due to computational intensity.")
            .Must(x => x.LocationType != LocationTypeEnum.Regional || x.LocationIds.Count <= 5)
            .WithMessage("Regional-level assessment generation is limited to 5 locations at once due to data volume.");
    }
}