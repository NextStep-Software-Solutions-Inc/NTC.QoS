using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.MapTiler;
using NTC.QoS.Infrastructure.Configuration;

namespace NTC.QoS.Infrastructure.Services;

/// <summary>
/// Implementation of IMapTilerIngestService interacting with MapTiler Cloud Service / Ingest API
/// </summary>
public class MapTilerIngestService : IMapTilerIngestService
{
    private readonly HttpClient _httpClient;
    private readonly MapTilerOptions _options;
    private readonly ILogger<MapTilerIngestService> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public MapTilerIngestService(
        HttpClient httpClient,
        IOptions<MapTilerOptions> options,
        ILogger<MapTilerIngestService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<MapTilerIngestResponseDto?> InitIngestAsync(
        string fileName,
        long fileSize,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = new MapTilerIngestRequestDto
            {
                Filename = fileName,
                Size = fileSize,
                SupportedUploadTypes = ["google_drive_resumable", "s3_multipart"],
                Output = new MapTilerIngestOutputDto { Type = "vector_tileset" }
            };

            var request = new HttpRequestMessage(HttpMethod.Post, "datasets/ingest")
            {
                Content = JsonContent.Create(payload)
            };
            ApplyAuthorization(request);

            _logger.LogInformation("Initiating MapTiler ingest for file {FileName} ({Size} bytes)", fileName, fileSize);
            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("MapTiler InitIngest failed with status {StatusCode}: {Error}", response.StatusCode, errorContent);
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<MapTilerIngestResponseDto>(JsonOptions, cancellationToken);
            _logger.LogInformation("MapTiler Ingest initiated successfully. IngestId: {IngestId}", result?.Id);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while initiating MapTiler ingest for {FileName}", fileName);
            return null;
        }
    }

    public async Task<MapTilerIngestResponseDto?> ProcessIngestAsync(
        string ingestId,
        object? uploadResult = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            object payload = uploadResult != null
                ? new { upload_result = uploadResult }
                : new { upload_result = (object)new { type = "google_drive_resumable" } };

            var request = new HttpRequestMessage(HttpMethod.Post, $"datasets/ingest/{ingestId}/process")
            {
                Content = JsonContent.Create(payload)
            };
            ApplyAuthorization(request);

            _logger.LogInformation("Triggering MapTiler ingest processing for IngestId {IngestId}", ingestId);
            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("MapTiler ProcessIngest failed for {IngestId} with status {StatusCode}: {Error}", ingestId, response.StatusCode, errorContent);
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<MapTilerIngestResponseDto>(JsonOptions, cancellationToken);
            _logger.LogInformation("MapTiler Ingest processing started. IngestId: {IngestId}, State: {State}", ingestId, result?.State);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while processing MapTiler ingest for {IngestId}", ingestId);
            return null;
        }
    }

    public async Task<MapTilerIngestResponseDto?> GetIngestDetailsAsync(
        string ingestId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"datasets/ingest/{ingestId}");
            ApplyAuthorization(request);

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("MapTiler GetIngestDetails failed for {IngestId} with status {StatusCode}: {Error}", ingestId, response.StatusCode, errorContent);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<MapTilerIngestResponseDto>(JsonOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception getting MapTiler ingest details for {IngestId}", ingestId);
            return null;
        }
    }

    public async Task<bool> CancelIngestAsync(
        string ingestId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"datasets/ingest/{ingestId}/cancel");
            ApplyAuthorization(request);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception cancelling MapTiler ingest for {IngestId}", ingestId);
            return false;
        }
    }

    public string GetTileJsonUrl(string documentId)
    {
        var apiKey = !string.IsNullOrWhiteSpace(_options.ApiKey) ? _options.ApiKey : "YOUR_MAPTILER_API_KEY";
        return $"https://api.maptiler.com/tiles/{documentId}/tiles.json?key={apiKey}";
    }

    private void ApplyAuthorization(HttpRequestMessage request)
    {
        if (!string.IsNullOrWhiteSpace(_options.ServiceToken))
        {
            // MapTiler uses "Token <token>" scheme for Service API authentication
            request.Headers.Authorization = new AuthenticationHeaderValue("Token", _options.ServiceToken);
        }
        else
        {
            _logger.LogWarning("MapTiler ServiceToken is not configured. Service API requests may fail authentication.");
        }
    }
}
