using Microsoft.EntityFrameworkCore;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Interfaces;

namespace NTC.QoS.Infrastructure.Persistence.Repositories;

internal sealed class ZoneRepository(ApplicationDbContext context) : Repository<Zone>(context), IZoneRepository
{
    /// <summary>
    /// Asynchronously finds a Zone based on optional geographical names.
    /// </summary>
    /// <param name="municipalityName">The municipality name or description to search for (case-insensitive).</param>
    /// <param name="provinceName">The province name or description to search for (case-insensitive).</param>
    /// <param name="regionName">The region name or description to search for (case-insensitive).</param>
    /// <param name="cancellationToken">A CancellationToken to observe while waiting for the task to complete.</param>
    /// <returns>The first Zone that matches all provided non-null criteria, including related geographical data, or null if no match is found.</returns>
    public async Task<Zone?> FindByNameAsync(string? municipalityName, string? provinceName, string? regionName, CancellationToken cancellationToken = default)
    {
        IQueryable<Zone> query = _context.Set<Zone>()
            .Where(z => !EF.Property<bool>(z, "IsDeleted"))
            .Include(z => z.Barangay)
                .ThenInclude(b => b.Municipality)
                    .ThenInclude(m => m.Province)
                        .ThenInclude(p => p.Region);

        if (!string.IsNullOrEmpty(municipalityName))
        {
            query = query.Where(z =>    
                z.Barangay != null &&
                z.Barangay.Municipality != null &&
                (
                    (!string.IsNullOrEmpty(z.Barangay.Municipality.Name) && z.Barangay.Municipality.Name.ToLower().Contains(municipalityName.ToLower())) ||
                    (!string.IsNullOrEmpty(z.Barangay.Municipality.Description) && z.Barangay.Municipality.Description!.ToLower().Contains(municipalityName.ToLower()))
                ));
        }

        if (!string.IsNullOrEmpty(provinceName))
        {
            query = query.Where(z =>
                z.Barangay != null &&
                z.Barangay.Municipality != null &&
                z.Barangay.Municipality.Province != null &&
                (
                    (!string.IsNullOrEmpty(z.Barangay.Municipality.Province.Name) && z.Barangay.Municipality.Province.Name.ToLower().Contains(provinceName.ToLower())) ||
                    (!string.IsNullOrEmpty(z.Barangay.Municipality.Province.Description) && z.Barangay.Municipality.Province.Description!.ToLower().Contains(provinceName.ToLower()))
                ));
        }

        if (!string.IsNullOrEmpty(regionName))
        {
            query = query.Where(z =>
                z.Barangay != null &&
                z.Barangay.Municipality != null &&
                z.Barangay.Municipality.Province != null &&
                z.Barangay.Municipality.Province.Region != null &&
                (
                    (!string.IsNullOrEmpty(z.Barangay.Municipality.Province.Region.Name) &&
                     z.Barangay.Municipality.Province.Region.Name!.ToLower().Contains(regionName.ToLower())) ||

                    (!string.IsNullOrEmpty(z.Barangay.Municipality.Province.Region.Description) &&
                     z.Barangay.Municipality.Province.Region.Description!.Contains(regionName, StringComparison.CurrentCultureIgnoreCase))
            ));
        }

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Asynchronously finds the first Zone that matches based on the first provided (non-null) geographical name.
    /// </summary>
    /// <param name="municipalityName">The municipality name or description to search for (case-insensitive).</param>
    /// <param name="provinceName">The province name or description to search for (case-insensitive) if municipalityName is null.</param>
    /// <param name="regionName">The region name or description to search for (case-insensitive) if both municipalityName and provinceName are null.</param>
    /// <param name="cancellationToken">A CancellationToken to observe while waiting for the task to complete.</param>
    /// <returns>The first Zone that matches the first provided non-null criteria, including related geographical data, or null if no match is found.</returns>
    public async Task<Zone?> FindFirstAnyMatchAsync(string? municipalityName, string? provinceName, string? regionName, CancellationToken cancellationToken = default)
    {
        IQueryable<Zone> query = _context.Set<Zone>()
            .Where(z => !EF.Property<bool>(z, "IsDeleted"))
            .Include(z => z.Barangay)
                .ThenInclude(b => b.Municipality)
                    .ThenInclude(m => m.Province)
                        .ThenInclude(p => p.Region);

        if (!string.IsNullOrEmpty(municipalityName))
        {
            query = query.Where(z =>
                z.Barangay != null &&
                z.Barangay.Municipality != null &&
                (
                    (!string.IsNullOrEmpty(z.Barangay.Municipality.Name) && z.Barangay.Municipality.Name.ToLower().Contains(municipalityName.ToLower())) ||
                    (!string.IsNullOrEmpty(z.Barangay.Municipality.Description) && z.Barangay.Municipality.Description!.ToLower().Contains(municipalityName.ToLower()))
                ));
        }
        else if (!string.IsNullOrEmpty(provinceName))
        {
            query = query.Where(z =>
                z.Barangay != null &&
                z.Barangay.Municipality != null &&
                z.Barangay.Municipality.Province != null &&
                (
                    (!string.IsNullOrEmpty(z.Barangay.Municipality.Province.Name) && z.Barangay.Municipality.Province.Name.ToLower().Contains(provinceName.ToLower())) ||
                    (!string.IsNullOrEmpty(z.Barangay.Municipality.Province.Description) && z.Barangay.Municipality.Province.Description!.ToLower().Contains(provinceName.ToLower()))
                ));
        }
        else if (!string.IsNullOrEmpty(regionName))
        {
            query = query.Where(z =>
                z.Barangay != null &&
                z.Barangay.Municipality != null &&
                z.Barangay.Municipality.Province != null &&
                z.Barangay.Municipality.Province.Region != null &&
                (
                    (!string.IsNullOrEmpty(z.Barangay.Municipality.Province.Region.Name) &&
                     z.Barangay.Municipality.Province.Region.Name!.ToLower().Contains(regionName.ToLower())) ||

                    (!string.IsNullOrEmpty(z.Barangay.Municipality.Province.Region.Description) &&
                     z.Barangay.Municipality.Province.Region.Description!.Contains(regionName, StringComparison.CurrentCultureIgnoreCase))
            ));
        }

        return await query.FirstOrDefaultAsync(cancellationToken);
    }
}
