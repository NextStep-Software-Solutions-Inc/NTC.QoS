using Microsoft.EntityFrameworkCore;
using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.GeoEntity;

namespace NTC.QoS.Infrastructure.Persistence.Repositories;

internal sealed class ProvinceRepository(ApplicationDbContext context) : Repository<Province>(context), IProvinceRepository
{
    public async Task<Dictionary<Guid, int>> GetIspCountsForProvincesAsync(List<Guid> provinceIds, CancellationToken cancellationToken = default)
    {
        var result = await _context.Set<Province>()
            .Where(p => provinceIds.Contains(p.Id))
            .Select(p => new
            {
                ProvinceId = p.Id,
                IspCount = _context.Set<Cell>()
                    .Where(c => c.Location.Intersects(p.Boundaries))
                    .Select(c => c.ServiceProviderId)
                    .Concat(
                        _context.Set<NAP>()
                            .Where(n => n.Location.Intersects(p.Boundaries))
                            .Select(n => n.ServiceProviderId)
                    )
                    .Distinct()
                    .Count()
            })
            .ToDictionaryAsync(x => x.ProvinceId, x => x.IspCount, cancellationToken);

        return result;
    }

    /// <summary>
    /// Gets top provinces by cell count with optimized spatial queries.
    /// OPTIMIZED: Uses materialized cell count to avoid multiple ST_Intersects operations.
    /// </summary>
    public async Task<List<GeoEntityCellCountDto>> GetTopProvincesByCellCountAsync(
        Guid? regionId,
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

        // Build province query with optional parent filtering
        var provinceQuery = _context.Set<Province>()
            .AsNoTracking()
            .Where(p => p.Boundaries != null);

        if (regionId.HasValue)
        {
            provinceQuery = provinceQuery.Where(p => p.RegionId == regionId.Value);
        }

        // CRITICAL FIX: Materialize intermediate result to calculate CellCount only ONCE
        var provinceCellCounts = await provinceQuery
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Code,
                CellCount = cellsQuery.Count(c => p.Boundaries!.Intersects(c.Location))
            })
            .ToListAsync(cancellationToken);

        // Filter, sort, and project in-memory
        var results = provinceCellCounts
            .Where(p => p.CellCount > 0)
            .OrderByDescending(p => p.CellCount)
            .Take(limit > 0 ? limit : int.MaxValue)
            .Select(p => new GeoEntityCellCountDto
            {
                Id = p.Id,
                Name = p.Name,
                Code = p.Code,
                EntityType = nameof(Province),
                CellCount = p.CellCount
            })
            .ToList();

        return results;
    }
}
