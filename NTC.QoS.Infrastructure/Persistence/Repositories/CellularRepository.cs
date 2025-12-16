using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Interfaces;

namespace NTC.QoS.Infrastructure.Persistence.Repositories;

internal sealed class CellularRepository(ApplicationDbContext context) : Repository<Cellular>(context), ICellularRepository
{

}
