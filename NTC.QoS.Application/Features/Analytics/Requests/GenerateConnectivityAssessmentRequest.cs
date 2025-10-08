using MediatR;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Analytics;

namespace NTC.QoS.Application.Features.Analytics.Requests;

public class GenerateConnectivityAssessmentRequest : IRequest<ApiResponse<List<LocationConnectivityDto>>>
{
    public List<Guid> LocationIds { get; set; } = [];
    public LocationTypeEnum LocationType { get; set; }
    public bool ForceRecalculate { get; set; } = false;
    
    /// <summary>
    /// When true, automatically generates assessments for child locations:
    /// - Region -> Provinces, Municipalities, Barangays
    /// - Province -> Municipalities, Barangays  
    /// - Municipality -> Barangays
    /// This ensures comprehensive data availability but is resource intensive.
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
    /// Maximum number of child locations to process in parallel during hierarchical generation
    /// </summary>
    public int MaxParallelism { get; set; } = 5;
}