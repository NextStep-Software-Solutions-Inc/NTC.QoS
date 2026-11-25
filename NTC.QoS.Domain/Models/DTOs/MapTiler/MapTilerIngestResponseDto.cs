using System.Text.Json.Serialization;

namespace NTC.QoS.Domain.Models.DTOs.MapTiler;

/// <summary>
/// MapTiler Service API response for dataset ingest operations
/// </summary>
public class MapTilerIngestResponseDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("document_id")]
    public string? DocumentId { get; set; }

    [JsonPropertyName("state")]
    public string State { get; set; } = string.Empty;

    [JsonPropertyName("filename")]
    public string Filename { get; set; } = string.Empty;

    [JsonPropertyName("size")]
    public long Size { get; set; }

    [JsonPropertyName("upload_url")]
    public string? UploadUrl { get; set; }

    [JsonPropertyName("upload")]
    public MapTilerUploadInfoDto? Upload { get; set; }

    [JsonIgnore]
    public string? EffectiveUploadUrl =>
        !string.IsNullOrWhiteSpace(UploadUrl)
            ? UploadUrl
            : !string.IsNullOrWhiteSpace(Upload?.Url)
                ? Upload.Url
                : Upload?.Parts?.FirstOrDefault()?.Url;

    [JsonPropertyName("progress")]
    public double Progress { get; set; }

    [JsonPropertyName("errors")]
    public object? Errors { get; set; }
}

public class MapTilerUploadInfoDto
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("part_size")]
    public long? PartSize { get; set; }

    [JsonPropertyName("parts")]
    public List<MapTilerUploadPartDto>? Parts { get; set; }
}

public class MapTilerUploadPartDto
{
    [JsonPropertyName("part_id")]
    public int PartId { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }
}
