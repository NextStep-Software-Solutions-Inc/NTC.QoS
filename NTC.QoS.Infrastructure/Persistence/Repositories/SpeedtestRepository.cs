using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Interfaces;

namespace NTC.QoS.Infrastructure.Persistence.Repositories;

internal sealed class SpeedtestRepository(ApplicationDbContext context)
    : Repository<Speedtest>(context), ISpeedtestRepository
{
}

