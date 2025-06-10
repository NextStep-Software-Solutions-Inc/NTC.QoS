using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Interfaces;

namespace NTC.QoS.Infrastructure.Persistence.Repositories;

internal sealed class SectorRepository(ApplicationDbContext context) 
    : Repository<Sector>(context), ISectorRepository
{
}
