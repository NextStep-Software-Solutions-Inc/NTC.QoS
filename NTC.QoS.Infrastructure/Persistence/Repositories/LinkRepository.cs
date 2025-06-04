using Microsoft.EntityFrameworkCore;
using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Interfaces;
using System.Linq.Expressions;

namespace NTC.QoS.Infrastructure.Persistence.Repositories;

internal sealed class LinkRepository(ApplicationDbContext context) : Repository<Link>(context), ILinkRepository
{
    public async Task<bool> ValidateLinkTypesAsync(Guid linkId, LinkTypeEnum expectedType, CancellationToken cancellationToken = default)
    {
        

        var link = await _context.Set<Link>()
            .AsNoTracking()
            .FirstOrDefaultAsync(l => EF.Property<Guid>(l, "Id") == linkId && l.Type == expectedType && !l.IsDeleted, cancellationToken);

        return link is not null;
    }
}
