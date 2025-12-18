using NTC.QoS.Domain.Enums;

namespace NTC.QoS.Domain.Models.DTOs.Analytics;

/// <summary>
/// Request object for background connectivity assessment jobs
/// Contains all necessary information to process connectivity assessments
/// </summary>
public class ConnectivityAssessmentJobRequestDto
{
    /// <summary>
    /// Unique identifier for this job (Hangfire Job ID)
    /// </summary>
    public string JobId { get; set; } = string.Empty;

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

    /// <summary>
    /// DEPRECATED: Use typed properties instead (RegionIds, ProvinceIds, etc.)
    /// Kept for backward compatibility - will be removed in future version
    /// </summary>
    [Obsolete("Use typed location ID properties (RegionIds, ProvinceIds, MunicipalityIds, BarangayIds) instead")]
    public List<Guid> LocationIds { get; set; } = [];

    /// <summary>
    /// DEPRECATED: Use GetLocationType() method instead
    /// Kept for backward compatibility - will be removed in future version
    /// </summary>
    [Obsolete("Use GetLocationType() method instead")]
    public LocationTypeEnum LocationType { get; set; }

    /// <summary>
    /// Whether to force recalculation of existing assessments
    /// </summary>
    public bool ForceRecalculate { get; set; } = false;

    /// <summary>
    /// Whether to generate assessments for child locations hierarchically
    /// </summary>
    public bool GenerateHierarchicalAssessments { get; set; } = false;

    /// <summary>
    /// Depth of hierarchical processing (1-3 levels)
    /// </summary>
    public int HierarchicalDepth { get; set; } = 3;

    /// <summary>
    /// Maximum number of hierarchical locations to process (optional performance limit).
    /// NULL = No limit, process all child locations (ensures complete coverage)
    /// VALUE = Cap processing at this number (may result in incomplete assessments)
    /// </summary>
    public int? MaxHierarchicalLocations { get; set; } = null;

    /// <summary>
    /// NEW: Batch size for hierarchical processing (smaller than primary processing)
    /// </summary>
    public int HierarchicalBatchSize { get; set; } = 50;

    /// <summary>
    /// NEW: Actual total number of locations to be processed (primary + hierarchical children)
    /// Calculated by StartConnectivityAssessmentJobHandler based on database counts
    /// Used for accurate progress tracking throughout job execution
    /// </summary>
    public int ActualTotalLocations { get; set; }

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
    /// User ID who requested the job
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// When this job was requested
    /// </summary>
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Maximum number of locations to process in parallel
    /// </summary>
    public int MaxParallelism { get; set; } = 5;

    /// <summary>
    /// Get the location type based on which typed property is populated
    /// </summary>
    public LocationTypeEnum GetLocationType()
    {
        if (BarangayIds?.Any() == true) return LocationTypeEnum.Barangay;
        if (MunicipalityIds?.Any() == true) return LocationTypeEnum.Municipality;
        if (ProvinceIds?.Any() == true) return LocationTypeEnum.Provincial;
        if (RegionIds?.Any() == true) return LocationTypeEnum.Regional;
        
        // Fallback to deprecated property
        #pragma warning disable CS0618 // Type or member is obsolete
        return LocationType;
        #pragma warning restore CS0618
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
        
        // Fallback to deprecated property
        #pragma warning disable CS0618 // Type or member is obsolete
        return LocationIds;
        #pragma warning restore CS0618
    }

    /// <summary>
    /// Get count of locations to process
    /// </summary>
    public int GetLocationCount()
    {
        return GetLocationIds().Count;
    }
}