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

    [JsonPropertyName("isVerified")]
    public bool? IsVerified { get; set; }

    [JsonPropertyName("minDownloadSpeed")]
    public decimal? MinDownloadSpeed { get; set; }

    [JsonPropertyName("maxDownloadSpeed")]
    public decimal? MaxDownloadSpeed { get; set; }

    [JsonPropertyName("minUploadSpeed")]
    public decimal? MinUploadSpeed { get; set; }

    [JsonPropertyName("maxUploadSpeed")]
    public decimal? MaxUploadSpeed { get; set; }

    [JsonPropertyName("startDate")]
    public DateTime? StartDate { get; set; }

    [JsonPropertyName("endDate")]
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Validates that viewport bounds are logically correct
    /// </summary>
    public bool IsViewportValid()
    {
        return MinLat < MaxLat && MinLng < MaxLng;
    }

    /// <summary>
    /// Creates a ViewportBoundsDto from the query parameters
    /// </summary>
    public ViewportBounds ToViewportBounds()
    {
        return new ViewportBounds
        {
            MinLatitude = MinLat,
            MaxLatitude = MaxLat,
            MinLongitude = MinLng,
            MaxLongitude = MaxLng
        };
    }
}
