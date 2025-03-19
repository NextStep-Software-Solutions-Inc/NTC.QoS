using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Enums;

namespace NTC.QoS.Domain.Interfaces;

public interface ILinkRepository: IRepository<Link>
{
    Task<bool> ValidateLinkTypesAsync(Guid? linkId, LinkTypeEnum expectedType, CancellationToken cancellationToken = default);
}