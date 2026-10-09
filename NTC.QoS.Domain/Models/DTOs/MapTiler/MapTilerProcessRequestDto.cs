using System.Text.Json.Serialization;

namespace NTC.QoS.Domain.Models.DTOs.MapTiler;

/// <summary>
/// Payload to trigger processing on MapTiler after binary upload completes
/// </summary>
public class MapTilerProcessRequestDto
{
    [JsonPropertyName("upload_result")]
    public object? UploadResult { get; set; }
}
