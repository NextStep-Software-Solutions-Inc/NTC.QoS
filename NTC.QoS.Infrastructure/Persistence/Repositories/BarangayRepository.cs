using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;

namespace NTC.QoS.Infrastructure.Persistence.Repositories;

internal sealed class BarangayRepository(ApplicationDbContext context) : Repository<Barangay>(context), IBarangayRepository
{
    /// <summary>
    /// Finds barangay by geographic location using spatial Contains operation.
    /// OPTIMIZED: Uses projection to query only needed fields instead of loading full entity graph.
    /// </summary>
    public async Task<Barangay?> FindByLocationAsync(double latitude, double longitude)
    {
        var point = GeometryHelper.CreatePoint(new Coordinate(longitude, latitude));

        // Use projection to select only needed data - EF Core generates efficient LEFT JOIN SQL
        var barangayQuery = _context.Set<Barangay>()
            .Where(b => b.Boundaries != null && b.Boundaries.Intersects(point))
            .Select(b => new
            {
                b.Id,
                b.Name,
                b.Description,
                b.Code,
                b.Area,
                b.Center,
                b.Boundaries,
                b.TotalPopulation,
                b.HouseholdPopulation,
                b.HouseholdCount,
                b.MunicipalityId,
                Municipality = b.Municipality != null ? new
                {
                    b.Municipality.Id,
                    b.Municipality.Name,
                    b.Municipality.Description,
                    b.Municipality.IsCity,
                    b.Municipality.ProvinceId,
                    Province = b.Municipality.Province != null ? new
                    {
                        b.Municipality.Province.Id,
                        b.Municipality.Province.Name,
                        b.Municipality.Province.Description,
                        b.Municipality.Province.RegionId,
                        Region = b.Municipality.Province.Region != null ? new
                        {
                            b.Municipality.Province.Region.Id,
                            b.Municipality.Province.Region.Name,
                            b.Municipality.Province.Region.Description
                        } : null
                    } : null
                } : null
            });

        var result = await barangayQuery.FirstOrDefaultAsync();

        if (result == null)
        {
            return null;
        }

        // Reconstruct Barangay entity from projected data
        var barangay = new Barangay
        {
            Id = result.Id,
            Name = result.Name,
            Description = result.Description,
            Code = result.Code,
            Area = result.Area,
            Center = result.Center,
            Boundaries = result.Boundaries,
            TotalPopulation = result.TotalPopulation,
            HouseholdPopulation = result.HouseholdPopulation,
            HouseholdCount = result.HouseholdCount,
            MunicipalityId = result.MunicipalityId
        };

        // Reconstruct navigation properties if they exist
        if (result.Municipality != null)
        {
            barangay.Municipality = new Municipality
            {
                Id = result.Municipality.Id,
                Name = result.Municipality.Name,
                Description = result.Municipality.Description,
                IsCity = result.Municipality.IsCity,
                ProvinceId = result.Municipality.ProvinceId
            };

            if (result.Municipality.Province != null)
            {
                barangay.Municipality.Province = new Province
                {
                    Id = result.Municipality.Province.Id,
                    Name = result.Municipality.Province.Name,
                    Description = result.Municipality.Province.Description,
                    RegionId = result.Municipality.Province.RegionId
                };

                if (result.Municipality.Province.Region != null)
                {
                    barangay.Municipality.Province.Region = new Region
                    {
                        Id = result.Municipality.Province.Region.Id,
                        Name = result.Municipality.Province.Region.Name,
                        Description = result.Municipality.Province.Region.Description
                    };
                }
            }
        }

        return barangay;
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
