using System.Net;
using System.Text;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NTC.QoS.API.Controllers;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Infrastructure.Configuration;
using NTC.QoS.Infrastructure.Services;
using Xunit;

namespace NTC.QoS.Tests.API.Controllers;

public class CellularCoveragesProxyUploadIntegrationTests
{
    private const string ValidUploadUrl = "https://upload.googleapis.com/upload/storage/v1/b/maptiler/o?uploadType=resumable&upload_id=test-123";

    private static CellularCoveragesController CreateController(
        HttpMessageHandler upstreamHandler,
        Stream requestBody,
        long? contentLength)
    {
        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        httpClientFactoryMock
            .Setup(f => f.CreateClient(MapTilerIngestService.UploadHttpClientName))
            .Returns(() => new HttpClient(upstreamHandler, disposeHandler: false));

        var ingestService = new MapTilerIngestService(
            new HttpClient(),
            Options.Create(new MapTilerOptions { UploadTimeoutMinutes = 30 }),
            Mock.Of<ILogger<MapTilerIngestService>>(),
            httpClientFactoryMock.Object);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Body = requestBody;
        httpContext.Request.ContentLength = contentLength;

        return new CellularCoveragesController(
            Mock.Of<IMediator>(),
            new MemoryCache(new MemoryCacheOptions()),
            ingestService)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };
    }

    [Fact]
    public async Task UploadTilesetBinary_StreamsFullPayloadToUpstream_AndReturnsOk()
    {
        // Arrange
        var payload = Encoding.UTF8.GetBytes(new string('A', 64 * 1024));
        byte[]? capturedBytes = null;
        HttpMethod? capturedMethod = null;
        string? capturedContentType = null;
        long? capturedContentLength = null;

        var handler = new StubHttpMessageHandler(async (request, ct) =>
        {
            capturedMethod = request.Method;
            capturedContentType = request.Content?.Headers.ContentType?.MediaType;
            capturedContentLength = request.Content?.Headers.ContentLength;
            capturedBytes = await request.Content!.ReadAsByteArrayAsync(ct);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        using var bodyStream = new MemoryStream(payload);
        var controller = CreateController(handler, bodyStream, payload.Length);

        // Act
        var result = await controller.UploadTilesetBinary(Guid.NewGuid(), ValidUploadUrl, CancellationToken.None);

        // Assert
        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = ok.Value.Should().BeOfType<ApiResponse<bool>>().Subject;
        apiResponse.Succeeded.Should().BeTrue();
        apiResponse.Data.Should().BeTrue();

        capturedMethod.Should().Be(HttpMethod.Put);
        capturedContentType.Should().Be("application/octet-stream");
        capturedContentLength.Should().Be(payload.Length);
        capturedBytes.Should().Equal(payload);
    }

    [Fact]
    public async Task UploadTilesetBinary_WhenClientDropsConnectionMidStream_Returns502()
    {
        // Arrange: stream yields first 8 KB then throws IOException simulating an abrupt client disconnect
        var partialChunk = new byte[8 * 1024];
        Random.Shared.NextBytes(partialChunk);
        const long declaredLength = 64 * 1024;

        var handler = new StubHttpMessageHandler(async (request, ct) =>
        {
            await request.Content!.ReadAsByteArrayAsync(ct);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        using var droppingStream = new DroppingStream(partialChunk, new IOException("Client connection reset by peer."));
        var controller = CreateController(handler, droppingStream, declaredLength);

        // Act
        var result = await controller.UploadTilesetBinary(Guid.NewGuid(), ValidUploadUrl, CancellationToken.None);

        // Assert
        var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(502);
        var apiResponse = statusResult.Value.Should().BeOfType<ApiResponse<bool>>().Subject;
        apiResponse.Succeeded.Should().BeFalse();
        apiResponse.Data.Should().BeFalse();
    }

    [Fact]
    public async Task UploadTilesetBinary_WhenStreamEndsEarlyPartialUpload_Returns502EvenIfUpstreamReturnsOk()
    {
        // Arrange: client declares 32 KB Content-Length but stream reaches EOF after only 12 KB
        var truncatedBytes = new byte[12 * 1024];
        Random.Shared.NextBytes(truncatedBytes);
        const long declaredLength = 32 * 1024;

        var handler = new StubHttpMessageHandler(async (request, ct) =>
        {
            // Simulate an upstream endpoint that reads until EOF and returns 200 without enforcing Content-Length
            await request.Content!.ReadAsByteArrayAsync(ct);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        using var truncatedStream = new MemoryStream(truncatedBytes);
        var controller = CreateController(handler, truncatedStream, declaredLength);

        // Act
        var result = await controller.UploadTilesetBinary(Guid.NewGuid(), ValidUploadUrl, CancellationToken.None);

        // Assert
        var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(502);
        var apiResponse = statusResult.Value.Should().BeOfType<ApiResponse<bool>>().Subject;
        apiResponse.Succeeded.Should().BeFalse();
        apiResponse.Data.Should().BeFalse();
    }

    [Fact]
    public async Task UploadTilesetBinary_WhenClientCancelsMidUpload_Returns499()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var chunk = new byte[4 * 1024];
        const long declaredLength = 64 * 1024;

        var handler = new StubHttpMessageHandler(async (request, ct) =>
        {
            await request.Content!.ReadAsByteArrayAsync(ct);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        using var cancellingStream = new CancellingStream(chunk, cts);
        var controller = CreateController(handler, cancellingStream, declaredLength);

        // Act
        var result = await controller.UploadTilesetBinary(Guid.NewGuid(), ValidUploadUrl, cts.Token);

        // Assert
        var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(499);
        var apiResponse = statusResult.Value.Should().BeOfType<ApiResponse<bool>>().Subject;
        apiResponse.Succeeded.Should().BeFalse();
        apiResponse.Data.Should().BeFalse();
    }

    [Fact]
    public async Task UploadTilesetBinary_WhenUpstreamDropsConnectionOrReturns503_Returns502()
    {
        // Arrange
        var payload = Encoding.UTF8.GetBytes("valid-mbtiles-content");

        var errorStatusHandler = new StubHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
            {
                Content = new StringContent("Upstream storage temporarily unavailable")
            }));

        using var stream1 = new MemoryStream(payload);
        var controller1 = CreateController(errorStatusHandler, stream1, payload.Length);

        var networkDropHandler = new StubHttpMessageHandler((_, _) =>
            throw new HttpRequestException("Upstream connection closed unexpectedly"));

        using var stream2 = new MemoryStream(payload);
        var controller2 = CreateController(networkDropHandler, stream2, payload.Length);

        // Act
        var result1 = await controller1.UploadTilesetBinary(Guid.NewGuid(), ValidUploadUrl, CancellationToken.None);
        var result2 = await controller2.UploadTilesetBinary(Guid.NewGuid(), ValidUploadUrl, CancellationToken.None);

        // Assert
        result1.Result.Should().BeOfType<ObjectResult>().Subject.StatusCode.Should().Be(502);
        result2.Result.Should().BeOfType<ObjectResult>().Subject.StatusCode.Should().Be(502);
    }

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => handler(request, cancellationToken);
    }

    private sealed class DroppingStream(byte[] initialData, Exception errorToThrow) : Stream
    {
        private bool _sentInitial;

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() { }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (!_sentInitial)
            {
                _sentInitial = true;
                var toCopy = Math.Min(count, initialData.Length);
                Buffer.BlockCopy(initialData, 0, buffer, offset, toCopy);
                return toCopy;
            }

            throw errorToThrow;
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (!_sentInitial)
            {
                _sentInitial = true;
                var toCopy = Math.Min(buffer.Length, initialData.Length);
                initialData.AsMemory(0, toCopy).CopyTo(buffer);
                return ValueTask.FromResult(toCopy);
            }

            return ValueTask.FromException<int>(errorToThrow);
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    private sealed class CancellingStream(byte[] initialData, CancellationTokenSource cts) : Stream
    {
        private bool _sentInitial;

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() { }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (!_sentInitial)
            {
                _sentInitial = true;
                var toCopy = Math.Min(count, initialData.Length);
                Buffer.BlockCopy(initialData, 0, buffer, offset, toCopy);
                return toCopy;
            }

            cts.Cancel();
            cts.Token.ThrowIfCancellationRequested();
            return 0;
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (!_sentInitial)
            {
                _sentInitial = true;
                var toCopy = Math.Min(buffer.Length, initialData.Length);
                initialData.AsMemory(0, toCopy).CopyTo(buffer);
                return ValueTask.FromResult(toCopy);
            }

            cts.Cancel();
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromCanceled<int>(cts.Token);
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
