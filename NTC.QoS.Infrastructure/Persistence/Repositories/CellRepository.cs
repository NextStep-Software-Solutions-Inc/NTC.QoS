using Microsoft.EntityFrameworkCore;
using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.Dashboard;

namespace NTC.QoS.Infrastructure.Persistence.Repositories;

internal sealed class CellRepository(ApplicationDbContext context) : Repository<Cell>(context), ICellRepository
{
    /// <summary>
    /// Gets cell sites grouped by service provider with optimized queries.
    /// OPTIMIZED: Single query with proper filtering and spatial index usage.
    /// </summary>
    public async Task<CellSitesByProviderResponseDto> GetCellSitesByProviderAsync(
        Guid? regionId = null,
        Guid? serviceProviderId = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        // Validate service provider if specified
        if (serviceProviderId.HasValue)
        {
            var providerInfo = await _context.ServiceProviders
                .AsNoTracking()
                .Where(sp => sp.Id == serviceProviderId.Value && !sp.IsDeleted)
                .Select(sp => new { Exists = true, sp.Service.Type })
                .FirstOrDefaultAsync(cancellationToken) ?? throw new ArgumentException($"Service Provider with ID {serviceProviderId} not found");
            
            if (providerInfo.Type != ServiceTypeEnum.Wireless)
            {
                throw new ArgumentException($"Service Provider with ID {serviceProviderId} is not a wireless service provider. Only wireless providers are supported for cell site statistics.");
            }
        }

        // Validate region if specified (without materializing the boundary)
        if (regionId.HasValue)
        {
            var regionExists = await _context.Regions
                .AsNoTracking()
                .Where(r => r.Id == regionId.Value && !r.IsDeleted)
                .AnyAsync(cancellationToken);

            if (!regionExists)
            {
                throw new ArgumentException($"Region with ID {regionId} not found");
            }
        }

        // Build cell query with filters
        var cellQuery = _context.Cells
            .AsNoTracking()
            .Where(c => !c.IsDeleted && c.Location != null && c.ServiceProvider.Service.Type == ServiceTypeEnum.Wireless);

        if (serviceProviderId.HasValue)
            cellQuery = cellQuery.Where(c => c.ServiceProviderId == serviceProviderId.Value);

        if (startDate.HasValue)
            cellQuery = cellQuery.Where(c => c.CreatedAt >= startDate.Value);

        if (endDate.HasValue)
            cellQuery = cellQuery.Where(c => c.CreatedAt <= endDate.Value);

        // Apply spatial filter using subquery to leverage spatial indexes
        if (regionId.HasValue)
        {
            cellQuery = cellQuery.Where(c => 
                _context.Regions
                    .Where(r => r.Id == regionId.Value && !r.IsDeleted)
                    .Any(r => r.Boundaries != null && r.Boundaries.Intersects(c.Location))
            );
        }

        // Group by provider and get counts
        var providerCellCounts = await cellQuery
            .GroupBy(c => new {
                c.ServiceProviderId,
                ProviderName = c.ServiceProvider.Provider.Name,
                ProviderColor = c.ServiceProvider.Provider.Color
            })
            .Select(g => new CellSitesByProviderDto
            {
                ServiceProviderId = g.Key.ServiceProviderId,
                ProviderName = g.Key.ProviderName,
                ProviderColor = g.Key.ProviderColor,
                CellCount = g.Count(),
                Percentage = 0 // Calculated in-memory
            })
            .OrderByDescending(p => p.CellCount)
            .ToListAsync(cancellationToken);

        // Calculate percentages in-memory (faster than database calculation)
        var totalCellSites = providerCellCounts.Sum(p => p.CellCount);
        if (totalCellSites > 0)
        {
            foreach (var provider in providerCellCounts)
            {
                provider.Percentage = Math.Round((double)provider.CellCount / totalCellSites * 100, 2);
            }
        }

        return new CellSitesByProviderResponseDto
        {
            TotalCellSites = totalCellSites,
            Providers = providerCellCounts
        };
    }
}
