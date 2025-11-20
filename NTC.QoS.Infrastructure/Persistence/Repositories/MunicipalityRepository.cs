using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.GeoEntity;
using System.Linq.Expressions;

namespace NTC.QoS.Infrastructure.Persistence.Repositories;

internal sealed class MunicipalityRepository(ApplicationDbContext context) : Repository<Municipality>(context), IMunicipalityRepository
{
    public override IQueryable<Municipality> Query(Expression<Func<Municipality, bool>>? expression = null)
    {
        var query = _context.Set<Municipality>()
            .Include(m => m.Province)
            .ThenInclude(p => p.Region)
            .AsQueryable();

        if (expression != null)
        {
            query = query.Where(expression);
        }

        return query;

    }

    public async Task<Municipality?> GetMunicipalityByLocationAsync(Point location, CancellationToken cancellationToken = default)
    {
        if (location == null)
            return null;

        return await Query(m => m.Boundaries != null && m.Boundaries.Intersects(location))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<(Guid? Id, string Name)> GetMunicipalityIdAndNameByLocationAsync(Point location, CancellationToken cancellationToken = default)
    {
        if (location == null)
            return (null, "Unknown");

        var municipality = await Query(m => m.Boundaries != null && m.Boundaries.Intersects(location))
            .Select(m => new { m.Id, m.Name })
            .FirstOrDefaultAsync(cancellationToken);

        return municipality != null
            ? (municipality.Id, municipality.Name)
            : (null, "Unknown");
    }

    /// <summary>
    /// Gets top municipalities by cell count with optimized spatial queries.
    /// OPTIMIZED: Uses materialized cell count to avoid multiple ST_Intersects operations.
    /// </summary>
    public async Task<List<GeoEntityCellCountDto>> GetTopMunicipalitiesByCellCountAsync(
        Guid? provinceId,
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

        // Build municipality query with optional parent filtering
        var municipalityQuery = _context.Set<Municipality>()
            .AsNoTracking()
            .Where(m => m.Boundaries != null);

        if (provinceId.HasValue)
        {
            municipalityQuery = municipalityQuery.Where(m => m.ProvinceId == provinceId.Value);
        }

        // CRITICAL FIX: Materialize intermediate result to calculate CellCount only ONCE
        var municipalityCellCounts = await municipalityQuery
            .Select(m => new
            {
                m.Id,
                m.Name,
                m.Code,
                CellCount = cellsQuery.Count(c => m.Boundaries!.Intersects(c.Location))
            })
            .ToListAsync(cancellationToken);

        // Filter, sort, and project in-memory
        var results = municipalityCellCounts
            .Where(m => m.CellCount > 0)
            .OrderByDescending(m => m.CellCount)
            .Take(limit > 0 ? limit : int.MaxValue)
            .Select(m => new GeoEntityCellCountDto
            {
                Id = m.Id,
                Name = m.Name,
                Code = m.Code,
                EntityType = nameof(Municipality),
                CellCount = m.CellCount
            })
            .ToList();

        return results;
    }

    /// <summary>
    /// Gets the actual count of child locations (barangays) for a municipality.
    /// Optimized for accurate progress tracking in connectivity assessment jobs.
    /// </summary>
    public async Task<int> GetChildLocationCountAsync(Guid municipalityId, CancellationToken cancellationToken = default)
    {
        // Municipalities only have barangays as children
        var barangayCount = await _context.Set<Barangay>()
            .AsNoTracking()
            .Where(b => b.MunicipalityId == municipalityId)
            .CountAsync(cancellationToken);

        return barangayCount;
    }
}
