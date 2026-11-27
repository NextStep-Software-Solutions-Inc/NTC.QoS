using System.Linq.Expressions;
using System.Reflection;
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
