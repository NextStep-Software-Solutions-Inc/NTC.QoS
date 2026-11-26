using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NTC.QoS.Application.Features.CellularCoverage.Handlers.Commands;
using NTC.QoS.Application.Features.CellularCoverage.Requests;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.MapTiler;
using Xunit;

namespace NTC.QoS.Tests.Application.Features.CellularCoverage.Handlers.Commands;

public class InitTilesetIngestHandlerTests
{
    private readonly Mock<IRepository<CellularCoverageTileset>> _tilesetRepositoryMock;
    private readonly Mock<IMapTilerIngestService> _mapTilerServiceMock;
    private readonly Mock<IUserContextService> _userContextServiceMock;
    private readonly Mock<ILogger<InitTilesetIngestHandler>> _loggerMock;
    private readonly InitTilesetIngestHandler _handler;

    public InitTilesetIngestHandlerTests()
    {
        _tilesetRepositoryMock = new Mock<IRepository<CellularCoverageTileset>>();
        _mapTilerServiceMock = new Mock<IMapTilerIngestService>();
        _userContextServiceMock = new Mock<IUserContextService>();
        _loggerMock = new Mock<ILogger<InitTilesetIngestHandler>>();

        _handler = new InitTilesetIngestHandler(
            _tilesetRepositoryMock.Object,
            _mapTilerServiceMock.Object,
            _userContextServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WhenMapTilerInitSucceeds_CreatesTilesetAndReturnsUploadUrl()
    {
        // Arrange
        var regionId = Guid.NewGuid();
        var providerId = Guid.NewGuid();
        var bandId = Guid.NewGuid();
        var techId = Guid.NewGuid();
        var command = new InitTilesetIngestCommand
        {
            FileName = "caraga_4g.mbtiles",
            FileSize = 1048576,
            DisplayName = "Caraga 4G Coverage",
            RegionId = regionId,
            ServiceProviderId = providerId,
            BandId = bandId,
            TechnologyId = techId
        };

        _mapTilerServiceMock
            .Setup(s => s.InitIngestAsync(command.FileName, command.FileSize, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MapTilerIngestResponseDto
            {
                Id = "ingest-123",
                DocumentId = "doc-456",
                State = "upload",
                UploadUrl = "https://upload.googleapis.com/resumable/abc"
            });

        _userContextServiceMock.Setup(u => u.GetCurrentUserId()).Returns("user_abc");

        CellularCoverageTileset? capturedTileset = null;
        _tilesetRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<CellularCoverageTileset>(), It.IsAny<CancellationToken>()))
            .Callback<CellularCoverageTileset, CancellationToken>((t, _) => capturedTileset = t)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.IngestId.Should().Be("ingest-123");
        result.Data.UploadUrl.Should().Be("https://upload.googleapis.com/resumable/abc");
        result.Data.State.Should().Be("upload");

        capturedTileset.Should().NotBeNull();
        capturedTileset!.Name.Should().Be("Caraga 4G Coverage");
        capturedTileset.FileName.Should().Be("caraga_4g.mbtiles");
        capturedTileset.FileSize.Should().Be(1048576);
        capturedTileset.IngestId.Should().Be("ingest-123");
        capturedTileset.DocumentId.Should().Be("doc-456");
        capturedTileset.Status.Should().Be(TilesetStatusEnum.Uploading);
        capturedTileset.RegionId.Should().Be(regionId);
        capturedTileset.ServiceProviderId.Should().Be(providerId);
        capturedTileset.BandId.Should().Be(bandId);
        capturedTileset.TechnologyId.Should().Be(techId);
        capturedTileset.CreatedBy.Should().Be("user_abc");
        capturedTileset.IsActive.Should().BeTrue();

        _tilesetRepositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenDisplayNameOmittedAndDocumentIdEmpty_UsesFallbacks()
    {
        // Arrange
        var command = new InitTilesetIngestCommand
        {
            FileName = "cabadbaran_2g.mbtiles",
            FileSize = 2048,
            DisplayName = null
        };

        _mapTilerServiceMock.SetupGet(s => s.DefaultDocumentId).Returns("default-doc-id");
        _mapTilerServiceMock
            .Setup(s => s.InitIngestAsync(command.FileName, command.FileSize, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MapTilerIngestResponseDto
            {
                Id = "ingest-789",
                DocumentId = Guid.Empty.ToString(),
                State = "upload",
                Upload = new MapTilerUploadInfoDto { Url = "https://storage.maptiler.com/upload/789" }
            });

        _userContextServiceMock.Setup(u => u.GetCurrentUserId()).Returns((string?)null);

        CellularCoverageTileset? capturedTileset = null;
        _tilesetRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<CellularCoverageTileset>(), It.IsAny<CancellationToken>()))
            .Callback<CellularCoverageTileset, CancellationToken>((t, _) => capturedTileset = t)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Data!.UploadUrl.Should().Be("https://storage.maptiler.com/upload/789");
        capturedTileset.Should().NotBeNull();
        capturedTileset!.Name.Should().Be("cabadbaran_2g");
        capturedTileset.DocumentId.Should().Be("default-doc-id");
        capturedTileset.CreatedBy.Should().Be("System");
    }

    [Fact]
    public async Task Handle_WhenMapTilerReturnsNullOrMissingUploadUrl_ReturnsFailureWithoutSaving()
    {
        // Arrange
        var command = new InitTilesetIngestCommand { FileName = "test.mbtiles", FileSize = 512 };

        _mapTilerServiceMock
            .Setup(s => s.InitIngestAsync(command.FileName, command.FileSize, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MapTilerIngestResponseDto { Id = "ingest-empty", UploadUrl = null });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Data.Should().BeNull();
        _tilesetRepositoryMock.Verify(
            r => r.AddAsync(It.IsAny<CellularCoverageTileset>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _tilesetRepositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenMapTilerThrowsException_ReturnsFailureResponse()
    {
        // Arrange
        var command = new InitTilesetIngestCommand { FileName = "boom.mbtiles", FileSize = 100 };

        _mapTilerServiceMock
            .Setup(s => s.InitIngestAsync(command.FileName, command.FileSize, null, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("MapTiler unreachable"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Data.Should().BeNull();
        result.Message.Should().Contain("MapTiler unreachable");
    }
}
