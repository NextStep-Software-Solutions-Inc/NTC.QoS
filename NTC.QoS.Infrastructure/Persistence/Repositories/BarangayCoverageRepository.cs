using Microsoft.EntityFrameworkCore;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Interfaces;

namespace NTC.QoS.Infrastructure.Persistence.Repositories;

internal sealed class BarangayCoverageRepository(ApplicationDbContext context) 
    : Repository<BarangayCoverage>(context), IBarangayCoverageRepository
{
    public async Task<BarangayCoverage?> GetByBarangayIdAsync(Guid barangayId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<BarangayCoverage>()
            .Include(bc => bc.Barangay.Municipality.Province.Region)
            .AsSplitQuery()
            .Select(bc => new BarangayCoverage
            {
                Id = bc.Id,
                BarangayId = bc.BarangayId,
                WiredFtthPercentage = bc.WiredFtthPercentage,
                WirelessPercentage = bc.WirelessPercentage,
                CreatedAt = bc.CreatedAt,
                CreatedBy = bc.CreatedBy,
                UpdatedAt = bc.UpdatedAt,
                UpdatedBy = bc.UpdatedBy,
                Barangay = new Barangay
                {
                    Id = bc.Barangay.Id,
                    Name = bc.Barangay.Name,
                    Code = bc.Barangay.Code,
                    MunicipalityId = bc.Barangay.MunicipalityId,
                    Center = bc.Barangay.Center,
                    Area = bc.Barangay.Area,
                    TotalPopulation = bc.Barangay.TotalPopulation,
                    HouseholdPopulation = bc.Barangay.HouseholdPopulation,
                    HouseholdCount = bc.Barangay.HouseholdCount,
                    Municipality = new Municipality
                    {
                        Id = bc.Barangay.Municipality.Id,
                        Name = bc.Barangay.Municipality.Name,
                        Code = bc.Barangay.Municipality.Code,
                        IsCity = bc.Barangay.Municipality.IsCity,
                        ProvinceId = bc.Barangay.Municipality.ProvinceId,
                        Province = new Province
                        {
                            Id = bc.Barangay.Municipality.Province.Id,
                            Name = bc.Barangay.Municipality.Province.Name,
                            Code = bc.Barangay.Municipality.Province.Code,
                            RegionId = bc.Barangay.Municipality.Province.RegionId,
                            Region = new Region
                            {
                                Id = bc.Barangay.Municipality.Province.Region.Id,
                                Name = bc.Barangay.Municipality.Province.Region.Name,
                                Code = bc.Barangay.Municipality.Province.Region.Code
                            }
                        }
                    }
                }
            })
            .FirstOrDefaultAsync(bc => bc.BarangayId == barangayId, cancellationToken);
    }

    public async Task<List<BarangayCoverage>> GetByRegionIdAsync(Guid regionId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<BarangayCoverage>()
            .Include(bc => bc.Barangay.Municipality.Province)
            .AsSplitQuery()
            .Where(bc => bc.Barangay.Municipality.Province.RegionId == regionId)
            .Select(bc => new BarangayCoverage
            {
                Id = bc.Id,
                BarangayId = bc.BarangayId,
                WiredFtthPercentage = bc.WiredFtthPercentage,
                WirelessPercentage = bc.WirelessPercentage,
                CreatedAt = bc.CreatedAt,
                CreatedBy = bc.CreatedBy,
                UpdatedAt = bc.UpdatedAt,
                UpdatedBy = bc.UpdatedBy,
                Barangay = new Barangay
                {
                    Id = bc.Barangay.Id,
                    Name = bc.Barangay.Name,
                    Code = bc.Barangay.Code,
                    MunicipalityId = bc.Barangay.MunicipalityId,
                    Center = bc.Barangay.Center,
                    Area = bc.Barangay.Area,
                    TotalPopulation = bc.Barangay.TotalPopulation,
                    HouseholdPopulation = bc.Barangay.HouseholdPopulation,
                    HouseholdCount = bc.Barangay.HouseholdCount,
                    Municipality = new Municipality
                    {
                        Id = bc.Barangay.Municipality.Id,
                        Name = bc.Barangay.Municipality.Name,
                        Code = bc.Barangay.Municipality.Code,
                        IsCity = bc.Barangay.Municipality.IsCity,
                        ProvinceId = bc.Barangay.Municipality.ProvinceId,
                        Province = new Province
                        {
                            Id = bc.Barangay.Municipality.Province.Id,
                            Name = bc.Barangay.Municipality.Province.Name,
                            Code = bc.Barangay.Municipality.Province.Code,
                            RegionId = bc.Barangay.Municipality.Province.RegionId
                        }
                    }
                }
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<BarangayCoverage?> GetByIdWithRelationshipsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<BarangayCoverage>()
            .Include(bc => bc.Barangay.Municipality.Province.Region)
            .AsSplitQuery()
            .Select(bc => new BarangayCoverage
            {
                Id = bc.Id,
                BarangayId = bc.BarangayId,
                WiredFtthPercentage = bc.WiredFtthPercentage,
                WirelessPercentage = bc.WirelessPercentage,
                CreatedAt = bc.CreatedAt,
                CreatedBy = bc.CreatedBy,
                UpdatedAt = bc.UpdatedAt,
                UpdatedBy = bc.UpdatedBy,
                Barangay = new Barangay
                {
                    Id = bc.Barangay.Id,
                    Name = bc.Barangay.Name,
                    Code = bc.Barangay.Code,
                    MunicipalityId = bc.Barangay.MunicipalityId,
                    Center = bc.Barangay.Center,
                    Area = bc.Barangay.Area,
                    TotalPopulation = bc.Barangay.TotalPopulation,
                    HouseholdPopulation = bc.Barangay.HouseholdPopulation,
                    HouseholdCount = bc.Barangay.HouseholdCount,
                    Municipality = new Municipality
                    {
                        Id = bc.Barangay.Municipality.Id,
                        Name = bc.Barangay.Municipality.Name,
                        Code = bc.Barangay.Municipality.Code,
                        IsCity = bc.Barangay.Municipality.IsCity,
                        ProvinceId = bc.Barangay.Municipality.ProvinceId,
                        Province = new Province
                        {
                            Id = bc.Barangay.Municipality.Province.Id,
                            Name = bc.Barangay.Municipality.Province.Name,
                            Code = bc.Barangay.Municipality.Province.Code,
                            RegionId = bc.Barangay.Municipality.Province.RegionId,
                            Region = new Region
                            {
                                Id = bc.Barangay.Municipality.Province.Region.Id,
                                Name = bc.Barangay.Municipality.Province.Region.Name,
                                Code = bc.Barangay.Municipality.Province.Region.Code
                            }
                        }
                    }
                }
            })
            .FirstOrDefaultAsync(bc => bc.Id == id, cancellationToken);
    }

    public async Task<List<BarangayCoverage>> GetAllWithFiltersAsync(
        List<Guid>? regionIds = null,
        List<Guid>? provinceIds = null,
        List<Guid>? municipalityIds = null,
        List<Guid>? barangayIds = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Set<BarangayCoverage>()
            .Include(bc => bc.Barangay)
                .ThenInclude(b => b.Municipality)
                .ThenInclude(m => m.Province)
            .AsQueryable();

        // Apply filters
        if (barangayIds != null && barangayIds.Count > 0)
        {
            query = query.Where(bc => barangayIds.Contains(bc.BarangayId));
        }
        
        if (municipalityIds != null && municipalityIds.Count > 0)
        {
            query = query.Where(bc => municipalityIds.Contains(bc.Barangay.MunicipalityId));
        }
        
        if (provinceIds != null && provinceIds.Count > 0)
        {
            query = query.Where(bc => provinceIds.Contains(bc.Barangay.Municipality.ProvinceId));
        }
        
        if (regionIds != null && regionIds.Count > 0)
        {
            query = query.Where(bc => regionIds.Contains(bc.Barangay.Municipality.Province.RegionId));
        }

        // Select only the coverage data without relationships (lighter)
        return await query
            .Select(bc => new BarangayCoverage
            {
                Id = bc.Id,
                BarangayId = bc.BarangayId,
                WiredFtthPercentage = bc.WiredFtthPercentage,
                WirelessPercentage = bc.WirelessPercentage,
                CreatedAt = bc.CreatedAt,
                CreatedBy = bc.CreatedBy,
                UpdatedAt = bc.UpdatedAt,
                UpdatedBy = bc.UpdatedBy
            })
            .ToListAsync(cancellationToken);
    }
}
