using System.Text.Json.Serialization;

namespace NTC.QoS.Domain.Models.RequestParams;

/// <summary>
/// Query parameters for location-based entity queries with geographic filtering.
/// Supports pagination, distance-based searches, and filtering by geographic regions.
/// </summary>
public sealed class LocationQueryParams
{
    [JsonPropertyName("pageNumber")]
    public int PageNumber { get; set; } = 1;

    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; } = 10;

    [JsonPropertyName("search")]
    public string? Search { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("serviceProviderIds")]
    public List<Guid>? ServiceProviderIds { get; set; }

    [JsonPropertyName("providerIds")]
    public List<Guid>? ProviderIds { get; set; }

    [JsonPropertyName("serviceIds")]
    public List<Guid>? ServiceIds { get; set; }

    [JsonPropertyName("generationIds")]
    public List<Guid>? GenerationIds { get; set; }

    [JsonPropertyName("technologyIds")]
    public List<Guid>? TechnologyIds { get; set; }

    [JsonPropertyName("bandIds")]
    public List<Guid>? BandIds { get; set; }

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

    [JsonPropertyName("isVerified")]
    public bool? IsVerified { get; set; }
}