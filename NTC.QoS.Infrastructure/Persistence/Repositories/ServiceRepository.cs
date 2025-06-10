using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Interfaces;

namespace NTC.QoS.Infrastructure.Persistence.Repositories;

internal sealed class ServiceRepository(ApplicationDbContext context) : Repository<Service>(context), IServiceRepository
{
}
