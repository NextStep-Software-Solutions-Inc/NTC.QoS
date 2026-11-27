using System.Linq.Expressions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Query;
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

public class ProcessTilesetIngestHandlerTests
{
    private readonly Mock<IRepository<CellularCoverageTileset>> _tilesetRepositoryMock;
    private readonly Mock<IMapTilerIngestService> _mapTilerServiceMock;
    private readonly Mock<IBackgroundJobService> _backgroundJobServiceMock;
    private readonly Mock<ILogger<ProcessTilesetIngestHandler>> _loggerMock;
    private readonly ProcessTilesetIngestHandler _handler;

    public ProcessTilesetIngestHandlerTests()
    {
        _tilesetRepositoryMock = new Mock<IRepository<CellularCoverageTileset>>();
        _mapTilerServiceMock = new Mock<IMapTilerIngestService>();
        _backgroundJobServiceMock = new Mock<IBackgroundJobService>();
        _loggerMock = new Mock<ILogger<ProcessTilesetIngestHandler>>();

        _handler = new ProcessTilesetIngestHandler(
            _tilesetRepositoryMock.Object,
            _mapTilerServiceMock.Object,
            _backgroundJobServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WhenTilesetNotFound_ReturnsFailureWithoutCallingMapTilerOrSchedulingJob()
    {
        // Arrange
        var tilesetId = Guid.NewGuid();
        var command = new ProcessTilesetIngestCommand { TilesetId = tilesetId };

        _tilesetRepositoryMock
            .Setup(r => r.Query(It.IsAny<Expression<Func<CellularCoverageTileset, bool>>>()))
            .Returns(new TestAsyncEnumerable<CellularCoverageTileset>([]));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Data.Should().BeFalse();
        result.Message.Should().Contain(tilesetId.ToString());

        _mapTilerServiceMock.Verify(
            s => s.ProcessIngestAsync(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _backgroundJobServiceMock.Verify(
            b => b.Schedule<IPollMapTilerTilesetJob>(It.IsAny<Expression<Action<IPollMapTilerTilesetJob>>>(), It.IsAny<TimeSpan>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenTilesetFound_UpdatesStatusCallsMapTilerAndSchedulesPollingJob()
    {
        // Arrange
        var tilesetId = Guid.NewGuid();
        var tileset = new CellularCoverageTileset
        {
            Id = tilesetId,
            Name = "Caraga 4G",
            FileName = "caraga_4g.mbtiles",
            IngestId = "ingest-abc",
            Status = TilesetStatusEnum.Uploading
        };
        var uploadResult = new { etag = "etag-1" };
        var command = new ProcessTilesetIngestCommand
        {
            TilesetId = tilesetId,
            UploadResult = uploadResult
        };

        _tilesetRepositoryMock
            .Setup(r => r.Query(It.IsAny<Expression<Func<CellularCoverageTileset, bool>>>()))
            .Returns(new TestAsyncEnumerable<CellularCoverageTileset>([tileset]));

        _mapTilerServiceMock
            .Setup(s => s.ProcessIngestAsync("ingest-abc", uploadResult, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MapTilerIngestResponseDto { Id = "ingest-abc", State = "processing" });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Data.Should().BeTrue();
        tileset.Status.Should().Be(TilesetStatusEnum.Processing);

        _tilesetRepositoryMock.Verify(r => r.Update(tileset), Times.Once);
        _tilesetRepositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _backgroundJobServiceMock.Verify(
            b => b.Schedule<IPollMapTilerTilesetJob>(
                It.IsAny<Expression<Action<IPollMapTilerTilesetJob>>>(),
                TimeSpan.FromSeconds(5)),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenMapTilerThrows_ReturnsFailureResponse()
    {
        // Arrange
        var tilesetId = Guid.NewGuid();
        var tileset = new CellularCoverageTileset
        {
            Id = tilesetId,
            IngestId = "ingest-err",
            Status = TilesetStatusEnum.Uploading
        };
        var command = new ProcessTilesetIngestCommand { TilesetId = tilesetId };

        _tilesetRepositoryMock
            .Setup(r => r.Query(It.IsAny<Expression<Func<CellularCoverageTileset, bool>>>()))
            .Returns(new TestAsyncEnumerable<CellularCoverageTileset>([tileset]));

        _mapTilerServiceMock
            .Setup(s => s.ProcessIngestAsync("ingest-err", null, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Process API error"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Data.Should().BeFalse();
        result.Message.Should().Contain("Process API error");
    }

    private sealed class TestAsyncEnumerable<T>(IEnumerable<T> enumerable)
        : EnumerableQuery<T>(enumerable), IAsyncEnumerable<T>, IQueryable<T>
    {
        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) =>
            new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());

        IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);
    }

    private sealed class TestAsyncEnumerator<T>(IEnumerator<T> inner) : IAsyncEnumerator<T>
    {
        public T Current => inner.Current;

        public ValueTask DisposeAsync()
        {
            inner.Dispose();
            return ValueTask.CompletedTask;
        }

        public ValueTask<bool> MoveNextAsync() => new(inner.MoveNext());
    }

    private sealed class TestAsyncQueryProvider<TEntity>(IQueryProvider inner) : IAsyncQueryProvider
    {
        public IQueryable CreateQuery(Expression expression) => new TestAsyncEnumerable<TEntity>(inner.CreateQuery<TEntity>(expression));

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression) =>
            new TestAsyncEnumerable<TElement>(inner.CreateQuery<TElement>(expression));

        public object? Execute(Expression expression) => inner.Execute(expression);

        public TResult Execute<TResult>(Expression expression) => inner.Execute<TResult>(expression);

        public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
        {
            var expectedResultType = typeof(TResult).GetGenericArguments()[0];
            var executionResult = typeof(IQueryProvider)
                .GetMethod(name: nameof(IQueryProvider.Execute), genericParameterCount: 1, types: [typeof(Expression)])!
                .MakeGenericMethod(expectedResultType)
                .Invoke(inner, [expression]);

            return (TResult)typeof(Task)
                .GetMethod(nameof(Task.FromResult))!
                .MakeGenericMethod(expectedResultType)
                .Invoke(null, [executionResult])!;
        }
    }
}
