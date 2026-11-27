using System.Net;
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
    public const string UploadHttpClientName = "MapTilerUpload";
    private const int MaxTransientRetries = 2;

    private readonly HttpClient _httpClient;
    private readonly IHttpClientFactory? _httpClientFactory;
    private readonly MapTilerOptions _options;
    private readonly ILogger<MapTilerIngestService> _logger;
    private readonly TimeSpan _retryBaseDelay;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public MapTilerIngestService(
        HttpClient httpClient,
        IOptions<MapTilerOptions> options,
        ILogger<MapTilerIngestService> logger,
        IHttpClientFactory? httpClientFactory = null,
        TimeSpan? retryBaseDelay = null)
    {
        _httpClient = httpClient;
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
        _retryBaseDelay = retryBaseDelay ?? TimeSpan.FromMilliseconds(250);
    }

    public string? DefaultDocumentId =>
        string.IsNullOrWhiteSpace(_options.DefaultDocumentId) ? null : _options.DefaultDocumentId.Trim();

    public async Task<MapTilerIngestResponseDto?> InitIngestAsync(
        string fileName,
        long fileSize,
        string? targetDocumentId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var effectiveDocumentId = !string.IsNullOrWhiteSpace(targetDocumentId)
                ? targetDocumentId.Trim()
                : DefaultDocumentId;

            var endpoint = !string.IsNullOrWhiteSpace(effectiveDocumentId)
                ? $"datasets/{Uri.EscapeDataString(effectiveDocumentId)}/ingest"
                : "datasets/ingest";

            var payload = new MapTilerIngestRequestDto
            {
                Filename = fileName,
                Size = fileSize,
                SupportedUploadTypes = ["google_drive_resumable"],
                Output = new MapTilerIngestOutputDto { Type = "vector_tileset" }
            };

            if (!string.IsNullOrWhiteSpace(effectiveDocumentId))
            {
                _logger.LogInformation(
                    "Initiating MapTiler replace ingest for file {FileName} ({Size} bytes) targeting DocumentId {DocumentId}",
                    fileName, fileSize, effectiveDocumentId);
            }
            else
            {
                _logger.LogInformation("Initiating MapTiler ingest for file {FileName} ({Size} bytes)", fileName, fileSize);
            }

            using var response = await SendWithRetryAsync(() =>
            {
                var req = new HttpRequestMessage(HttpMethod.Post, endpoint)
                {
                    Content = JsonContent.Create(payload)
                };
                ApplyAuthorization(req);
                return req;
            }, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("MapTiler InitIngest failed with status {StatusCode}: {Error}", response.StatusCode, errorContent);
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<MapTilerIngestResponseDto>(JsonOptions, cancellationToken);
            if (result != null)
            {
                if (string.IsNullOrWhiteSpace(result.UploadUrl) && !string.IsNullOrWhiteSpace(result.Upload?.Url))
                {
                    result.UploadUrl = result.Upload.Url;
                }

                var isZeroGuid = result.DocumentId == Guid.Empty.ToString();
                if ((string.IsNullOrWhiteSpace(result.DocumentId) || isZeroGuid) && !string.IsNullOrWhiteSpace(effectiveDocumentId))
                {
                    result.DocumentId = effectiveDocumentId;
                }
            }

            _logger.LogInformation(
                "MapTiler Ingest initiated successfully. IngestId: {IngestId}, DocumentId: {DocumentId}",
                result?.Id, result?.DocumentId);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while initiating MapTiler ingest for {FileName}", fileName);
            return null;
        }
    }

    public async Task<bool> UploadBinaryAsync(
        string uploadUrl,
        Stream content,
        long? contentLength,
        CancellationToken cancellationToken = default)
    {
        HttpClient? ownedUploadClient = null;
        try
        {
            var uploadClient = _httpClientFactory?.CreateClient(UploadHttpClientName);
            if (uploadClient == null)
            {
                ownedUploadClient = new HttpClient
                {
                    Timeout = TimeSpan.FromMinutes(Math.Max(1, _options.UploadTimeoutMinutes))
                };
                uploadClient = ownedUploadClient;
            }

            await using var countingStream = new CountingReadStream(content);
            using var streamContent = new StreamContent(countingStream);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            if (contentLength.HasValue && contentLength.Value > 0)
            {
                streamContent.Headers.ContentLength = contentLength.Value;
            }

            using var request = new HttpRequestMessage(HttpMethod.Put, uploadUrl)
            {
                Content = streamContent
            };

            _logger.LogInformation("Streaming {Bytes} bytes to MapTiler upload storage", contentLength);
            using var response = await uploadClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError(
                    "MapTiler binary upload failed with status {StatusCode}: {Error}",
                    response.StatusCode, errorContent);
                return false;
            }

            if (contentLength.HasValue && contentLength.Value > 0 && countingStream.BytesRead != contentLength.Value)
            {
                _logger.LogError(
                    "MapTiler binary upload was truncated: expected {ExpectedBytes} bytes but streamed {ActualBytes} bytes",
                    contentLength.Value, countingStream.BytesRead);
                return false;
            }

            _logger.LogInformation("MapTiler binary upload succeeded with status {StatusCode} ({Bytes} bytes)", response.StatusCode, countingStream.BytesRead);
            return true;
        }
        catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "MapTiler binary upload was cancelled by the client");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while streaming binary content to MapTiler upload storage");
            return false;
        }
        finally
        {
            ownedUploadClient?.Dispose();
        }
    }

    public async Task<MapTilerIngestResponseDto?> ProcessIngestAsync(
        string ingestId,
        object? uploadResult = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Triggering MapTiler ingest processing for IngestId {IngestId}", ingestId);
            using var response = await SendWithRetryAsync(() =>
            {
                // For google_drive_resumable uploads, MapTiler expects an empty POST body.
                // Only send a JSON body when an explicit uploadResult (e.g. s3_multipart ETags) is provided.
                var req = new HttpRequestMessage(HttpMethod.Post, $"datasets/ingest/{ingestId}/process");
                if (uploadResult != null)
                {
                    req.Content = JsonContent.Create(new { upload_result = uploadResult });
                }
                ApplyAuthorization(req);
                return req;
            }, cancellationToken);

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
            using var response = await SendWithRetryAsync(() =>
            {
                var req = new HttpRequestMessage(HttpMethod.Get, $"datasets/ingest/{ingestId}");
                ApplyAuthorization(req);
                return req;
            }, cancellationToken);

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
            using var response = await SendWithRetryAsync(() =>
            {
                var req = new HttpRequestMessage(HttpMethod.Post, $"datasets/ingest/{ingestId}/cancel");
                ApplyAuthorization(req);
                return req;
            }, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception cancelling MapTiler ingest for {IngestId}", ingestId);
            return false;
        }
    }

    public string GetTileJsonUrl(string documentId, string? cacheBuster = null)
    {
        var apiKey = !string.IsNullOrWhiteSpace(_options.ApiKey) ? _options.ApiKey : "YOUR_MAPTILER_API_KEY";
        var baseUrl = $"https://api.maptiler.com/tiles/{documentId}/tiles.json?key={apiKey}";
        return !string.IsNullOrWhiteSpace(cacheBuster)
            ? $"{baseUrl}&v={Uri.EscapeDataString(cacheBuster)}"
            : baseUrl;
    }

    private async Task<HttpResponseMessage> SendWithRetryAsync(
        Func<HttpRequestMessage> requestFactory,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; ; attempt++)
        {
            using var request = requestFactory();
            try
            {
                var response = await _httpClient.SendAsync(request, cancellationToken);
                if (!IsTransientStatusCode(response.StatusCode) || attempt >= MaxTransientRetries)
                {
                    return response;
                }

                _logger.LogWarning(
                    "MapTiler API returned transient status {StatusCode} on attempt {Attempt}/{MaxAttempts}; retrying",
                    response.StatusCode, attempt + 1, MaxTransientRetries + 1);
                response.Dispose();
            }
            catch (HttpRequestException ex) when (attempt < MaxTransientRetries)
            {
                _logger.LogWarning(
                    ex,
                    "Intermittent network error calling MapTiler API on attempt {Attempt}/{MaxAttempts}; retrying",
                    attempt + 1, MaxTransientRetries + 1);
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested && attempt < MaxTransientRetries)
            {
                _logger.LogWarning(
                    ex,
                    "MapTiler API request timed out on attempt {Attempt}/{MaxAttempts}; retrying",
                    attempt + 1, MaxTransientRetries + 1);
            }

            if (_retryBaseDelay > TimeSpan.Zero)
            {
                var delay = TimeSpan.FromMilliseconds(_retryBaseDelay.TotalMilliseconds * Math.Pow(2, attempt));
                await Task.Delay(delay, cancellationToken);
            }
        }
    }

    private static bool IsTransientStatusCode(HttpStatusCode statusCode) =>
        statusCode is HttpStatusCode.RequestTimeout
            or HttpStatusCode.TooManyRequests
            or HttpStatusCode.InternalServerError
            or HttpStatusCode.BadGateway
            or HttpStatusCode.ServiceUnavailable
            or HttpStatusCode.GatewayTimeout;

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

    private sealed class CountingReadStream(Stream inner) : Stream
    {
        public long BytesRead { get; private set; }

        public override bool CanRead => inner.CanRead;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => BytesRead;
            set => throw new NotSupportedException();
        }

        public override void Flush() => inner.Flush();

        public override int Read(byte[] buffer, int offset, int count)
        {
            var read = inner.Read(buffer, offset, count);
            BytesRead += read;
            return read;
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var read = await inner.ReadAsync(buffer, offset, count, cancellationToken);
            BytesRead += read;
            return read;
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            var read = await inner.ReadAsync(buffer, cancellationToken);
            BytesRead += read;
            return read;
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
