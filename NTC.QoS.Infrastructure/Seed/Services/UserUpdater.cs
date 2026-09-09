using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Infrastructure.Persistence;
using NTC.QoS.Infrastructure.Seed.Interfaces;

namespace NTC.QoS.Infrastructure.Seed.Services;

public class UserUpdater : IUserUpdater
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<UserUpdater> _logger;

    public UserUpdater(ApplicationDbContext context, ILogger<UserUpdater> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task UpdateExistingUserAsync(User existingUser, ValidatedConfiguration config, string password)
    {
        _logger.LogInformation("Updating existing admin user with Clerk integration for email: {Email}...", config.Email);

        var role = await _context.Roles
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Key == "super_admin");
        var organization = await _context.Organizations
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Name.ToLower() == "national");

        string authUserId;
        var existingClerkUsers = await config.ClerkUserService.GetUsersByEmailAsync(config.Email);
        if (existingClerkUsers.Count > 0)
        {
            authUserId = existingClerkUsers[0].Id;
            var metadataPayload = new UserMetadataPayload
            {
                PublicMetadata = new Dictionary<string, object>
                {
                    { "role", role?.Key ?? "super_admin" },
                    { "organization", organization?.Name ?? "national" },
                    { "isDefaultAdmin", true }
                }
            };
            await config.ClerkUserService.UpdateUserMetadataAsync(authUserId, metadataPayload);
        }
        else
        {
            var clerkUserPayload = new CreateClerkUserPayload
            {
                EmailAddress = config.Email,
                FirstName = config.FirstName,
                LastName = config.LastName,
                Password = password,
                PublicMetadata = new Dictionary<string, object>
                {
                    { "role", role?.Key ?? "super_admin" },
                    { "organization", organization?.Name ?? "national" },
                    { "isDefaultAdmin", true }
                },
                PrivateMetadata = new Dictionary<string, object>
                {
                    { "created_by_seeder", true },
                    { "updated_from_placeholder", true },
                    { "updated_at", DateTime.UtcNow.ToString("O") }
                }
            };

            var clerkUser = await config.ClerkUserService.CreateUserAsync(clerkUserPayload);
            authUserId = clerkUser.Id;
        }

        var userToUpdate = await _context.Users.FirstOrDefaultAsync(u => u.Id == existingUser.Id);
        if (userToUpdate != null)
        {
            var oldAuthUserId = userToUpdate.AuthUserId;
            userToUpdate.AuthUserId = authUserId;
            userToUpdate.Email = config.Email;
            userToUpdate.FirstName = config.FirstName;
            userToUpdate.LastName = config.LastName;
            if (role != null) userToUpdate.RoleId = role.Id;
            if (organization != null) userToUpdate.OrganizationId = organization.Id;
            userToUpdate.UpdatedAt = DateTime.UtcNow;
            userToUpdate.UpdatedBy = "System";

            await _context.SaveChangesAsync();

            _logger.LogInformation("Successfully updated admin user AuthUserId from {OldAuthUserId} to {NewAuthUserId} for email: {Email}",
                oldAuthUserId, authUserId, config.Email);

            // Auto-assign active regions to superadmin
            await AssignAllRegionsToUserAsync(userToUpdate.Id);
        }
    }

    private async Task AssignAllRegionsToUserAsync(Guid userId)
    {
        var allRegions = await _context.Regions
            .AsNoTracking()
            .Where(r => !r.IsDeleted)
            .Select(r => r.Id)
            .ToListAsync();

        if (allRegions.Count == 0) return;

        var existingUserRegions = await _context.Set<UserRegion>()
            .Where(ur => ur.UserId == userId && !ur.IsDeleted)
            .ToListAsync();

        var hasUpdates = false;
        foreach (var existing in existingUserRegions)
        {
            if (existing.Status != UserRegionStatus.Active.ToString())
            {
                existing.Status = UserRegionStatus.Active.ToString();
                existing.UpdatedAt = DateTime.UtcNow;
                existing.UpdatedBy = "System";
                hasUpdates = true;
            }
        }

        var existingUserRegionIds = existingUserRegions.Select(ur => ur.RegionId).ToHashSet();
        var missingRegionIds = allRegions.Except(existingUserRegionIds).ToList();
        if (missingRegionIds.Count > 0)
        {
            var newUserRegions = missingRegionIds.Select(regionId => new UserRegion
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                RegionId = regionId,
                Status = UserRegionStatus.Active.ToString(),
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "System",
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = "System",
                IsDeleted = false
            }).ToList();

            await _context.Set<UserRegion>().AddRangeAsync(newUserRegions);
            hasUpdates = true;
            _logger.LogInformation("Assigned {Count} active regions to superadmin user.", newUserRegions.Count);
        }

        if (hasUpdates)
        {
            await _context.SaveChangesAsync();
        }
    }
}