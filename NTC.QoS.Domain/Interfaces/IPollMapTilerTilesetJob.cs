namespace NTC.QoS.Domain.Interfaces;

/// <summary>
/// Background job interface to poll MapTiler Ingestion task status until completion
/// </summary>
public interface IPollMapTilerTilesetJob
{
    Task ExecuteAsync(Guid tilesetId, string ingestId, int attemptCount, CancellationToken cancellationToken);
}
