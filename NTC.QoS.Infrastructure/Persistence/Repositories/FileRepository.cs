using NTC.QoS.Domain.Interfaces;

namespace NTC.QoS.Infrastructure.Persistence.Repositories;

internal sealed class FileRepository(ApplicationDbContext context) : Repository<Domain.Entities.File>(context), IFileRepository
{
    public IQueryable<Domain.Entities.File> SearchByName(IQueryable<Domain.Entities.File> query, string name)
    {
        return ApplySearch(query, name, f => f.Name);
    }
}