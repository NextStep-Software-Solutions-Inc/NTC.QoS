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

    [JsonPropertyName("progress")]
    public int Progress { get; set; }

    [JsonPropertyName("errors")]
    public object? Errors { get; set; }
}
