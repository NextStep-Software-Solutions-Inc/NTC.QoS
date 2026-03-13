using System.Text.Json.Serialization;

namespace NTC.QoS.Domain.Models.RequestParams;

/// <summary>
/// Query parameters for location-based entity queries with geographic filtering.
/// Supports common pagination, service provider filtering, distance-based searches,
/// and filtering by geographic regions.
/// </summary>
public class LocationQueryParams
{
    [JsonPropertyName("pageNumber")]
    public int PageNumber { get; set; } = 1;

    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; } = 10;

    [JsonPropertyName("serviceProviderIds")]
    public List<Guid>? ServiceProviderIds { get; set; }

    [JsonPropertyName("longitude")]
    public double Longitude { get; set; } = 0;

    [JsonPropertyName("latitude")]
    public double Latitude { get; set; } = 0;

    [JsonPropertyName("distance")]
    public double Distance { get; set; } = 15000000;

    [JsonPropertyName("barangayIds")]
    public List<Guid>? BarangayIds { get; set; }

    [JsonPropertyName("municipalityIds")]
    public List<Guid>? MunicipalityIds { get; set; }

    [JsonPropertyName("provinceIds")]
    public List<Guid>? ProvinceIds { get; set; }

    [JsonPropertyName("regionIds")]
    public List<Guid>? RegionIds { get; set; }

}