using Microsoft.EntityFrameworkCore;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Interfaces;
using System.Linq.Expressions;

namespace NTC.QoS.Infrastructure.Persistence.Repositories;

internal sealed class ZoneRepository(ApplicationDbContext context) : Repository<Zone>(context), IZoneRepository
{
    public override async Task<Zone?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<Zone>().Where(e => !EF.Property<bool>(e, "IsDeleted"))
            .Include(z => z.Barangay)
                .ThenInclude(b => b!.Municipality)
                    .ThenInclude(m => m!.Province)
                        .ThenInclude(p => p!.Region);

        return await query.FirstOrDefaultAsync(e => EF.Property<Guid>(e, "Id") == id, cancellationToken);
    }

    public override async Task<List<Zone>> ListAsync(
        Expression<Func<Zone, bool>>? expression = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Zone> query = _context.Set<Zone>()
            .Where(e => !EF.Property<bool>(e, "IsDeleted"))
            .Include(z => z.Barangay)
                .ThenInclude(b => b!.Municipality)
                    .ThenInclude(m => m!.Province)
                        .ThenInclude(p => p!.Region);

        if (expression != null)
        {
            query = query.Where(expression);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public override async Task<Zone?> FindAsync(Expression<Func<Zone, bool>> expression, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<Zone>().Where(expression)
           .Include(z => z.Barangay).ThenInclude(z => z!.Municipality).ThenInclude(m => m!.Province).ThenInclude(p => p!.Region);

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Zone?> FindByNameAsync(string? municipalityName, string? provinceName, string? regionName, CancellationToken cancellationToken = default)
    {
        IQueryable<Zone> query = _context.Set<Zone>()
            .Where(z => !EF.Property<bool>(z, "IsDeleted"))
            .Include(z => z.Barangay!)
                .ThenInclude(b => b.Municipality!)
                    .ThenInclude(m => m.Province!)
                        .ThenInclude(p => p.Region!);

        if (!string.IsNullOrEmpty(municipalityName))
        {
            query = query.Where(z =>
                z.Barangay != null &&
                z.Barangay.Municipality != null &&
                (
                    (!string.IsNullOrEmpty(z.Barangay.Municipality.Name) && z.Barangay.Municipality.Name.ToLower().Contains(municipalityName.ToLower())) ||
                    (!string.IsNullOrEmpty(z.Barangay.Municipality.Description) && z.Barangay.Municipality.Description.ToLower().Contains(municipalityName.ToLower()))
                ));
        }

        if (!string.IsNullOrEmpty(provinceName))
        {
            query = query.Where(z =>
                z.Barangay != null &&
                z.Barangay.Municipality != null &&
                z.Barangay.Municipality.Province != null &&
                (
                    (!string.IsNullOrEmpty(z.Barangay.Municipality.Province.Name) && z.Barangay.Municipality.Province.Name.ToLower().Contains(provinceName.ToLower())) ||
                    (!string.IsNullOrEmpty(z.Barangay.Municipality.Province.Description) && z.Barangay.Municipality.Province.Description.ToLower().Contains(provinceName.ToLower()))
                ));
        }

        if (!string.IsNullOrEmpty(regionName))
        {
            query = query.Where(z =>
                z.Barangay != null &&
                z.Barangay.Municipality != null &&
                z.Barangay.Municipality.Province != null &&
                z.Barangay.Municipality.Province.Region != null &&
                (
                    (!string.IsNullOrEmpty(z.Barangay.Municipality.Province.Region.Name) && z.Barangay.Municipality.Province.Region.Name.ToLower().Contains(regionName.ToLower())) ||
                    (!string.IsNullOrEmpty(z.Barangay.Municipality.Province.Region.Description) && z.Barangay.Municipality.Province.Region.Description.ToLower().Contains(regionName.ToLower()))
                ));
        }

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Zone?> FindFirstAnyMatchAsync(string? municipalityName, string? provinceName, string? regionName, CancellationToken cancellationToken = default)
    {
        IQueryable<Zone> query = _context.Set<Zone>()
            .Where(z => !EF.Property<bool>(z, "IsDeleted"))
            .Include(z => z.Barangay!)
                .ThenInclude(b => b.Municipality!)
                    .ThenInclude(m => m.Province!)
                        .ThenInclude(p => p.Region!);

        if (!string.IsNullOrEmpty(municipalityName))
        {
            query = query.Where(z =>
                z.Barangay != null &&
                z.Barangay.Municipality != null &&
                (
                    (!string.IsNullOrEmpty(z.Barangay.Municipality.Name) && z.Barangay.Municipality.Name.ToLower().Contains(municipalityName.ToLower())) ||
                    (!string.IsNullOrEmpty(z.Barangay.Municipality.Description) && z.Barangay.Municipality.Description.ToLower().Contains(municipalityName.ToLower()))
                ));
        }
        else if (!string.IsNullOrEmpty(provinceName))
        {
            query = query.Where(z =>
                z.Barangay != null &&
                z.Barangay.Municipality != null &&
                z.Barangay.Municipality.Province != null &&
                (
                    (!string.IsNullOrEmpty(z.Barangay.Municipality.Province.Name) && z.Barangay.Municipality.Province.Name.ToLower().Contains(provinceName.ToLower())) ||
                    (!string.IsNullOrEmpty(z.Barangay.Municipality.Province.Description) && z.Barangay.Municipality.Province.Description.ToLower().Contains(provinceName.ToLower()))
                ));
        }
        else if (!string.IsNullOrEmpty(regionName))
        {
            query = query.Where(z =>
                 z.Barangay != null &&
                 z.Barangay.Municipality != null &&
                 z.Barangay.Municipality.Province != null &&
                 z.Barangay.Municipality.Province.Region != null &&
                 (
                     (!string.IsNullOrEmpty(z.Barangay.Municipality.Province.Region.Name) && z.Barangay.Municipality.Province.Region.Name.ToLower().Contains(regionName.ToLower())) ||
                     (!string.IsNullOrEmpty(z.Barangay.Municipality.Province.Region.Description) && z.Barangay.Municipality.Province.Region.Description.ToLower().Contains(regionName.ToLower()))
                 ));
        }

        return await query.FirstOrDefaultAsync(cancellationToken);
    }
}
