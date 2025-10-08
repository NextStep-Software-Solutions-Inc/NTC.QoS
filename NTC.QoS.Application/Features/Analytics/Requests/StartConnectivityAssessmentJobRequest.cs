using MediatR;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Application.Features.Analytics.Handlers.Queries;

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
    /// User ID for tracking purposes
    /// </summary>
    public string? UserId { get; set; }
}