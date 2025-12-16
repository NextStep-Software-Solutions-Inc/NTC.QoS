using Microsoft.EntityFrameworkCore;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.GeoEntity;

namespace NTC.QoS.Infrastructure.Persistence.Repositories;

internal sealed class RegionRepository(ApplicationDbContext context) : Repository<Region>(context), IRegionRepository
{
    /// <summary>
    /// Gets top regions by cell count with optimized spatial queries.
    /// OPTIMIZED: Uses materialized cell count to avoid multiple ST_Intersects operations.
    /// </summary>
    public async Task<List<GeoEntityCellCountDto>> GetTopRegionsByCellCountAsync(
        int limit,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        // Build reusable cell query
        var cellsQuery = _context.Cells
            .AsNoTracking()
            .Where(c => !c.IsDeleted && c.Location != null);

        if (startDate.HasValue)
            cellsQuery = cellsQuery.Where(c => c.CreatedAt >= startDate.Value);

        if (endDate.HasValue)
            cellsQuery = cellsQuery.Where(c => c.CreatedAt <= endDate.Value);

        // CRITICAL FIX: Materialize the intermediate result to calculate CellCount only ONCE
        // This prevents EF Core from repeating the expensive ST_Intersects operation in SELECT, WHERE, and ORDER BY
        var regionCellCounts = await _context.Set<Region>()
            .AsNoTracking()
            .Where(r => r.Boundaries != null)
            .Select(r => new
            {
                r.Id,
                r.Name,
                r.Code,
                CellCount = cellsQuery.Count(c => r.Boundaries!.Intersects(c.Location))
            })
            .ToListAsync(cancellationToken); // Materialize to avoid re-evaluation

        // Now filter, sort, and project in-memory (much faster than re-running spatial queries)
        var results = regionCellCounts
            .Where(r => r.CellCount > 0)
            .OrderByDescending(r => r.CellCount)
            .Take(limit > 0 ? limit : int.MaxValue)
            .Select(r => new GeoEntityCellCountDto
            {
                Id = r.Id,
                Name = r.Name,
                Code = r.Code,
                EntityType = nameof(Region),
                CellCount = r.CellCount
            })
            .ToList();

        return results;
    }

    /// <summary>
    /// Gets the actual count of child locations for a region based on hierarchical depth.
    /// Optimized for accurate progress tracking in connectivity assessment jobs.
    /// </summary>
    public async Task<int> GetChildLocationCountAsync(Guid regionId, int hierarchicalDepth, CancellationToken cancellationToken = default)
    {
        int totalCount = 0;

        // Depth 1: Provinces only
        var provinceCount = await _context.Set<Province>()
            .AsNoTracking()
            .Where(p => p.RegionId == regionId)
            .CountAsync(cancellationToken);

        totalCount += provinceCount;

        if (hierarchicalDepth >= 2)
        {
            // Depth 2: Add municipalities under all provinces in this region
            var municipalityCount = await _context.Set<Municipality>()
                .AsNoTracking()
                .Where(m => m.Province.RegionId == regionId)
                .CountAsync(cancellationToken);

            totalCount += municipalityCount;
        }

        if (hierarchicalDepth >= 3)
        {
            // Depth 3: Add barangays under all municipalities in all provinces in this region
            var barangayCount = await _context.Set<Barangay>()
                .AsNoTracking()
                .Where(b => b.Municipality.Province.RegionId == regionId)
                .CountAsync(cancellationToken);

            totalCount += barangayCount;
        }

        return totalCount;
    }
}