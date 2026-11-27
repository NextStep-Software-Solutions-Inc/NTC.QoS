using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using FluentAssertions;
using Hangfire;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.MapTiler;
using NTC.QoS.Infrastructure.BackgroundTasks;
using NTC.QoS.Infrastructure.Configuration;
using Xunit;

namespace NTC.QoS.Tests.Infrastructure.BackgroundTasks;

public class PollMapTilerTilesetJobTests
{
    private readonly Mock<IRepository<CellularCoverageTileset>> _tilesetRepositoryMock = new();
    private readonly Mock<IMapTilerIngestService> _mapTilerServiceMock = new();
    private readonly Mock<IBackgroundJobService> _backgroundJobServiceMock = new();
    private readonly Mock<ILogger<PollMapTilerTilesetJob>> _loggerMock = new();

    private PollMapTilerTilesetJob CreateJob(MapTilerOptions? options = null) =>
        new(
            _tilesetRepositoryMock.Object,
            _mapTilerServiceMock.Object,
            _backgroundJobServiceMock.Object,
            _loggerMock.Object,
            options != null ? Options.Create(options) : null);

    [Fact]
    public void ExecuteAsync_HasExpectedHangfireAutomaticRetryPolicy()
    {
        var method = typeof(PollMapTilerTilesetJob).GetMethod(nameof(PollMapTilerTilesetJob.ExecuteAsync));
        var retryAttr = method?.GetCustomAttribute<AutomaticRetryAttribute>();

        retryAttr.Should().NotBeNull();
        retryAttr!.Attempts.Should().Be(3);
        retryAttr.OnAttemptsExceeded.Should().Be(AttemptsExceededAction.Fail);
    }

