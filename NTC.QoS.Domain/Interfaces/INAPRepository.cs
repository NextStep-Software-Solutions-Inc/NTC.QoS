using NTC.QoS.Domain.Aggregates;

namespace NTC.QoS.Domain.Interfaces;

public interface INAPRepository : IRepository<NAP>
{
    IQueryable<NAP> QueryNAPsWithinBoundaries(
        List<Guid>? regionIds = null,
        List<Guid>? provinceIds = null,
        List<Guid>? municipalityIds = null,
        List<Guid>? barangayIds = null);
}
