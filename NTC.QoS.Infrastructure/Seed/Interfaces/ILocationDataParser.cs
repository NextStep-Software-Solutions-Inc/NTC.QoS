namespace NTC.QoS.Infrastructure.Seed.Interfaces;

/// <summary>
/// Parses GeoJSON features from a stream in a memory-efficient manner.
/// </summary>
public interface ILocationDataParser<TFeature>
{
    /// <summary>
    /// Streams features from a GeoJSON file asynchronously.
    /// </summary>
    /// <param name="stream">The GeoJSON stream to parse.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Async enumerable of parsed features.</returns>
    IAsyncEnumerable<TFeature> ParseFeaturesAsync(Stream stream, CancellationToken cancellationToken = default);
}
