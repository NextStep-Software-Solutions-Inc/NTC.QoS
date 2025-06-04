using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Interfaces;

namespace NTC.QoS.Infrastructure.Persistence.Repositories;

internal sealed class CellRepository(ApplicationDbContext context) : Repository<Cell>(context), ICellRepository
{
}
