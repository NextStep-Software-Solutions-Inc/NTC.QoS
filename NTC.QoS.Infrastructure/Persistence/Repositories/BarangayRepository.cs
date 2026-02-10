using Microsoft.EntityFrameworkCore;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.GeoEntity;
using System.Linq.Expressions;
using NetTopologySuite.Geometries;

namespace NTC.QoS.Infrastructure.Persistence.Repositories;

internal sealed class BarangayRepository(ApplicationDbContext context) : Repository<Barangay>(context), IBarangayRepository
{
    public IQueryable<Barangay> SearchByName(IQueryable<Barangay> query, string name)
    {
        return ApplySearch(query, name, b => b.Name);
    }

    public IQueryable<Barangay> SearchByMunicipalityName(IQueryable<Barangay> query, string municipalityName)
    {
        return ApplySearch(query, municipalityName, b => b.Municipality.Name);
    }

    public IQueryable<Barangay> SearchByProvinceName(IQueryable<Barangay> query, string provinceName)
    {
        return ApplySearch(query, provinceName, b => b.Municipality.Province.Name);
    }

    /// <summary>
    /// Gets top barangays by cell count with optimized spatial queries.
    /// OPTIMIZED: Uses materialized cell count to avoid multiple ST_Intersects operations.
    /// </summary>
    public async Task<List<GeoEntityCellCountDto>> GetTopBarangaysByCellCountAsync(
        Guid? municipalityId,
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

        // Build barangay query with optional parent filtering
        var barangayQuery = _context.Set<Barangay>()
            .AsNoTracking()
            .Where(b => b.Boundaries != null);

        if (municipalityId.HasValue)
        {
            barangayQuery = barangayQuery.Where(b => b.MunicipalityId == municipalityId.Value);
        }

        // CRITICAL FIX: Materialize intermediate result to calculate CellCount only ONCE
        var barangayCellCounts = await barangayQuery
            .Select(b => new
            {
                b.Id,
                b.Name,
                b.Code,
                CellCount = cellsQuery.Count(c => b.Boundaries!.Intersects(c.Location))
            })
            .ToListAsync(cancellationToken);

        // Filter, sort, and project in-memory
        var results = barangayCellCounts
            .Where(b => b.CellCount > 0)
            .OrderByDescending(b => b.CellCount)
            .Take(limit > 0 ? limit : int.MaxValue)
            .Select(b => new GeoEntityCellCountDto
            {
                Id = b.Id,
                Name = b.Name,
                Code = b.Code ?? string.Empty,
                EntityType = nameof(Barangay),
                CellCount = b.CellCount
            })
            .ToList();

        return results;
    }

    /// <summary>
    /// Finds a barangay containing the specified point location with its full hierarchy loaded.
    /// </summary>
    public async Task<Barangay?> FindBarangayByLocationWithHierarchyAsync(Point location, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Barangay>()
            .AsNoTracking()
            .Include(b => b.Municipality)
                .ThenInclude(m => m.Province)
                    .ThenInclude(p => p.Region)
            .Where(b => b.Boundaries != null && b.Boundaries.Intersects(location))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
