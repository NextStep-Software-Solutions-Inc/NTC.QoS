using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Infrastructure.Persistence;

namespace NTC.QoS.Infrastructure.Seed;

public static class UserSeeder
{
    // Fixed UUID for consistent default admin user seeding
    public static readonly Guid DefaultAdminUserId = Guid.Parse("99999999-9999-9999-9999-999999999999");

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

        // Check if Clerk is configured
        var clerkSecretKey = configuration.GetSection("Clerk:SecretKey").Value;
        var isClerkConfigured = !string.IsNullOrEmpty(clerkSecretKey) && clerkUserService != null;

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

            string authUserId = "clerk_placeholder_" + Guid.NewGuid().ToString("N")[..8]; // Default placeholder

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
                        EmailVerified = true,
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
                    logger.LogError(clerkEx, "Failed to create user in Clerk. Proceeding with database-only user creation. Error: {ErrorMessage}", clerkEx.Message);
                    // Continue with database creation even if Clerk fails
                }
            }
            else
            {
                logger.LogInformation("Clerk not configured. Creating database user with placeholder AuthUserId: {AuthUserId}", authUserId);
                logger.LogInformation("To integrate with Clerk later, configure Clerk credentials and restart the application.");
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
                logger.LogCritical("IMPORTANT: Generated password for admin user {Email}: {Password}. Save this password securely and change it immediately after first login!", 
                    defaultAdminEmail, defaultAdminPassword);
            }

            // Log additional instructions if Clerk is not configured
            if (!isClerkConfigured)
            {
                logger.LogWarning("To enable Clerk authentication for this user, configure Clerk credentials and restart the application.");
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

            // Get role and organization for metadata
            var role = await context.Roles.AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == existingUser.RoleId);
            var organization = await context.Organizations.AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == existingUser.OrganizationId);

            // Create user in Clerk
            var clerkUserPayload = new CreateClerkUserPayload
            {
                EmailAddress = existingUser.Email,
                FirstName = "System",
                LastName = "Administrator",
                Password = password,
                EmailVerified = true,
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

            // Update the user's AuthUserId in the database
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
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz023456789!@#$%^&*";
        var random = new Random();
        var password = new char[16];
        
        // Ensure at least one of each type
        password[0] = "ABCDEFGHJKLMNPQRSTUVWXYZ"[random.Next(23)]; // Uppercase
        password[1] = "abcdefghijkmnpqrstuvwxyz"[random.Next(23)]; // Lowercase  
        password[2] = "023456789"[random.Next(9)]; // Number
        password[3] = "!@#$%^&*"[random.Next(8)]; // Special
        
        // Fill the rest randomly
        for (int i = 4; i < 16; i++)
        {
            password[i] = chars[random.Next(chars.Length)];
        }
        
        // Shuffle the password
        for (int i = password.Length - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (password[i], password[j]) = (password[j], password[i]);
        }
        
        return new string(password);
    }
}