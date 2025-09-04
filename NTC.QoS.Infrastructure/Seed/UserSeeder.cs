using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Infrastructure.Persistence;
using System.Security.Cryptography;

namespace NTC.QoS.Infrastructure.Seed;

public static class UserSeeder
{
    // Fixed UUID for consistent default admin user seeding
    public static readonly Guid DefaultAdminUserId = Guid.Parse("99999999-9999-9999-9999-999999999999");

    private static bool IsValidClerkSecret(string? key) => !string.IsNullOrWhiteSpace(key) && key.StartsWith("sk_");

    // Seeds a default admin user into the database and Clerk
    public static async Task SeedAsync(ApplicationDbContext context, IClerkUserService? clerkUserService, IConfiguration configuration, ILogger logger)
    {
        logger.LogInformation("Starting User seeding process...");
        await SeedDefaultAdminUserAsync(context, clerkUserService, configuration, logger);
        logger.LogInformation("User seeding process completed.");
    }

    // Seeds a default admin user from configuration
    private static async Task SeedDefaultAdminUserAsync(ApplicationDbContext context, IClerkUserService? clerkUserService, IConfiguration configuration, ILogger logger)
    {
        logger.LogInformation("Checking if default admin user needs to be seeded...");

        // Get configuration values for default admin with secure fallbacks
        var defaultAdminEmail = configuration.GetSection("DefaultAdmin:Email").Value ?? "admin@ntc.gov.ph";
        var defaultAdminFirstName = configuration.GetSection("DefaultAdmin:FirstName").Value ?? "System";
        var defaultAdminLastName = configuration.GetSection("DefaultAdmin:LastName").Value ?? "Administrator";
        
        // Priority order for password: User Secrets > Environment Variable > Default (for dev only)
        var defaultAdminPassword = configuration.GetSection("DefaultAdmin:Password").Value ?? // User Secrets
                                  Environment.GetEnvironmentVariable("DEFAULT_ADMIN_PASSWORD") ?? // Environment Variable
                                  GenerateSecureDefaultPassword(); // Generate secure password as last resort

        // Check if Clerk is configured (must be a real secret key starting with sk_)
        var clerkSecretKey = configuration.GetSection("Clerk:SecretKey").Value;
        var isClerkConfigured = IsValidClerkSecret(clerkSecretKey) && clerkUserService != null;
        if (!IsValidClerkSecret(clerkSecretKey) && !string.IsNullOrWhiteSpace(clerkSecretKey))
        {
            logger.LogWarning("Clerk secret key present but does not look valid (expected to start with 'sk_'). Skipping Clerk user creation.");
        }

        // Check if default admin user already exists in database
        var existingUser = await context.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == defaultAdminEmail || u.Id == DefaultAdminUserId);

        if (existingUser is not null)
        {
            logger.LogInformation("Default admin user already exists in database.");
            
            // If Clerk is now configured and user has placeholder AuthUserId, update it
            if (isClerkConfigured && existingUser.AuthUserId.StartsWith("clerk_placeholder_"))
            {
                await UpdateExistingUserWithClerkIntegration(context, clerkUserService!, existingUser, defaultAdminPassword, logger);
            }
            else
            {
                logger.LogInformation("Skipping user seeding - user already properly configured.");
            }
            return;
        }

        logger.LogInformation("Creating default admin user...");

