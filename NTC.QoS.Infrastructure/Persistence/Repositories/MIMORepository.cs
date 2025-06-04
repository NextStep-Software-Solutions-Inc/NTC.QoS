using Microsoft.EntityFrameworkCore;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Interfaces;

namespace NTC.QoS.Infrastructure.Persistence.Repositories;

internal sealed class MIMORepository(ApplicationDbContext context) : Repository<MIMO>(context), IMIMORepository
{
}