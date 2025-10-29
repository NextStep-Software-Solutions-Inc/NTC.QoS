using MediatR;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Analytics;

namespace NTC.QoS.Application.Features.Analytics.Requests;

/// <summary>
/// Request to start a background connectivity assessment job
/// Returns immediately with job ID for tracking
/// </summary>
public class StartConnectivityAssessmentJobRequest : IRequest<ApiResponse<ConnectivityAssessmentJobResponseDto>>
{
    public List<Guid> LocationIds { get; set; } = [];
    public LocationTypeEnum LocationType { get; set; }
    public bool ForceRecalculate { get; set; } = false;
    
    /// <summary>
    /// When true, automatically generates assessments for child locations:
    /// - Region -> Provinces, Municipalities, Barangays
    /// - Province -> Municipalities, Barangays  
    /// - Municipality -> Barangays
    /// Background processing makes this much more feasible for large hierarchies.
    /// </summary>
    public bool GenerateHierarchicalAssessments { get; set; } = false;
    
    /// <summary>
    /// When hierarchical generation is enabled, this limits the depth:
    /// - 1: Only direct children (e.g., Region -> Provinces only)
    /// - 2: Two levels down (e.g., Region -> Provinces -> Municipalities)
    /// - 3: All levels down (e.g., Region -> Provinces -> Municipalities -> Barangays)
    /// Default is 3 (complete hierarchy)
    /// </summary>
    public int HierarchicalDepth { get; set; } = 3;
    
    /// <summary>
    /// NEW: Optional flag to enable hierarchical processing (default null = disabled for performance)
    /// Must be explicitly set to true to enable hierarchical processing
    /// This is separate from GenerateHierarchicalAssessments for backward compatibility
    /// </summary>
    public bool? EnableHierarchicalProcessing { get; set; } = null; // null = use default (disabled)
    
    /// <summary>
    /// NEW: Optional maximum number of hierarchical locations to process
    /// When null, uses location-type specific defaults for safety
    /// </summary>
    public int? MaxHierarchicalLocations { get; set; } = null; // null = use defaults
    
    /// <summary>
    /// NEW: Optional batch size for hierarchical processing
    /// When null, uses location-type specific defaults for performance
    /// </summary>
    public int? HierarchicalBatchSize { get; set; } = null; // null = use defaults
}