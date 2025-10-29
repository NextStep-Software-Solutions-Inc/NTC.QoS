using FluentValidation;
using NTC.QoS.Application.Features.Analytics.Requests;
using NTC.QoS.Domain.Enums;

namespace NTC.QoS.Application.Features.Analytics.Validators;

/// <summary>
/// Validator for background connectivity assessment job requests
/// More lenient than synchronous processing since background jobs can handle larger loads
/// </summary>
public class StartConnectivityAssessmentJobRequestValidator : AbstractValidator<StartConnectivityAssessmentJobRequest>
{
    private const int MAX_LOCATIONS_PER_JOB = 200; // Higher limit for background processing
    private const int MAX_REGIONAL_LOCATIONS = 20; // More generous for background jobs
    private const int MAX_HIERARCHICAL_DEPTH = 3;
    private const int MAX_HIERARCHICAL_LOCATIONS = 5000; // Safety limit for hierarchical processing
    private const int MIN_BATCH_SIZE = 10;
    private const int MAX_BATCH_SIZE = 200;

    public StartConnectivityAssessmentJobRequestValidator()
    {
        // Location IDs validation
        RuleFor(x => x.LocationIds)
            .NotNull()
            .WithMessage("LocationIds is required.")
            .NotEmpty()
            .WithMessage("At least one LocationId must be provided.")
            .Must(ids => ids.Count <= MAX_LOCATIONS_PER_JOB)
            .WithMessage($"Cannot queue background job for more than {MAX_LOCATIONS_PER_JOB} locations at once.")
            .Must(ids => ids.All(id => id != Guid.Empty))
            .WithMessage("All LocationIds must be valid (non-empty) GUIDs.")
            .Must(ids => ids.Distinct().Count() == ids.Count)
            .WithMessage("Duplicate LocationIds are not allowed.");

        // Location type validation
        RuleFor(x => x.LocationType)
            .IsInEnum()
            .WithMessage("LocationType must be a valid enum value (Barangay, Municipality, Provincial, or Regional).");

        // Hierarchical depth validation
        RuleFor(x => x.HierarchicalDepth)
            .GreaterThan(0)
            .WithMessage("HierarchicalDepth must be greater than 0 when hierarchical generation is enabled.")
            .LessThanOrEqualTo(MAX_HIERARCHICAL_DEPTH)
            .WithMessage($"HierarchicalDepth cannot exceed {MAX_HIERARCHICAL_DEPTH} levels.")
            .When(x => x.GenerateHierarchicalAssessments);

        // NEW: Hierarchical processing configuration validation
        RuleFor(x => x.MaxHierarchicalLocations)
            .GreaterThan(0)
            .WithMessage("MaxHierarchicalLocations must be greater than 0 when specified.")
            .LessThanOrEqualTo(MAX_HIERARCHICAL_LOCATIONS)
            .WithMessage($"MaxHierarchicalLocations cannot exceed {MAX_HIERARCHICAL_LOCATIONS} for system stability.")
            .When(x => x.MaxHierarchicalLocations.HasValue);

        RuleFor(x => x.HierarchicalBatchSize)
            .GreaterThanOrEqualTo(MIN_BATCH_SIZE)
            .WithMessage($"HierarchicalBatchSize must be at least {MIN_BATCH_SIZE} when specified.")
            .LessThanOrEqualTo(MAX_BATCH_SIZE)
            .WithMessage($"HierarchicalBatchSize cannot exceed {MAX_BATCH_SIZE} when specified.")
            .When(x => x.HierarchicalBatchSize.HasValue);

        // Business rule validations - more lenient for background processing
        RuleFor(x => x)
            .Must(x => x.LocationType != LocationTypeEnum.Regional || x.LocationIds.Count <= MAX_REGIONAL_LOCATIONS)
            .WithMessage($"Regional-level background assessment generation is limited to {MAX_REGIONAL_LOCATIONS} locations at once due to data volume.")
            
            // Warning for hierarchical jobs without explicit enablement
            .Must(x => !x.GenerateHierarchicalAssessments || x.EnableHierarchicalProcessing == true || 
                     x.EnableHierarchicalProcessing == null) // null is acceptable (will use default)
            .WithMessage("When GenerateHierarchicalAssessments is true, consider setting EnableHierarchicalProcessing=true to actually process child locations.")
            
            // Warning for very large hierarchical jobs
            .Must(x => !x.GenerateHierarchicalAssessments || x.LocationIds.Count <= 50)
            .WithMessage("When GenerateHierarchicalAssessments is enabled, recommend limiting to 50 locations to manage processing time.")
            
            // Combined high-resource scenario guidance
            .Must(x => !(x.ForceRecalculate && x.GenerateHierarchicalAssessments && 
                        x.EnableHierarchicalProcessing == true && x.LocationIds.Count > 20))
            .WithMessage("When ForceRecalculate, GenerateHierarchicalAssessments, and EnableHierarchicalProcessing are all enabled, recommend limiting to 20 locations for reasonable processing time.")
            
            // Prevent extremely large jobs with full hierarchical processing
            .Must(x => !(x.GenerateHierarchicalAssessments && x.EnableHierarchicalProcessing == true && 
                        x.HierarchicalDepth >= 3 && x.LocationIds.Count > 10))
            .WithMessage("When enabling full hierarchical processing (depth >= 3), recommend limiting to 10 locations to prevent extremely long processing times.")
            
            // Configuration consistency check
            .Must(x => x.EnableHierarchicalProcessing != true || x.GenerateHierarchicalAssessments)
            .WithMessage("EnableHierarchicalProcessing can only be true when GenerateHierarchicalAssessments is also true.");
    }
}