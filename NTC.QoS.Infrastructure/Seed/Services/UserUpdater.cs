using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NTC.QoS.Domain.Entities;
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
        _logger.LogInformation("Updating existing admin user with Clerk integration...");

        var role = await _context.Roles
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == existingUser.RoleId);
        var organization = await _context.Organizations
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == existingUser.OrganizationId);

        string authUserId;
        var existingClerkUsers = await config.ClerkUserService.GetUsersByEmailAsync(existingUser.Email);
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
                EmailAddress = existingUser.Email,
                FirstName = "System",
                LastName = "Administrator",
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
            userToUpdate.UpdatedAt = DateTime.UtcNow;
            userToUpdate.UpdatedBy = "System";

            await _context.SaveChangesAsync();

            _logger.LogInformation("Successfully updated admin user AuthUserId from {OldAuthUserId} to {NewAuthUserId}",
                oldAuthUserId, authUserId);
        }
    }
}