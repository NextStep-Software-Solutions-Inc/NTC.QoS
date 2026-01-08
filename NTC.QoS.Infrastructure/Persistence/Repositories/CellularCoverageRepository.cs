using Microsoft.EntityFrameworkCore;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Interfaces;
using System.Linq.Expressions;

namespace NTC.QoS.Infrastructure.Persistence.Repositories;

internal sealed class CellularCoverageRepository(ApplicationDbContext context) : Repository<CellularCoverage>(context), ICellularCoverageRepository
{
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
        return await QueryAsNoTracking(cc => cc.MunicipalityId == municipalityId && !cc.IsDeleted)
            .ToListAsync(cancellationToken);
    }
}