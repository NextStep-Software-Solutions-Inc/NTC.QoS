using Microsoft.EntityFrameworkCore;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Interfaces;

namespace NTC.QoS.Infrastructure.Persistence.Repositories;


internal sealed class ConnectivityAssessmentRepository(ApplicationDbContext context) 
    : Repository<ConnectivityAssessment>(context), IConnectivityAssessmentRepository
{
    public async Task<List<ConnectivityAssessment>> GetByLocationIdsAsync(
        List<Guid> locationIds, 
        LocationTypeEnum locationType, 
        CancellationToken cancellationToken = default)
    {
        return await _context.Set<ConnectivityAssessment>()
            .Where(ca => locationIds.Contains(ca.LocationId) && ca.LocationType == locationType && !ca.IsDeleted)
            .OrderBy(ca => ca.LocationId)
            .ToListAsync(cancellationToken);
    }

    public async Task<ConnectivityAssessment?> GetByLocationAsync(
        Guid locationId, 
        LocationTypeEnum locationType, 
        CancellationToken cancellationToken = default)
    {
        return await _context.Set<ConnectivityAssessment>()
            .FirstOrDefaultAsync(ca => ca.LocationId == locationId && ca.LocationType == locationType && !ca.IsDeleted, cancellationToken);
    }

    /// <summary>
    /// Gets a connectivity assessment by location ID and location type (alias for GetByLocationAsync)
    /// </summary>
    public async Task<ConnectivityAssessment?> GetByLocationIdAsync(
        Guid locationId, 
        LocationTypeEnum locationType, 
        CancellationToken cancellationToken = default)
    {
        return await GetByLocationAsync(locationId, locationType, cancellationToken);
    }

    public async Task<List<ConnectivityAssessment>> GetByConnectivityStatusAsync(
        ConnectivityStatusEnum status, 
        CancellationToken cancellationToken = default)
    {
        return await _context.Set<ConnectivityAssessment>()
            .Where(ca => ca.ConnectivityStatus == status && !ca.IsDeleted)
            .OrderBy(ca => ca.LocationType)
            .ThenBy(ca => ca.LocationId)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsForLocationAsync(
        Guid locationId, 
        LocationTypeEnum locationType, 
        CancellationToken cancellationToken = default)
    {
        return await _context.Set<ConnectivityAssessment>()
            .AnyAsync(ca => ca.LocationId == locationId && ca.LocationType == locationType && !ca.IsDeleted, cancellationToken);
    }
}
