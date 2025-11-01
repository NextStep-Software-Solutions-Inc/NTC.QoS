using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using NTC.QoS.Domain.Constants;
using NTC.QoS.Domain.Models.DTOs.Common;

namespace NTC.QoS.Domain.Models.RequestParams;

/// <summary>
/// Query parameters for viewport-based spatial queries (v2 API).
/// OPTIMIZED FOR MILLIONS OF DATA POINTS - Uses bounding box instead of distance calculations.
/// </summary>
public sealed class ViewportQueryParams
{
    [JsonPropertyName("pageNumber")]
    [Range(1, int.MaxValue, ErrorMessage = "PageNumber must be at least 1")]
    public int PageNumber { get; set; } = 1;

    [JsonPropertyName("pageSize")]
    [Range(1, GeoSpatialConstants.MaxViewportPageSize, 
        ErrorMessage = "PageSize must be between 1 and " + nameof(GeoSpatialConstants.MaxViewportPageSize))]
    public int PageSize { get; set; } = GeoSpatialConstants.RecommendedViewportPageSize;

    [JsonPropertyName("minLatitude")]
    [Range(-90, 90)]
    public double MinLatitude { get; set; }

    [JsonPropertyName("maxLatitude")]
    [Range(-90, 90)]
    public double MaxLatitude { get; set; }

    [JsonPropertyName("minLongitude")]
    [Range(-180, 180)]
    public double MinLongitude { get; set; }

    [JsonPropertyName("maxLongitude")]
    [Range(-180, 180)]
    public double MaxLongitude { get; set; }

    [JsonPropertyName("serviceProviderIds")]
    public List<Guid>? ServiceProviderIds { get; set; }

    [JsonPropertyName("barangayIds")]
    public List<Guid>? BarangayIds { get; set; }

    [JsonPropertyName("municipalityIds")]
    public List<Guid>? MunicipalityIds { get; set; }

    [JsonPropertyName("provinceIds")]
    public List<Guid>? ProvinceIds { get; set; }

    [JsonPropertyName("regionIds")]
    public List<Guid>? RegionIds { get; set; }

    /// <summary>
    /// Gets the viewport bounds for spatial filtering
    /// </summary>
    public ViewportBoundsDto Viewport => new()
    {
        MinLatitude = MinLatitude,
        MaxLatitude = MaxLatitude,
        MinLongitude = MinLongitude,
        MaxLongitude = MaxLongitude
    };

    /// <summary>
    /// Validates that viewport bounds are logically correct
    /// </summary>
    public bool IsViewportValid()
    {
        return MinLatitude < MaxLatitude && MinLongitude < MaxLongitude;
    }
}
