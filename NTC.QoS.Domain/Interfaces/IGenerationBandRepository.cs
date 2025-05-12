using NTC.QoS.Domain.Aggregates;

namespace NTC.QoS.Domain.Interfaces;

public interface IGenerationBandRepository : IRepository<GenerationBand>
{
    Task<GenerationBand?> GetGenerationBandIdAsync(string generation, string band, CancellationToken cancellationToken = default);
}