    [Fact]
    public async Task ExecuteAsync_WhenProcessingAndBelowConfiguredMaxAttempts_ReschedulesWithConfiguredInterval()
    {
        // Arrange
        var tilesetId = Guid.NewGuid();
        var tileset = new CellularCoverageTileset
        {
            Id = tilesetId,
            Name = "Caraga 3G",
            FileName = "caraga_3g.mbtiles",
            IngestId = "ingest-heavy-1",
            Status = TilesetStatusEnum.Processing
        };

        _tilesetRepositoryMock
            .Setup(r => r.Query(It.IsAny<Expression<Func<CellularCoverageTileset, bool>>>()))
            .Returns(new TestAsyncEnumerable<CellularCoverageTileset>([tileset]));

        _mapTilerServiceMock
            .Setup(s => s.GetIngestDetailsAsync("ingest-heavy-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MapTilerIngestResponseDto { Id = "ingest-heavy-1", State = "processing", Progress = 45 });

        var job = CreateJob(new MapTilerOptions { PollMaxAttempts = 120, PollIntervalSeconds = 20 });

        // Act
        await job.ExecuteAsync(tilesetId, "ingest-heavy-1", attemptCount: 60, CancellationToken.None);

        // Assert: attempt 60 still reschedules when PollMaxAttempts = 120
        _backgroundJobServiceMock.Verify(
            b => b.Schedule<IPollMapTilerTilesetJob>(
                It.IsAny<Expression<Action<IPollMapTilerTilesetJob>>>(),
                TimeSpan.FromSeconds(20)),
            Times.Once);
        tileset.Status.Should().Be(TilesetStatusEnum.Processing);
    }

    [Fact]
    public async Task ExecuteAsync_WhenAtConfiguredMaxAttempts_MarksTilesetFailedAndDoesNotReschedule()
    {
        // Arrange
        var tilesetId = Guid.NewGuid();
        var tileset = new CellularCoverageTileset
        {
            Id = tilesetId,
            Name = "Caraga 3G",
            FileName = "caraga_3g.mbtiles",
            IngestId = "ingest-timeout",
            Status = TilesetStatusEnum.Processing
        };

        _tilesetRepositoryMock
            .Setup(r => r.Query(It.IsAny<Expression<Func<CellularCoverageTileset, bool>>>()))
            .Returns(new TestAsyncEnumerable<CellularCoverageTileset>([tileset]));

        _mapTilerServiceMock
            .Setup(s => s.GetIngestDetailsAsync("ingest-timeout", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MapTilerIngestResponseDto { Id = "ingest-timeout", State = "processing", Progress = 90 });

        var job = CreateJob(new MapTilerOptions { PollMaxAttempts = 120, PollIntervalSeconds = 15 });

        // Act
        await job.ExecuteAsync(tilesetId, "ingest-timeout", attemptCount: 120, CancellationToken.None);

        // Assert
        _backgroundJobServiceMock.Verify(
            b => b.Schedule<IPollMapTilerTilesetJob>(
                It.IsAny<Expression<Action<IPollMapTilerTilesetJob>>>(),
                It.IsAny<TimeSpan>()),
            Times.Never);
        tileset.Status.Should().Be(TilesetStatusEnum.Failed);
        tileset.ErrorMessage.Should().Contain("timed out");
        _tilesetRepositoryMock.Verify(r => r.Update(tileset), Times.Once);
        _tilesetRepositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenTilesetNotFound_AbortsWithoutPollingOrScheduling()
    {
        _tilesetRepositoryMock
            .Setup(r => r.Query(It.IsAny<Expression<Func<CellularCoverageTileset, bool>>>()))
            .Returns(new TestAsyncEnumerable<CellularCoverageTileset>([]));

        var job = CreateJob();

        await job.ExecuteAsync(Guid.NewGuid(), "ingest-missing", attemptCount: 1, CancellationToken.None);

        _mapTilerServiceMock.Verify(
            s => s.GetIngestDetailsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _backgroundJobServiceMock.Verify(
            b => b.Schedule<IPollMapTilerTilesetJob>(It.IsAny<Expression<Action<IPollMapTilerTilesetJob>>>(), It.IsAny<TimeSpan>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WhenMapTilerReturnsNull_ReschedulesUntilMaxAttemptsThenFails()
    {
        var tilesetId = Guid.NewGuid();
        var tileset = new CellularCoverageTileset
        {
            Id = tilesetId,
            IngestId = "ingest-transient",
            Status = TilesetStatusEnum.Processing
        };

        _tilesetRepositoryMock
            .Setup(r => r.Query(It.IsAny<Expression<Func<CellularCoverageTileset, bool>>>()))
            .Returns(new TestAsyncEnumerable<CellularCoverageTileset>([tileset]));
        _mapTilerServiceMock
            .Setup(s => s.GetIngestDetailsAsync("ingest-transient", It.IsAny<CancellationToken>()))
            .ReturnsAsync((MapTilerIngestResponseDto?)null);

        var job = CreateJob(new MapTilerOptions { PollMaxAttempts = 3, PollIntervalSeconds = 10 });

        await job.ExecuteAsync(tilesetId, "ingest-transient", attemptCount: 2, CancellationToken.None);
        _backgroundJobServiceMock.Verify(
            b => b.Schedule<IPollMapTilerTilesetJob>(It.IsAny<Expression<Action<IPollMapTilerTilesetJob>>>(), TimeSpan.FromSeconds(10)),
            Times.Once);
        tileset.Status.Should().Be(TilesetStatusEnum.Processing);

        await job.ExecuteAsync(tilesetId, "ingest-transient", attemptCount: 3, CancellationToken.None);
        tileset.Status.Should().Be(TilesetStatusEnum.Failed);
        tileset.ErrorMessage.Should().Contain("timed out");
    }

    [Theory]
    [InlineData("completed", "doc-ready-1")]
    [InlineData("", "doc-implicit-ready")]
    [InlineData(null, "doc-null-state-ready")]
    public async Task ExecuteAsync_WhenCompleted_MarksReadyDeactivatesSupersededAndDoesNotReschedule(string? state, string documentId)
    {
        var tilesetId = Guid.NewGuid();
        var oldTileset = new CellularCoverageTileset
        {
            Id = Guid.NewGuid(),
            DocumentId = documentId,
            Status = TilesetStatusEnum.Ready,
            IsActive = true
        };
        var tileset = new CellularCoverageTileset
        {
            Id = tilesetId,
            IngestId = "ingest-done",
            Status = TilesetStatusEnum.Processing,
            ErrorMessage = "previous transient error",
            IsActive = true
        };

        _tilesetRepositoryMock
            .SetupSequence(r => r.Query(It.IsAny<Expression<Func<CellularCoverageTileset, bool>>>()))
            .Returns(new TestAsyncEnumerable<CellularCoverageTileset>([tileset]))
            .Returns(new TestAsyncEnumerable<CellularCoverageTileset>([oldTileset]));

        _mapTilerServiceMock
            .Setup(s => s.GetIngestDetailsAsync("ingest-done", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MapTilerIngestResponseDto { Id = "ingest-done", State = state!, DocumentId = documentId });
        _mapTilerServiceMock
            .Setup(s => s.GetTileJsonUrl(documentId, It.IsAny<string?>()))
            .Returns($"https://api.maptiler.com/tiles/{documentId}/tiles.json?key=pub");

        var job = CreateJob();

        await job.ExecuteAsync(tilesetId, "ingest-done", attemptCount: 1, CancellationToken.None);

        tileset.Status.Should().Be(TilesetStatusEnum.Ready);
        tileset.DocumentId.Should().Be(documentId);
        tileset.ErrorMessage.Should().BeNull();
        tileset.TileJsonUrl.Should().Contain(documentId);
        oldTileset.IsActive.Should().BeFalse();
        _backgroundJobServiceMock.Verify(
            b => b.Schedule<IPollMapTilerTilesetJob>(It.IsAny<Expression<Action<IPollMapTilerTilesetJob>>>(), It.IsAny<TimeSpan>()),
            Times.Never);
    }

    [Theory]
    [InlineData("upload")]
    [InlineData("queued")]
    [InlineData("unknown_future_state")]
    public async Task ExecuteAsync_WhenUploadingTransitionsToProcessingAndReschedules(string state)
    {
        var tilesetId = Guid.NewGuid();
        var tileset = new CellularCoverageTileset
        {
            Id = tilesetId,
            IngestId = "ingest-in-progress",
            Status = TilesetStatusEnum.Uploading
        };

        _tilesetRepositoryMock
            .Setup(r => r.Query(It.IsAny<Expression<Func<CellularCoverageTileset, bool>>>()))
            .Returns(new TestAsyncEnumerable<CellularCoverageTileset>([tileset]));
        _mapTilerServiceMock
            .Setup(s => s.GetIngestDetailsAsync("ingest-in-progress", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MapTilerIngestResponseDto { Id = "ingest-in-progress", State = state, DocumentId = Guid.Empty.ToString() });

        var job = CreateJob(new MapTilerOptions { PollMaxAttempts = 10, PollIntervalSeconds = 15 });

        await job.ExecuteAsync(tilesetId, "ingest-in-progress", attemptCount: 1, CancellationToken.None);

        tileset.Status.Should().Be(TilesetStatusEnum.Processing);
        _backgroundJobServiceMock.Verify(
            b => b.Schedule<IPollMapTilerTilesetJob>(It.IsAny<Expression<Action<IPollMapTilerTilesetJob>>>(), TimeSpan.FromSeconds(15)),
            Times.Once);
    }

    [Theory]
    [InlineData("error")]
    [InlineData("failed")]
    [InlineData("canceled")]
    public async Task ExecuteAsync_WhenTerminalErrorState_SanitizesSensitiveMetadataBeforePersisting(string terminalState)
    {
        var tilesetId = Guid.NewGuid();
        var tileset = new CellularCoverageTileset
        {
            Id = tilesetId,
            IngestId = "ingest-fail",
            Status = TilesetStatusEnum.Processing
        };

        using var errorJson = JsonDocument.Parse(
            """
            {
              "message": "Invalid MBTiles schema from https://storage.googleapis.com/internal-bucket/file?key=secret123&token=abc using Token superSecretToken at /var/app/ingest/worker.py"
            }
            """);

        _tilesetRepositoryMock
            .Setup(r => r.Query(It.IsAny<Expression<Func<CellularCoverageTileset, bool>>>()))
            .Returns(new TestAsyncEnumerable<CellularCoverageTileset>([tileset]));
        _mapTilerServiceMock
            .Setup(s => s.GetIngestDetailsAsync("ingest-fail", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MapTilerIngestResponseDto
            {
                Id = "ingest-fail",
                State = terminalState,
                Errors = errorJson.RootElement.Clone()
            });

        var job = CreateJob();

        await job.ExecuteAsync(tilesetId, "ingest-fail", attemptCount: 1, CancellationToken.None);

        tileset.Status.Should().Be(TilesetStatusEnum.Failed);
        tileset.ErrorMessage.Should().NotBeNullOrWhiteSpace();
        tileset.ErrorMessage.Should().Contain("Invalid MBTiles schema");
        tileset.ErrorMessage.Should().NotContain("secret123");
        tileset.ErrorMessage.Should().NotContain("superSecretToken");
        tileset.ErrorMessage.Should().NotContain("storage.googleapis.com");
        tileset.ErrorMessage.Should().NotContain("/var/app/ingest/worker.py");
        _backgroundJobServiceMock.Verify(
            b => b.Schedule<IPollMapTilerTilesetJob>(It.IsAny<Expression<Action<IPollMapTilerTilesetJob>>>(), It.IsAny<TimeSpan>()),
            Times.Never);
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
