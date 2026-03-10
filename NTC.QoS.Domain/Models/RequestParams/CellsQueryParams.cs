using System.Text.Json.Serialization;

namespace NTC.QoS.Domain.Models.RequestParams;

/// <summary>
/// Cell-specific query parameters that extend shared location filters.
/// Includes text/status, provider/service, cellular generation/technology/band,
/// and verification filters used by the Cells endpoints.
/// </summary>
public sealed class CellsQueryParams : LocationQueryParams
{
    [JsonPropertyName("search")]
    public string? Search { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

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

    [JsonPropertyName("isVerified")]
    public bool? IsVerified { get; set; }
}
