using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Interfaces;

namespace NTC.QoS.Application.Services;

/// <summary>
/// Service responsible for managing user-region assignments.
/// </summary>
public class UserRegionService(
    IUserRegionRepository userRegionRepository,
    IRepository<Region> regionRepository) : IUserRegionService
{
    private readonly IUserRegionRepository _userRegionRepository = userRegionRepository;
    private readonly IRepository<Region> _regionRepository = regionRepository;

    public async Task AssignAllRegionsToUserAsync(Guid userId, Guid? roleId = null, CancellationToken cancellationToken = default)
    {
        var allRegionsQuery = _regionRepository.Query(r => !r.IsDeleted);
        var allRegions = await _regionRepository.ToListAsync(allRegionsQuery, cancellationToken);

        if (allRegions.Count == 0) return;

        var existingUserRegionsQuery = _userRegionRepository.Query(ur => ur.UserId == userId && !ur.IsDeleted);
        var existingUserRegions = await _userRegionRepository.ToListAsync(existingUserRegionsQuery, cancellationToken);

        var hasUpdates = false;
        foreach (var existing in existingUserRegions)
        {
            var updated = false;
            if (existing.Status != UserRegionStatus.Active.ToString())
            {
                existing.Status = UserRegionStatus.Active.ToString();
                updated = true;
            }

            if (roleId.HasValue && existing.RoleId != roleId.Value)
            {
                existing.RoleId = roleId.Value;
                updated = true;
            }

            if (updated)
            {
                _userRegionRepository.Update(existing);
                hasUpdates = true;
            }
        }

        var existingRegionIds = existingUserRegions.Select(ur => ur.RegionId).ToHashSet();
        var missingRegions = allRegions.Where(r => !existingRegionIds.Contains(r.Id)).ToList();

        if (missingRegions.Count > 0)
        {
            var newUserRegions = missingRegions.Select(region => new UserRegion
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                RegionId = region.Id,
                RoleId = roleId,
                Status = UserRegionStatus.Active.ToString(),
            }).ToList();

            _userRegionRepository.AddRange(newUserRegions);
            hasUpdates = true;
        }

        if (hasUpdates)
        {
            await _userRegionRepository.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task DeleteAllRegionsForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var existingUserRegionsQuery = _userRegionRepository.Query(ur => ur.UserId == userId);
        var existingUserRegions = await _userRegionRepository.ToListAsync(existingUserRegionsQuery, cancellationToken);

        if (existingUserRegions.Count > 0)
        {
            _userRegionRepository.RemoveRange(existingUserRegions);
            await _userRegionRepository.SaveChangesAsync(cancellationToken);
        }
    }
}
