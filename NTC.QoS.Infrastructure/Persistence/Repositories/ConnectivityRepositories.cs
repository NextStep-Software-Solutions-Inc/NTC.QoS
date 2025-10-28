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
            .Where(ca => locationIds.Contains(ca.LocationId) && ca.LocationType == locationType)
            .OrderBy(ca => ca.LocationId)
            .ToListAsync(cancellationToken);
    }

    public async Task<ConnectivityAssessment?> GetByLocationAsync(
        Guid locationId, 
        LocationTypeEnum locationType, 
        CancellationToken cancellationToken = default)
    {
        return await _context.Set<ConnectivityAssessment>()
            .FirstOrDefaultAsync(ca => ca.LocationId == locationId && ca.LocationType == locationType, cancellationToken);
    }

    public async Task<List<ConnectivityAssessment>> GetByConnectivityStatusAsync(
        ConnectivityStatusEnum status, 
        CancellationToken cancellationToken = default)
    {
        return await _context.Set<ConnectivityAssessment>()
            .Where(ca => ca.ConnectivityStatus == status)
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
            .AnyAsync(ca => ca.LocationId == locationId && ca.LocationType == locationType, cancellationToken);
    }

    public async Task<List<ConnectivityAssessmentProvider>> GetProvidersByAssessmentIdAsync(
        Guid assessmentId, 
        CancellationToken cancellationToken = default)
    {
        return await _context.Set<ConnectivityAssessmentProvider>()
            .Where(cap => cap.ConnectivityAssessmentId == assessmentId)
            .OrderByDescending(cap => cap.TotalAssetCount)
            .ToListAsync(cancellationToken);
    }

    public async Task DeleteProvidersByAssessmentIdAsync(
        Guid assessmentId, 
        CancellationToken cancellationToken = default)
    {
        var providers = await _context.Set<ConnectivityAssessmentProvider>()
            .Where(cap => cap.ConnectivityAssessmentId == assessmentId)
            .ToListAsync(cancellationToken);

        _context.Set<ConnectivityAssessmentProvider>().RemoveRange(providers);
    }

    public async Task AddProviderAsync(
        ConnectivityAssessmentProvider provider, 
        CancellationToken cancellationToken = default)
    {
        await _context.Set<ConnectivityAssessmentProvider>().AddAsync(provider, cancellationToken);
    }
}

internal sealed class WiFiHotspotRepository(ApplicationDbContext context) 
    : Repository<WiFiHotspot>(context), IWiFiHotspotRepository
{
    public override IQueryable<WiFiHotspot> Query(System.Linq.Expressions.Expression<Func<WiFiHotspot, bool>>? expression = null)
    {
        var query = _context.Set<WiFiHotspot>()
            .Include(w => w.ServiceProvider)
                .ThenInclude(sp => sp.Provider)
            .Include(w => w.ServiceProvider)
                .ThenInclude(sp => sp.Service)
            .AsQueryable();

        if (expression != null)
        {
            query = query.Where(expression);
        }

        return query;
    }

    public async Task<List<WiFiHotspot>> GetActiveHotspotsAsync(CancellationToken cancellationToken = default)
    {
        return await Query(w => w.IsActive)
            .OrderBy(w => w.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<WiFiHotspot>> GetPublicHotspotsAsync(CancellationToken cancellationToken = default)
    {
        return await Query(w => w.IsPublic && w.IsActive)
            .OrderBy(w => w.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<WiFiHotspot>> GetHotspotsByServiceProviderAsync(
        Guid serviceProviderId, 
        CancellationToken cancellationToken = default)
    {
        return await Query(w => w.ServiceProviderId == serviceProviderId)
            .OrderBy(w => w.Name)
            .ToListAsync(cancellationToken);
    }
}