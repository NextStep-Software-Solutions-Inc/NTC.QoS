using Microsoft.EntityFrameworkCore;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Interfaces;
using System.Linq.Expressions;

namespace NTC.QoS.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for CellularCoverage (CSV uploads)
/// </summary>
internal sealed class CellularCoverageV2Repository : Repository<CellularCoverage>, ICellularCoverageV2Repository
{
    public CellularCoverageV2Repository(ApplicationDbContext context) : base(context)
    {
    }

    public override IQueryable<CellularCoverage> Query(Expression<Func<CellularCoverage, bool>>? expression = null)
    {
        var query = _context.Set<CellularCoverage>()
            .Include(cc => cc.Network)
            .Include(cc => cc.Band)
            .Include(cc => cc.Technology)
            .Include(cc => cc.CellSector!)
                .ThenInclude(cs => cs.Cell)
            .Include(cc => cc.Municipality)
            .AsQueryable();

        if (expression != null)
        {
            query = query.Where(expression);
        }

        return query;
    }

    public async Task<bool> CheckDuplicateAsync(
        double latitude,
        double longitude,
        Guid networkId,
        Guid bandId,
        Guid municipalityId,
        CancellationToken cancellationToken = default)
    {
        // Use small tolerance for coordinate comparison (~11 meters)
        const double tolerance = 0.0001;

        return await _context.Set<CellularCoverage>()
            .Where(cc =>
                !cc.IsDeleted &&
                cc.NetworkId == networkId &&
                cc.BandId == bandId &&
                cc.MunicipalityId == municipalityId &&
                Math.Abs(cc.Location.Y - latitude) < tolerance &&
                Math.Abs(cc.Location.X - longitude) < tolerance)
            .AnyAsync(cancellationToken);
    }

    public async Task<List<CellularCoverage>> GetByMunicipalityAsync(
        Guid municipalityId,
        CancellationToken cancellationToken = default)
    {
        return await Query(cc => cc.MunicipalityId == municipalityId && !cc.IsDeleted)
            .ToListAsync(cancellationToken);
    }
}
