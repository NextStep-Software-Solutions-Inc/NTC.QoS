using NTC.QoS.Domain.Aggregates;

namespace NTC.QoS.Domain.Interfaces;

public interface ILinkRepository: IRepository<Link>
{
    Task<bool> ValidateLinkTypesAsync(Guid? uplinkId, Guid? downlinkId, CancellationToken cancellationToken = default);
}