using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using System.Linq.Expressions;

namespace NTC.QoS.Infrastructure.Persistence.Repositories;

internal sealed class BarangayRepository(ApplicationDbContext context) : Repository<Barangay>(context), IBarangayRepository
{
    public override IQueryable<Barangay> Query(Expression<Func<Barangay, bool>>? expression = null)
    {
        var query = _context.Set<Barangay>()
            .Include(b => b.Municipality)
                .ThenInclude(m => m.Province)
                    .ThenInclude(p => p.Region)
            .AsQueryable();

        if (expression != null)
        {
            query = query.Where(expression);
        }

        return query;
    }

    public async Task<Barangay?> FindByLocationAsync(double latitude, double longitude)
    {
        var point = GeometryHelper.CreatePoint(new Coordinate(longitude, latitude));

        return await _context.Set<Barangay>()
            .Include(b => b.Municipality)
                .ThenInclude(m => m.Province)
                    .ThenInclude(p => p.Region)
            .Where(b => b.Boundaries != null && b.Boundaries.Contains(point))
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// Applies case-insensitive search filter on barangay name using EF.Functions.ILike
    /// </summary>
    public IQueryable<Barangay> SearchByName(IQueryable<Barangay> query, string name)
    {
        return query.Where(b => EF.Functions.ILike(b.Name, $"%{name}%"));
    }

    /// <summary>
    /// Applies case-insensitive search filter on municipality name using EF.Functions.ILike
    /// </summary>
    public IQueryable<Barangay> SearchByMunicipalityName(IQueryable<Barangay> query, string municipalityName)
    {
        return query.Where(b => EF.Functions.ILike(b.Municipality.Name, $"%{municipalityName}%"));
    }

    /// <summary>
    /// Applies case-insensitive search filter on province name using EF.Functions.ILike
    /// </summary>
    public IQueryable<Barangay> SearchByProvinceName(IQueryable<Barangay> query, string provinceName)
    {
        return query.Where(b => EF.Functions.ILike(b.Municipality.Province.Name, $"%{provinceName}%"));
    }
}
