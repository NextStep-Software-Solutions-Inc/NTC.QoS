using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NTC.QoS.Infrastructure.Configuration;
using NTC.QoS.Infrastructure.Services;
using Xunit;

namespace NTC.QoS.Tests.Infrastructure.Services;

public class MapTilerIngestServiceTests
{
    private static MapTilerIngestService CreateService(
        HttpMessageHandler handler,
        MapTilerOptions? options = null)
    {
        var httpClient = new HttpClient(handler, disposeHandler: false)
        {
            BaseAddress = new Uri("https://service.maptiler.com/v1/")
        };

        var effectiveOptions = options ?? new MapTilerOptions
        {
            ServiceToken = "svc-token-123",
            ApiKey = "pub-key-456"
        };

        return new MapTilerIngestService(
            httpClient,
            Options.Create(effectiveOptions),
            Mock.Of<ILogger<MapTilerIngestService>>(),
            httpClientFactory: null,
            retryBaseDelay: TimeSpan.Zero);
    }

    [Fact]
    public async Task GetIngestDetailsAsync_RecoversFromIntermittentNetworkAnd503Errors()
    {
        var attempts = 0;
        var handler = new SequenceHttpMessageHandler((_, _) =>
        {
            attempts++;
            return attempts switch
            {
                1 => throw new HttpRequestException("Transient DNS reset"),
                2 => Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)),
                _ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        """{"id":"ing-1","state":"completed","progress":100,"document_id":"doc-99"}""",
                        Encoding.UTF8,
                        "application/json")
                })
            };
        });

        var service = CreateService(handler);

        var result = await service.GetIngestDetailsAsync("ing-1");

        attempts.Should().Be(3);
        result.Should().NotBeNull();
        result!.State.Should().Be("completed");
        result.DocumentId.Should().Be("doc-99");
    }

    [Fact]
    public async Task GetIngestDetailsAsync_ReturnsNullAfterExhaustingRetriesOnPersistentNetworkFailure()
    {
        var attempts = 0;
        var handler = new SequenceHttpMessageHandler((_, _) =>
        {
            attempts++;
            throw new HttpRequestException("Network unreachable");
        });

        var service = CreateService(handler);

        var result = await service.GetIngestDetailsAsync("ing-down");

        attempts.Should().Be(3); // 1 initial + 2 retries
        result.Should().BeNull();
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.BadRequest)]
    public async Task GetIngestDetailsAsync_DoesNotRetryNonTransientClientErrors(HttpStatusCode statusCode)
    {
        var attempts = 0;
        var handler = new SequenceHttpMessageHandler((_, _) =>
        {
            attempts++;
            return Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent("client error")
            });
        });

        var service = CreateService(handler);

        var result = await service.GetIngestDetailsAsync("ing-client-err");

        attempts.Should().Be(1);
        result.Should().BeNull();
    }

    [Fact]
    public async Task InitIngestAsync_RetriesTransient503_SetsAuthHeader_AndFallsBackToDefaultDocumentId()
    {
        var attempts = 0;
        string? capturedAuth = null;
        string? capturedPath = null;

        var handler = new SequenceHttpMessageHandler((request, _) =>
        {
            attempts++;
            capturedAuth = request.Headers.Authorization?.ToString();
            capturedPath = request.RequestUri?.AbsolutePath;

            if (attempts == 1)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"id":"ing-init","document_id":"00000000-0000-0000-0000-000000000000","upload":{"url":"https://upload.googleapis.com/resumable/xyz"}}""",
                    Encoding.UTF8,
                    "application/json")
            });
        });

        var service = CreateService(handler, new MapTilerOptions
        {
            ServiceToken = "svc-token-xyz",
            DefaultDocumentId = "existing-doc-id"
        });

        var result = await service.InitIngestAsync("caraga.mbtiles", 1024);

        attempts.Should().Be(2);
        capturedAuth.Should().Be("Token svc-token-xyz");
        capturedPath.Should().EndWith("/datasets/existing-doc-id/ingest");
        result.Should().NotBeNull();
        result!.UploadUrl.Should().Be("https://upload.googleapis.com/resumable/xyz");
        result.DocumentId.Should().Be("existing-doc-id");
    }

    [Fact]
    public async Task ProcessIngestAsync_RecoversFromHttpTimeout_AndSendsEmptyBodyWhenUploadResultIsNull()
    {
        var attempts = 0;
        HttpContent? capturedContent = null;

        var handler = new SequenceHttpMessageHandler((request, _) =>
        {
            attempts++;
            capturedContent = request.Content;

            if (attempts == 1)
            {
                throw new TaskCanceledException("Simulated HTTP client timeout");
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"id":"ing-proc","state":"processing"}""",
                    Encoding.UTF8,
                    "application/json")
            });
        });

        var service = CreateService(handler);

        var result = await service.ProcessIngestAsync("ing-proc", uploadResult: null);

        attempts.Should().Be(2);
        capturedContent.Should().BeNull();
        result.Should().NotBeNull();
        result!.State.Should().Be("processing");
    }

    [Fact]
    public async Task CancelIngestAsync_RetriesTransient500AndReturnsTrueOnRecovery()
    {
        var attempts = 0;
        var handler = new SequenceHttpMessageHandler((_, _) =>
        {
            attempts++;
            return Task.FromResult(attempts == 1
                ? new HttpResponseMessage(HttpStatusCode.InternalServerError)
                : new HttpResponseMessage(HttpStatusCode.OK));
        });

        var service = CreateService(handler);

        var cancelled = await service.CancelIngestAsync("ing-cancel");

        attempts.Should().Be(2);
        cancelled.Should().BeTrue();
    }

    [Fact]
    public async Task GetIngestDetailsAsync_DoesNotRetryWhenCallerCancellationTokenIsCancelled()
    {
        var attempts = 0;
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var handler = new SequenceHttpMessageHandler((_, ct) =>
        {
            attempts++;
            ct.ThrowIfCancellationRequested();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });

        var service = CreateService(handler);

        var result = await service.GetIngestDetailsAsync("ing-cancelled", cts.Token);

        attempts.Should().Be(1);
        result.Should().BeNull();
    }

    private sealed class SequenceHttpMessageHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => handler(request, cancellationToken);
    }
}
