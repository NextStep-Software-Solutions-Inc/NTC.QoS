using Microsoft.EntityFrameworkCore;
using NTC.QoS.Domain.Interfaces;
using System.Linq.Expressions;

namespace NTC.QoS.Infrastructure.Persistence.Repositories;

internal sealed class FileRepository(ApplicationDbContext context) : Repository<Domain.Entities.File>(context), IFileRepository
{
    /// <summary>
    /// Applies case-insensitive search filter on file name using EF.Functions.ILike
    /// </summary>
    public IQueryable<Domain.Entities.File> SearchByName(IQueryable<Domain.Entities.File> query, string name)
    {
        return ApplySearch(query, name, f => f.Name);
    }
}