        try
        {
            // Get the SuperAdmin role and National organization
            var superAdminRole = await context.Roles.AsNoTracking()
                .FirstOrDefaultAsync(r => r.Key == "super_admin");
            
            if (superAdminRole is null)
            {
                logger.LogError("SuperAdmin role not found. Cannot create default admin user.");
                throw new InvalidOperationException("SuperAdmin role not found. Ensure role seeding runs before user seeding.");
            }

            var nationalOrganization = await context.Organizations.AsNoTracking()
                .FirstOrDefaultAsync(o => o.Name == "National");
            
            if (nationalOrganization is null)
            {
                logger.LogError("National organization not found. Cannot create default admin user.");
                throw new InvalidOperationException("National organization not found. Ensure organization seeding runs before user seeding.");
            }

            // Use a deterministic placeholder based on the user's fixed ID
            string authUserId = $"clerk_placeholder_{DefaultAdminUserId:N}";

            // Create user in Clerk if configured
            if (isClerkConfigured && clerkUserService != null)
            {
                try
                {
                    logger.LogInformation("Creating user in Clerk...");
                    var clerkUserPayload = new CreateClerkUserPayload
                    {
                        EmailAddress = defaultAdminEmail,
                        FirstName = defaultAdminFirstName,
                        LastName = defaultAdminLastName,
                        Password = defaultAdminPassword,
                        PublicMetadata = new Dictionary<string, object>
                        {
                            { "role", superAdminRole.Key },
                            { "organization", nationalOrganization.Name },
                            { "isDefaultAdmin", true }
                        },
                        PrivateMetadata = new Dictionary<string, object>
                        {
                            { "created_by_seeder", true },
                            { "created_at", DateTime.UtcNow.ToString("O") }
                        }
                    };

                    var clerkUser = await clerkUserService.CreateUserAsync(clerkUserPayload);
                    authUserId = clerkUser.Id;
                    logger.LogInformation("Successfully created user in Clerk with ID: {ClerkUserId}", authUserId);
                }
                catch (Exception clerkEx)
                {
                    logger.LogError(clerkEx, "Failed to create user in Clerk (will continue with placeholder AuthUserId). Error: {ErrorMessage}", clerkEx.Message);
                }
            }
            else
            {
                logger.LogInformation("Clerk not configured or invalid secret. Creating database user with placeholder AuthUserId: {AuthUserId}", authUserId);
                logger.LogInformation("To integrate with Clerk later, configure a valid Clerk secret (starts with 'sk_') and restart the application.");
            }

            // Create user in local database
            logger.LogInformation("Creating user in local database...");
            var newUser = new User
            {
                Id = DefaultAdminUserId,
                RoleId = superAdminRole.Id,
                OrganizationId = nationalOrganization.Id,
                AuthUserId = authUserId,
                Email = defaultAdminEmail,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "System",
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = "System",
                IsDeleted = false
            };

            await context.Users.AddAsync(newUser);
            await context.SaveChangesAsync();

            logger.LogInformation("Successfully created default admin user with email: {Email} and AuthUserId: {AuthUserId}", 
                defaultAdminEmail, authUserId);

            // Log password information securely
            if (string.IsNullOrEmpty(configuration.GetSection("DefaultAdmin:Password").Value) && 
                string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DEFAULT_ADMIN_PASSWORD")))
            {
                logger.LogCritical("IMPORTANT: A password for the admin user {Email} was auto-generated and is shown below. This password is only displayed ONCE in logs. Please store it securely and change it as soon as possible. You may also set a password using the 'DefaultAdmin:Password' user secret or the 'DEFAULT_ADMIN_PASSWORD' environment variable and restart the application to ensure you can log in.", 
                    defaultAdminEmail);
                logger.LogCritical("AUTO-GENERATED PASSWORD for {Email}: {Password}", defaultAdminEmail, defaultAdminPassword);
            }

            // Log additional instructions if Clerk is not configured
            if (!isClerkConfigured)
            {
                logger.LogWarning("To enable Clerk authentication for this user, configure a valid Clerk secret and restart the application.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during default admin user seeding: {Message}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Updates an existing user with placeholder AuthUserId to use real Clerk integration
    /// </summary>
    private static async Task UpdateExistingUserWithClerkIntegration(
        ApplicationDbContext context, 
        IClerkUserService clerkUserService, 
        User existingUser, 
        string password, 
        ILogger logger)
    {
        try
        {
            logger.LogInformation("Updating existing admin user with Clerk integration...");

            var role = await context.Roles.AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == existingUser.RoleId);
            var organization = await context.Organizations.AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == existingUser.OrganizationId);

            var clerkUserPayload = new CreateClerkUserPayload
            {
                EmailAddress = existingUser.Email,
                FirstName = "System",
                LastName = "Administrator",
                Password = password,
                PublicMetadata = new Dictionary<string, object>
                {
                    { "role", role?.Key ?? "super_admin" },
                    { "organization", organization?.Name ?? "National" },
                    { "isDefaultAdmin", true }
                },
                PrivateMetadata = new Dictionary<string, object>
                {
                    { "created_by_seeder", true },
                    { "updated_from_placeholder", true },
                    { "updated_at", DateTime.UtcNow.ToString("O") }
                }
            };

            var clerkUser = await clerkUserService.CreateUserAsync(clerkUserPayload);

            var userToUpdate = await context.Users.FirstOrDefaultAsync(u => u.Id == existingUser.Id);
            if (userToUpdate != null)
            {
                var oldAuthUserId = userToUpdate.AuthUserId;
                userToUpdate.AuthUserId = clerkUser.Id;
                userToUpdate.UpdatedAt = DateTime.UtcNow;
                userToUpdate.UpdatedBy = "System";

                await context.SaveChangesAsync();

                logger.LogInformation("Successfully updated admin user AuthUserId from {OldAuthUserId} to {NewAuthUserId}", 
                    oldAuthUserId, clerkUser.Id);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update existing user with Clerk integration: {ErrorMessage}", ex.Message);
            logger.LogWarning("User remains with placeholder AuthUserId. Manual intervention may be required.");
        }
    }

    private static string GenerateSecureDefaultPassword()
    {
        // Generate a secure random password for production use
        const string uppercase = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string lowercase = "abcdefghijkmnpqrstuvwxyz";
        const string digits = "023456789";
        const string special = "!@#$%^&*";
        const string allChars = uppercase + lowercase + digits + special;

        var password = new char[16];

        // Ensure at least one of each type
        password[0] = uppercase[RandomNumberGenerator.GetInt32(uppercase.Length)];
        password[1] = lowercase[RandomNumberGenerator.GetInt32(lowercase.Length)];
        password[2] = digits[RandomNumberGenerator.GetInt32(digits.Length)];
        password[3] = special[RandomNumberGenerator.GetInt32(special.Length)];

        // Fill the rest randomly
        for (int i = 4; i < 16; i++)
        {
            password[i] = allChars[RandomNumberGenerator.GetInt32(allChars.Length)];
        }

        // Shuffle the password using Fisher-Yates shuffle
        for (int i = password.Length - 1; i > 0; i--)
        {
            int j = RandomNumberGenerator.GetInt32(i + 1);
            (password[i], password[j]) = (password[j], password[i]);
        }

        return new string(password);
    }
}