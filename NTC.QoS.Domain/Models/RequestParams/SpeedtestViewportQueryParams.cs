using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace NTC.QoS.Domain.Models.RequestParams;

/// <summary>
/// Query parameters for speedtest viewport-based spatial queries (v2 API).
/// Extends ViewportQueryParams with speedtest-specific filters.
/// </summary>
public sealed class SpeedtestViewportQueryParams : ViewportQueryParams
{
    [JsonPropertyName("minDownloadSpeed")]
    [Range(0, double.MaxValue, ErrorMessage = "MinDownloadSpeed must be greater than or equal to 0")]
    public decimal? MinDownloadSpeed { get; set; }

    [JsonPropertyName("maxDownloadSpeed")]
    [Range(0, double.MaxValue, ErrorMessage = "MaxDownloadSpeed must be greater than or equal to 0")]
    public decimal? MaxDownloadSpeed { get; set; }

    [JsonPropertyName("minUploadSpeed")]
    [Range(0, double.MaxValue, ErrorMessage = "MinUploadSpeed must be greater than or equal to 0")]
    public decimal? MinUploadSpeed { get; set; }

    [JsonPropertyName("maxUploadSpeed")]
    [Range(0, double.MaxValue, ErrorMessage = "MaxUploadSpeed must be greater than or equal to 0")]
    public decimal? MaxUploadSpeed { get; set; }

    [JsonPropertyName("startDate")]
    public DateTime? StartDate { get; set; }

    [JsonPropertyName("endDate")]
    public DateTime? EndDate { get; set; }
}
