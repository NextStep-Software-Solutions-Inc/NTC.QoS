using System.Text.Json.Serialization;

namespace NTC.QoS.Domain.Models.DTOs.MapTiler;

/// <summary>
/// Payload to initiate a dataset ingest on MapTiler Service API
/// </summary>
public class MapTilerIngestRequestDto
{
    [JsonPropertyName("filename")]
    public string Filename { get; set; } = string.Empty;

    [JsonPropertyName("size")]
    public long Size { get; set; }

    [JsonPropertyName("supported_upload_types")]
    public List<string> SupportedUploadTypes { get; set; } = ["google_drive_resumable", "s3_multipart"];

    [JsonPropertyName("output")]
    public MapTilerIngestOutputDto? Output { get; set; } = new();
}

public class MapTilerIngestOutputDto
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "vector_tileset";
}
