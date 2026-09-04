using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Infrastructure.Persistence;
using NTC.QoS.Infrastructure.Seed.Interfaces;

namespace NTC.QoS.Infrastructure.Seed.Services;

public class UserSeeder(
    ApplicationDbContext context,
    IConfigurationValidator configurationValidator,
    IUserCreator userCreator,
    IUserUpdater userUpdater,
    ILogger<UserSeeder> logger) : IUserSeeder
{
    // Fixed UUID for consistent default admin user seeding
    public static readonly Guid DefaultAdminUserId = Guid.NewGuid();


    public async Task SeedAsync()
    {
        logger.LogInformation("Starting User seeding process...");

        var config = await configurationValidator.ValidateAsync();
        logger.LogInformation("Clerk configuration validated successfully.");

        var existingUser = await GetExistingUserAsync(config.Email);

        if (existingUser != null)
        {
            logger.LogInformation("Default admin user already exists in database with email: {Email}", existingUser.Email);

            var clerkUsers = await config.ClerkUserService.GetUsersByEmailAsync(config.Email);
            if (clerkUsers.Count > 0)
            {
                var actualClerkUserId = clerkUsers[0].Id;
                if (existingUser.AuthUserId != actualClerkUserId)
                {
                    logger.LogInformation("Existing user AuthUserId '{CurrentAuthUserId}' does not match Clerk ID '{ActualClerkUserId}'. Updating...",
                        existingUser.AuthUserId, actualClerkUserId);
                    await userUpdater.UpdateExistingUserAsync(existingUser, config, config.Password);
                }
                else
                {
                    logger.LogInformation("Default admin user is properly configured with Clerk AuthUserId: {AuthUserId}", existingUser.AuthUserId);
                }
            }
            else
            {
                logger.LogInformation("Default admin user exists in DB but not in Clerk. Creating in Clerk and updating AuthUserId...");
                await userUpdater.UpdateExistingUserAsync(existingUser, config, config.Password);
            }
        }
        else
        {
            await userCreator.CreateNewUserAsync(config, config.Password);
        }

        logger.LogInformation("User seeding process completed successfully.");
    }

    private async Task<User?> GetExistingUserAsync(string email)
    {
        var normalizedEmail = email.ToLower();
        return await context.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail || u.Role.Key == "super_admin");
    }
}