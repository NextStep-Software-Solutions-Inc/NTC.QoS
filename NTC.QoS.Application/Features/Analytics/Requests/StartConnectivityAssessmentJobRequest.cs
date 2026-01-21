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
    /// <summary>
    /// Region IDs to process (mutually exclusive with other location types)
    /// </summary>
    public List<Guid>? RegionIds { get; set; }

    /// <summary>
    /// Province IDs to process (mutually exclusive with other location types)
    /// </summary>
    public List<Guid>? ProvinceIds { get; set; }

    /// <summary>
    /// Municipality IDs to process (mutually exclusive with other location types)
    /// </summary>
    public List<Guid>? MunicipalityIds { get; set; }

    /// <summary>
    /// Barangay IDs to process (mutually exclusive with other location types)
    /// </summary>
    public List<Guid>? BarangayIds { get; set; }

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
    /// NEW: Optional maximum number of hierarchical locations to process
    /// When null, uses location-type specific defaults for safety
    /// </summary>
    public int? MaxHierarchicalLocations { get; set; } = null; // null = use defaults

    /// <summary>
    /// NEW: Optional batch size for hierarchical processing
    /// When null, uses location-type specific defaults for performance
    /// </summary>
    public int? HierarchicalBatchSize { get; set; } = null; // null = use defaults

    /// <summary>
    /// Start date for filtering speedtest and wireless validation data (inclusive).
    /// If null, no start date filter is applied.
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// End date for filtering speedtest and wireless validation data (inclusive).
    /// If null, no end date filter is applied.
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Get the location type based on which typed property is populated
    /// </summary>
    public LocationTypeEnum GetLocationType()
    {
        if (BarangayIds?.Any() == true) return LocationTypeEnum.Barangay;
        if (MunicipalityIds?.Any() == true) return LocationTypeEnum.Municipality;
        if (ProvinceIds?.Any() == true) return LocationTypeEnum.Provincial;
        if (RegionIds?.Any() == true) return LocationTypeEnum.Regional;
        
        throw new InvalidOperationException("No location IDs provided. Please specify RegionIds, ProvinceIds, MunicipalityIds, or BarangayIds.");
    }

    /// <summary>
    /// Get all location IDs regardless of type
    /// </summary>
    public List<Guid> GetLocationIds()
    {
        if (BarangayIds?.Any() == true) return BarangayIds;
        if (MunicipalityIds?.Any() == true) return MunicipalityIds;
        if (ProvinceIds?.Any() == true) return ProvinceIds;
        if (RegionIds?.Any() == true) return RegionIds;
        
        return [];
    }

    /// <summary>
    /// Get count of locations to process
    /// </summary>
    public int GetLocationCount()
    {
        return GetLocationIds().Count;
    }
}