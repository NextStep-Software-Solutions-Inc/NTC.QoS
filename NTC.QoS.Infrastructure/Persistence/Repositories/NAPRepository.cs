using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Interfaces;

namespace NTC.QoS.Infrastructure.Persistence.Repositories;

internal sealed class NAPRepository(ApplicationDbContext context) : Repository<NAP>(context), INAPRepository
{
}
