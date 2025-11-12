using Microsoft.EntityFrameworkCore;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Interfaces;

namespace NTC.QoS.Infrastructure.Persistence.Repositories;

internal sealed class ZoneRepository(ApplicationDbContext context) : Repository<Zone>(context), IZoneRepository
{
    /// <summary>
    /// Asynchronously finds a Zone based on optional geographical names.
    /// OPTIMIZED: Uses projection to avoid loading full navigation property graphs.
    /// </summary>
    public async Task<Zone?> FindByNameAsync(string? municipalityName, string? provinceName, string? regionName, CancellationToken cancellationToken = default)
    {
        var query = BuildFilteredQuery(municipalityName, provinceName, regionName, applyAllFilters: true);
        return await ExecuteProjectionQueryAsync(query, cancellationToken);
    }

    /// <summary>
    /// Asynchronously finds the first Zone that matches based on the first provided (non-null) geographical name.
    /// OPTIMIZED: Uses projection to avoid loading full navigation property graphs.
    /// </summary>
    public async Task<Zone?> FindFirstAnyMatchAsync(string? municipalityName, string? provinceName, string? regionName, CancellationToken cancellationToken = default)
    {
        var query = BuildFilteredQuery(municipalityName, provinceName, regionName, applyAllFilters: false);
        return await ExecuteProjectionQueryAsync(query, cancellationToken);
    }

    /// <summary>
    /// Builds the filtered query based on geographical name parameters.
    /// </summary>
    private IQueryable<Zone> BuildFilteredQuery(string? municipalityName, string? provinceName, string? regionName, bool applyAllFilters)
    {
        IQueryable<Zone> query = _context.Set<Zone>()
            .Where(z => !EF.Property<bool>(z, "IsDeleted"));

        if (applyAllFilters)
        {
            // Apply all filters (AND logic)
            query = ApplySearch(query, municipalityName,
                z => z.Barangay!.Municipality.Name,
                z => z.Barangay!.Municipality.Description);

            query = ApplySearch(query, provinceName,
                z => z.Barangay!.Municipality.Province.Name,
                z => z.Barangay!.Municipality.Province.Description);

            query = ApplySearch(query, regionName,
                z => z.Barangay!.Municipality.Province.Region.Name,
                z => z.Barangay!.Municipality.Province.Region.Description);
        }
        else
        {
            // Apply first non-null filter only (OR logic with priority)
            if (!string.IsNullOrEmpty(municipalityName))
            {
                query = ApplySearch(query, municipalityName,
                    z => z.Barangay!.Municipality.Name,
                    z => z.Barangay!.Municipality.Description);
            }
            else if (!string.IsNullOrEmpty(provinceName))
            {
                query = ApplySearch(query, provinceName,
                    z => z.Barangay!.Municipality.Province.Name,
                    z => z.Barangay!.Municipality.Province.Description);
            }
            else if (!string.IsNullOrEmpty(regionName))
            {
                query = ApplySearch(query, regionName,
                    z => z.Barangay!.Municipality.Province.Region.Name,
                    z => z.Barangay!.Municipality.Province.Region.Description);
            }
        }

        return query;
    }

    /// <summary>
    /// Executes the projection query and reconstructs the Zone entity with navigation properties.
    /// </summary>
    private async Task<Zone?> ExecuteProjectionQueryAsync(IQueryable<Zone> query, CancellationToken cancellationToken)
    {
        var result = await query
            .Select(z => new
            {
                z.Id,
                z.Name,
                z.BarangayId,
                Barangay = z.Barangay != null ? new
                {
                    z.Barangay.Id,
                    z.Barangay.Name,
                    z.Barangay.Description,
                    z.Barangay.MunicipalityId,
                    Municipality = z.Barangay.Municipality != null ? new
                    {
                        z.Barangay.Municipality.Id,
                        z.Barangay.Municipality.Name,
                        z.Barangay.Municipality.Description,
                        z.Barangay.Municipality.IsCity,
                        z.Barangay.Municipality.ProvinceId,
                        Province = z.Barangay.Municipality.Province != null ? new
                        {
                            z.Barangay.Municipality.Province.Id,
                            z.Barangay.Municipality.Province.Name,
                            z.Barangay.Municipality.Province.Description,
                            z.Barangay.Municipality.Province.RegionId,
                            Region = z.Barangay.Municipality.Province.Region != null ? new
                            {
                                z.Barangay.Municipality.Province.Region.Id,
                                z.Barangay.Municipality.Province.Region.Name,
                                z.Barangay.Municipality.Province.Region.Description
                            } : null
                        } : null
                    } : null
                } : null
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (result == null)
        {
            return null;
        }

        return ReconstructZoneEntity(result);
    }

    /// <summary>
    /// Reconstructs a Zone entity from projected anonymous type data.
    /// </summary>
    private static Zone ReconstructZoneEntity(dynamic result)
    {
        var zone = new Zone
        {
            Id = result.Id,
            Name = result.Name,
            BarangayId = result.BarangayId
        };

        if (result.Barangay != null)
        {
            zone.Barangay = new Barangay
            {
                Id = result.Barangay.Id,
                Name = result.Barangay.Name,
                Description = result.Barangay.Description,
                MunicipalityId = result.Barangay.MunicipalityId
            };

            if (result.Barangay.Municipality != null)
            {
                zone.Barangay.Municipality = new Municipality
                {
                    Id = result.Barangay.Municipality.Id,
                    Name = result.Barangay.Municipality.Name,
                    Description = result.Barangay.Municipality.Description,
                    IsCity = result.Barangay.Municipality.IsCity,
                    ProvinceId = result.Barangay.Municipality.ProvinceId
                };

                if (result.Barangay.Municipality.Province != null)
                {
                    zone.Barangay.Municipality.Province = new Province
                    {
                        Id = result.Barangay.Municipality.Province.Id,
                        Name = result.Barangay.Municipality.Province.Name,
                        Description = result.Barangay.Municipality.Province.Description,
                        RegionId = result.Barangay.Municipality.Province.RegionId
                    };

                    if (result.Barangay.Municipality.Province.Region != null)
                    {
                        zone.Barangay.Municipality.Province.Region = new Region
                        {
                            Id = result.Barangay.Municipality.Province.Region.Id,
                            Name = result.Barangay.Municipality.Province.Region.Name,
                            Description = result.Barangay.Municipality.Province.Region.Description
                        };
                    }
                }
            }
        }

        return zone;
    }
}
