using FluentValidation;
using NTC.QoS.Application.Features.Analytics.Requests;
using NTC.QoS.Domain.Enums;

namespace NTC.QoS.Application.Features.Analytics.Validators;

public class GenerateConnectivityAssessmentRequestValidator : AbstractValidator<GenerateConnectivityAssessmentRequest>
{
    private const int MAX_SAFE_PARALLELISM = 3; // Based on EF concurrency fix
    private const int MAX_LOCATIONS_PER_REQUEST = 50;
    private const int MAX_LOCATIONS_WITH_FORCE_RECALCULATE = 20;
    private const int MAX_REGIONAL_LOCATIONS = 5;
    private const int MAX_HIERARCHICAL_DEPTH = 3;

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

        // MaxParallelism validation - CRITICAL FOR EF CONCURRENCY FIX
        RuleFor(x => x.MaxParallelism)
            .GreaterThan(0)
            .WithMessage("MaxParallelism must be greater than 0.")
            .LessThanOrEqualTo(MAX_SAFE_PARALLELISM)
            .WithMessage($"MaxParallelism must not exceed {MAX_SAFE_PARALLELISM} to prevent Entity Framework concurrency issues and ensure database stability.");

        // Hierarchical depth validation
        RuleFor(x => x.HierarchicalDepth)
            .GreaterThan(0)
            .WithMessage("HierarchicalDepth must be greater than 0 when hierarchical generation is enabled.")
            .LessThanOrEqualTo(MAX_HIERARCHICAL_DEPTH)
            .WithMessage($"HierarchicalDepth cannot exceed {MAX_HIERARCHICAL_DEPTH} levels.")
            .When(x => x.GenerateHierarchicalAssessments);

        // Business rule validations
        RuleFor(x => x)
            .Must(x => !x.ForceRecalculate || x.LocationIds.Count <= MAX_LOCATIONS_WITH_FORCE_RECALCULATE)
            .WithMessage($"When ForceRecalculate is true, cannot process more than {MAX_LOCATIONS_WITH_FORCE_RECALCULATE} locations at once due to computational intensity.")
            
            .Must(x => x.LocationType != LocationTypeEnum.Regional || x.LocationIds.Count <= MAX_REGIONAL_LOCATIONS)
            .WithMessage($"Regional-level assessment generation is limited to {MAX_REGIONAL_LOCATIONS} locations at once due to data volume.")
            
            // EF Concurrency protection - stricter limits for hierarchical processing
            .Must(x => !x.GenerateHierarchicalAssessments || x.LocationIds.Count <= 10)
            .WithMessage("When GenerateHierarchicalAssessments is enabled, cannot process more than 10 locations at once to prevent database overload.")
            
            .Must(x => !x.GenerateHierarchicalAssessments || x.MaxParallelism <= 2)
            .WithMessage("When GenerateHierarchicalAssessments is enabled, MaxParallelism must not exceed 2 to ensure database stability.")
            
            // Combined high-risk scenario protection
            .Must(x => !(x.ForceRecalculate && x.GenerateHierarchicalAssessments && x.LocationIds.Count > 5))
            .WithMessage("When both ForceRecalculate and GenerateHierarchicalAssessments are enabled, cannot process more than 5 locations due to extreme computational requirements.")
            
            // Protect against excessive hierarchical processing
            .Must(x => !(x.GenerateHierarchicalAssessments && x.HierarchicalDepth >= 3 && x.LocationIds.Count > 3))
            .WithMessage("When generating full hierarchical assessments (depth >= 3), cannot process more than 3 locations to prevent system overload.");
    }
}