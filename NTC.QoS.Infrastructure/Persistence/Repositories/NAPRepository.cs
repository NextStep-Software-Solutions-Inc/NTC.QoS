using Microsoft.EntityFrameworkCore;
using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Interfaces;
using System.Linq.Expressions;

namespace NTC.QoS.Infrastructure.Persistence.Repositories;
internal sealed class NAPRepository(ApplicationDbContext context) : Repository<NAP>(context), INAPRepository
{

}
