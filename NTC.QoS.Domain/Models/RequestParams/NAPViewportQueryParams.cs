using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using NTC.QoS.Domain.Constants;
using NTC.QoS.Domain.Models.DTOs.Common;

namespace NTC.QoS.Domain.Models.RequestParams;

/// <summary>
/// Query parameters for NAP viewport-based spatial queries (v2 API).
/// NAP-specific subset of <see cref="ViewportQueryParams"/> — excludes <c>isVerified</c> and
/// <c>generationIds</c> which are Cell-specific concepts and do not apply to NAPs.
/// </summary>
public class NAPViewportQueryParams
{
    [JsonPropertyName("pageNumber")]
    [Range(1, int.MaxValue, ErrorMessage = "PageNumber must be at least 1")]
    public int PageNumber { get; set; } = 1;

    [JsonPropertyName("pageSize")]
    [Range(1, GeoSpatialConstants.MaxViewportPageSize,
        ErrorMessage = "PageSize must be between 1 and " + nameof(GeoSpatialConstants.MaxViewportPageSize))]
    public int PageSize { get; set; } = GeoSpatialConstants.RecommendedViewportPageSize;

    [JsonPropertyName("minLat")]
    [Range(-90, 90)]
    public double MinLat { get; set; }

    [JsonPropertyName("maxLat")]
    [Range(-90, 90)]
    public double MaxLat { get; set; }

    [JsonPropertyName("minLng")]
    [Range(-180, 180)]
    public double MinLng { get; set; }

    [JsonPropertyName("maxLng")]
    [Range(-180, 180)]
    public double MaxLng { get; set; }

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
    /// Validates that viewport bounds are logically correct.
    /// </summary>
    public bool IsViewportValid() => MinLat < MaxLat && MinLng < MaxLng;

    /// <summary>
    /// Creates a <see cref="ViewportBounds"/> from the query parameters.
    /// </summary>
    public ViewportBounds ToViewportBounds() => new()
    {
        MinLatitude = MinLat,
        MaxLatitude = MaxLat,
        MinLongitude = MinLng,
        MaxLongitude = MaxLng
    };
}
