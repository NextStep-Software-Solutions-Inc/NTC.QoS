using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.Dashboard;

namespace NTC.QoS.Infrastructure.Persistence.Repositories;

internal sealed class RegionRepository(ApplicationDbContext context) : Repository<Region>(context), IRegionRepository
{
    /// <summary>
    /// Gets a list of regions ordered by the count of cells located within their boundaries.
    /// This method explicitly validates cell location within region boundaries using spatial functions
    /// and does NOT rely on a direct navigation property between Region and Cell.
    /// </summary>
    /// <param name="topCount">The maximum number of top regions to return. If 0 or negative, returns all regions.</param>
    /// <returns>A list of RegionCellCountDto representing regions and their cell counts.</returns>
    //public async Task<List<RegionCellCountDto>> GetTopRegionsByCellCountAsync(int topCount)
    //{
    //    //// This query is designed to work even without a direct navigation property
    //    //// between Region and Cell in your EF Core model.
    //    //// It directly queries the Cells DbSet (_context.Set<Cell>()) for each region.
    //    //var regionCellCounts = await _context.Set<Region>() // Access the Regions DbSet
    //    //    .Select(r => new
    //    //    {
    //    //        // Select the region data needed for the DTO
    //    //        RegionId = r.Id,
    //    //        RegionName = r.Name,
    //    //        RegionCode = r.Code,
    //    //        // Count cells where the cell's Location is Within the region's Boundaries.
    //    //        // The crucial part: _context.Set<Cell>() allows access to the Cell DbSet
    //    //        // independently of the Region entity's navigation properties.
    //    //        CellCount = _context.Set<Cell>()
    //    //                        .Count(c => c.Location != null && r.Boundaries != null && c.Location.Within(r.Boundaries))
    //    //    })
    //    //    // Order by cell count in descending order to get the "top" regions
    //    //    .OrderByDescending(x => x.CellCount)
    //    //    // Apply the 'topCount' limit efficiently. If topCount is 0 or less, it effectively takes all.
    //    //    .Take(topCount > 0 ? topCount : int.MaxValue)
    //    //    // Project the anonymous type result into your desired DTO
    //    //    .Select(x => new RegionCellCountDto
    //    //    {
    //    //        RegionId = x.RegionId,
    //    //        RegionName = x.RegionName,
    //    //        RegionCode = x.RegionCode,
    //    //        CellCount = x.CellCount
    //    //    })
    //    //    .ToListAsync();

    //    //return regionCellCounts;
    //}
}