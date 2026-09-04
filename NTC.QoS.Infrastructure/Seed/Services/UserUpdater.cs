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
        }
    }
}