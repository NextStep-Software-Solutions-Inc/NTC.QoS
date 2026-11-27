using System.Text;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using NTC.QoS.API.Controllers;
using NTC.QoS.Application.Features.CellularCoverage.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.MapTiler;
using Xunit;

namespace NTC.QoS.Tests.API.Controllers;

public class CellularCoveragesControllerTilesetTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly IMemoryCache _memoryCache;
    private readonly Mock<IMapTilerIngestService> _mapTilerIngestServiceMock;
    private readonly CellularCoveragesController _controller;

    public CellularCoveragesControllerTilesetTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _mapTilerIngestServiceMock = new Mock<IMapTilerIngestService>();
        _controller = new CellularCoveragesController(
            _mediatorMock.Object,
            _memoryCache,
            _mapTilerIngestServiceMock.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    [Fact]
    public async Task InitTilesetUpload_WhenSucceeded_ReturnsOk()
    {
        // Arrange
        var requestDto = new InitTilesetIngestRequestDto
        {
            FileName = "region13.mbtiles",
            FileSize = 4096,
            DisplayName = "Region XIII"
        };
        var expected = new ApiResponse<InitTilesetIngestResultDto>
        {
            Succeeded = true,
            Data = new InitTilesetIngestResultDto
            {
                TilesetId = Guid.NewGuid(),
                IngestId = "ingest-1",
                UploadUrl = "https://upload.googleapis.com/session/1"
            }
        };

        _mediatorMock
            .Setup(m => m.Send(It.Is<InitTilesetIngestCommand>(c =>
                c.FileName == "region13.mbtiles" &&
                c.FileSize == 4096 &&
                c.DisplayName == "Region XIII"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var actionResult = await _controller.InitTilesetUpload(requestDto);

        // Assert
        var ok = actionResult.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(expected);
    }

    [Fact]
    public async Task InitTilesetUpload_WhenFailed_ReturnsBadRequest()
    {
        // Arrange
        var requestDto = new InitTilesetIngestRequestDto { FileName = "bad.mbtiles", FileSize = 10 };
        var failure = new ApiResponse<InitTilesetIngestResultDto> { Succeeded = false, Message = "Init failed" };

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<InitTilesetIngestCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(failure);

        // Act
        var actionResult = await _controller.InitTilesetUpload(requestDto);

        // Assert
        var badRequest = actionResult.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.Value.Should().BeSameAs(failure);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-valid-uri")]
    [InlineData("/relative/upload/path")]
    [InlineData("http://upload.googleapis.com/insecure")]
    [InlineData("ftp://upload.googleapis.com/file")]
    [InlineData("https://evil.example.com/upload")]
    [InlineData("https://evil-googleapis.com/upload")]
    [InlineData("https://googleapis.com/upload")]
    [InlineData("https://upload.googleapis.com@evil.com/upload")]
    [InlineData("https://user:pass@upload.googleapis.com/upload")]
    [InlineData("https://upload.googleapis.com:8443/upload")]
    public async Task UploadTilesetBinary_WhenUploadUrlInvalidOrDisallowedHost_ReturnsBadRequest(string badUrl)
    {
        // Act
        var actionResult = await _controller.UploadTilesetBinary(Guid.NewGuid(), badUrl, CancellationToken.None);

        // Assert
        var badRequest = actionResult.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var payload = badRequest.Value.Should().BeOfType<ApiResponse<bool>>().Subject;
        payload.Succeeded.Should().BeFalse();

        _mapTilerIngestServiceMock.Verify(
            s => s.UploadBinaryAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<long?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData("https://upload.googleapis.com/resumable/abc")]
    [InlineData("https://storage.maptiler.com/upload/123")]
    [InlineData("https://s3.gra.io.cloud.ovh.net/bucket/key")]
    [InlineData("https://bucket.s3.eu-central-1.amazonaws.com/key")]
    [InlineData("https://UPLOAD.GOOGLEAPIS.COM/resumable/upper")]
    public async Task UploadTilesetBinary_WhenValidAllowedHostAndUploadSucceeds_ReturnsOk(string validUrl)
    {
        // Arrange
        var bytes = Encoding.UTF8.GetBytes("mock-mbtiles-bytes");
        _controller.HttpContext.Request.Body = new MemoryStream(bytes);
        _controller.HttpContext.Request.ContentLength = bytes.Length;

        _mapTilerIngestServiceMock
            .Setup(s => s.UploadBinaryAsync(validUrl, It.IsAny<Stream>(), bytes.Length, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var actionResult = await _controller.UploadTilesetBinary(Guid.NewGuid(), validUrl, CancellationToken.None);

        // Assert
        var ok = actionResult.Result.Should().BeOfType<OkObjectResult>().Subject;
        var payload = ok.Value.Should().BeOfType<ApiResponse<bool>>().Subject;
        payload.Succeeded.Should().BeTrue();
        payload.Data.Should().BeTrue();
    }

    [Fact]
    public async Task UploadTilesetBinary_WhenUploadFails_Returns502()
    {
        // Arrange
        const string validUrl = "https://storage.maptiler.com/upload/xyz";
        _mapTilerIngestServiceMock
            .Setup(s => s.UploadBinaryAsync(validUrl, It.IsAny<Stream>(), It.IsAny<long?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var actionResult = await _controller.UploadTilesetBinary(Guid.NewGuid(), validUrl, CancellationToken.None);

        // Assert
        var statusResult = actionResult.Result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(502);
    }

    [Fact]
    public async Task ProcessTileset_WhenSucceeded_ReturnsOk()
    {
        // Arrange
        var tilesetId = Guid.NewGuid();
        var expected = new ApiResponse<bool> { Succeeded = true, Data = true };

        _mediatorMock
            .Setup(m => m.Send(It.Is<ProcessTilesetIngestCommand>(c => c.TilesetId == tilesetId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var actionResult = await _controller.ProcessTileset(tilesetId, new MapTilerProcessRequestDto { UploadResult = "ok" });

        // Assert
        var ok = actionResult.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(expected);
    }

    [Fact]
    public async Task GetTilesets_WhenSucceeded_ReturnsOk()
    {
        // Arrange
        var regionId = Guid.NewGuid();
        var expected = new ApiResponse<List<CellularCoverageTilesetDto>>
        {
            Succeeded = true,
            Data = [new CellularCoverageTilesetDto { Id = Guid.NewGuid(), Name = "Test Tileset" }]
        };

        _mediatorMock
            .Setup(m => m.Send(It.Is<GetTilesetsQuery>(q => q.RegionId == regionId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var actionResult = await _controller.GetTilesets(regionId);

        // Assert
        var ok = actionResult.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(expected);
    }

    [Fact]
    public async Task GetActiveTilesets_WhenSucceeded_ReturnsOk()
    {
        // Arrange
        var regionId = Guid.NewGuid();
        var providerId = Guid.NewGuid();
        var expected = new ApiResponse<List<CellularCoverageTilesetDto>>
        {
            Succeeded = true,
            Data = [new CellularCoverageTilesetDto { Id = Guid.NewGuid(), Name = "Active Tileset", IsActive = true }]
        };

        _mediatorMock
            .Setup(m => m.Send(It.Is<GetActiveTilesetsQuery>(q =>
                q.RegionId == regionId && q.ServiceProviderId == providerId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var actionResult = await _controller.GetActiveTilesets(regionId, providerId);

        // Assert
        var ok = actionResult.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(expected);
    }
}
