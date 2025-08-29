using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NTC.QoS.Domain.Authorization;
using NTC.QoS.Infrastructure.Persistence;

namespace NTC.QoS.Infrastructure.Seed;

/// <summary>
/// Seeds authorization data (permissions, roles, and role permissions)
/// </summary>
public static class AuthorizationSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context, ILogger logger)
    {
        try
        {
            logger.LogInformation("Starting authorization data seeding...");

            await SeedPermissionsAsync(context, logger);
            await SeedRolesAsync(context, logger);
            await SeedRolePermissionsAsync(context, logger);

            logger.LogInformation("Authorization data seeding completed successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while seeding authorization data");
            throw;
        }
    }

    private static async Task SeedPermissionsAsync(ApplicationDbContext context, ILogger logger)
    {
        var permissions = new[]
        {
            // File permissions
            new { Key = Permissions.ViewFiles, Name = "View Files", Description = "Can view file listings" },
            new { Key = Permissions.UploadFiles, Name = "Upload Files", Description = "Can upload files" },
            new { Key = Permissions.DeleteFiles, Name = "Delete Files", Description = "Can delete files" },
            new { Key = Permissions.ProcessFiles, Name = "Process Files", Description = "Can process uploaded files" },

            // User permissions
            new { Key = Permissions.ViewUsers, Name = "View Users", Description = "Can view user listings" },
            new { Key = Permissions.CreateUsers, Name = "Create Users", Description = "Can create new users" },
            new { Key = Permissions.UpdateUsers, Name = "Update Users", Description = "Can update user information" },
            new { Key = Permissions.DeleteUsers, Name = "Delete Users", Description = "Can delete users" },

            // Role permissions
            new { Key = Permissions.ViewRoles, Name = "View Roles", Description = "Can view role listings" },
            new { Key = Permissions.CreateRoles, Name = "Create Roles", Description = "Can create new roles" },
            new { Key = Permissions.UpdateRoles, Name = "Update Roles", Description = "Can update role information" },
            new { Key = Permissions.DeleteRoles, Name = "Delete Roles", Description = "Can delete roles" },

            // Permission permissions
            new { Key = Permissions.ViewPermissions, Name = "View Permissions", Description = "Can view permission listings" },
            new { Key = Permissions.CreatePermissions, Name = "Create Permissions", Description = "Can create new permissions" },
            new { Key = Permissions.UpdatePermissions, Name = "Update Permissions", Description = "Can update permission information" },
            new { Key = Permissions.DeletePermissions, Name = "Delete Permissions", Description = "Can delete permissions" },

            // Organization permissions
            new { Key = Permissions.ViewOrganizations, Name = "View Organizations", Description = "Can view organization listings" },
            new { Key = Permissions.CreateOrganizations, Name = "Create Organizations", Description = "Can create new organizations" },
            new { Key = Permissions.UpdateOrganizations, Name = "Update Organizations", Description = "Can update organization information" },
            new { Key = Permissions.DeleteOrganizations, Name = "Delete Organizations", Description = "Can delete organizations" },

            // Cell permissions
            new { Key = Permissions.ViewCells, Name = "View Cells", Description = "Can view cell tower listings" },
            new { Key = Permissions.CreateCells, Name = "Create Cells", Description = "Can create new cell towers" },
            new { Key = Permissions.UpdateCells, Name = "Update Cells", Description = "Can update cell tower information" },
            new { Key = Permissions.DeleteCells, Name = "Delete Cells", Description = "Can delete cell towers" },

            // Network permissions
            new { Key = Permissions.ViewNetworks, Name = "View Networks", Description = "Can view network listings" },
            new { Key = Permissions.CreateNetworks, Name = "Create Networks", Description = "Can create new networks" },
            new { Key = Permissions.UpdateNetworks, Name = "Update Networks", Description = "Can update network information" },
            new { Key = Permissions.DeleteNetworks, Name = "Delete Networks", Description = "Can delete networks" },

            // Provider permissions
            new { Key = Permissions.ViewProviders, Name = "View Providers", Description = "Can view service provider listings" },
            new { Key = Permissions.CreateProviders, Name = "Create Providers", Description = "Can create new service providers" },
            new { Key = Permissions.UpdateProviders, Name = "Update Providers", Description = "Can update service provider information" },
            new { Key = Permissions.DeleteProviders, Name = "Delete Providers", Description = "Can delete service providers" },

            // Report permissions
            new { Key = Permissions.ViewReports, Name = "View Reports", Description = "Can view reports" },
            new { Key = Permissions.CreateReports, Name = "Create Reports", Description = "Can create reports" },
            new { Key = Permissions.ExportReports, Name = "Export Reports", Description = "Can export reports" },

            // System permissions
            new { Key = Permissions.SystemAdministration, Name = "System Administration", Description = "Full system administration access" },
            new { Key = Permissions.ViewLogs, Name = "View Logs", Description = "Can view system logs" },
            new { Key = Permissions.ManageSettings, Name = "Manage Settings", Description = "Can manage system settings" }
        };

        foreach (var permission in permissions)
        {
            var exists = await context.Set<Domain.Entities.Permission>()
                .AnyAsync(p => p.Key == permission.Key);

            if (!exists)
            {
                context.Set<Domain.Entities.Permission>().Add(new Domain.Entities.Permission
                {
                    Key = permission.Key,
                    Name = permission.Name,
                    Description = permission.Description,
                    CreatedBy = "System",
                    UpdatedBy = "System"
                });

                logger.LogInformation("Added permission: {PermissionKey} - {PermissionName}", permission.Key, permission.Name);
            }
        }

        await context.SaveChangesAsync();
        logger.LogInformation("Permissions seeding completed.");
    }

    private static async Task SeedRolesAsync(ApplicationDbContext context, ILogger logger)
    {
        var roles = new[]
        {
            new { Key = Roles.SuperAdmin, Name = "Super Administrator", Description = "Full system access with all permissions" },
            new { Key = Roles.Admin, Name = "Administrator", Description = "Administrative access with most permissions" },
            new { Key = Roles.Manager, Name = "Manager", Description = "Management access with elevated permissions" },
            new { Key = Roles.RegionalManager, Name = "Regional Manager", Description = "Regional management access" },
            new { Key = Roles.TechnicalStaff, Name = "Technical Staff", Description = "Technical operations access" },
            new { Key = Roles.User, Name = "User", Description = "Standard user access" },
            new { Key = Roles.Viewer, Name = "Viewer", Description = "Read-only access" }
        };

        foreach (var role in roles)
        {
            var exists = await context.Set<Domain.Entities.Role>()
                .AnyAsync(r => r.Key == role.Key);

            if (!exists)
            {
                context.Set<Domain.Entities.Role>().Add(new Domain.Entities.Role
                {
                    Key = role.Key,
                    Name = role.Name,
                    Description = role.Description,
                    CreatedBy = "System",
                    UpdatedBy = "System"
                });

                logger.LogInformation("Added role: {RoleKey} - {RoleName}", role.Key, role.Name);
            }
        }

        await context.SaveChangesAsync();
        logger.LogInformation("Roles seeding completed.");
    }

    private static async Task SeedRolePermissionsAsync(ApplicationDbContext context, ILogger logger)
    {
        // Get all roles and permissions
        var roles = await context.Set<Domain.Entities.Role>().ToListAsync();
        var permissions = await context.Set<Domain.Entities.Permission>().ToListAsync();

        // Super Admin gets all permissions
        var superAdminRole = roles.FirstOrDefault(r => r.Key == Roles.SuperAdmin);
        if (superAdminRole != null)
        {
            await AssignPermissionsToRole(context, logger, superAdminRole, permissions.Select(p => p.Key).ToArray());
        }

        // Admin gets most permissions (excluding super admin specific ones)
        var adminRole = roles.FirstOrDefault(r => r.Key == Roles.Admin);
        if (adminRole != null)
        {
            var adminPermissions = permissions.Where(p => p.Key != Permissions.SystemAdministration).Select(p => p.Key).ToArray();
            await AssignPermissionsToRole(context, logger, adminRole, adminPermissions);
        }

        // Manager gets management permissions
        var managerRole = roles.FirstOrDefault(r => r.Key == Roles.Manager);
        if (managerRole != null)
        {
            var managerPermissions = new[]
            {
                Permissions.ViewFiles, Permissions.UploadFiles,
                Permissions.ViewUsers, Permissions.CreateUsers, Permissions.UpdateUsers,
                Permissions.ViewRoles, Permissions.ViewPermissions,
                Permissions.ViewOrganizations, Permissions.UpdateOrganizations,
                Permissions.ViewCells, Permissions.CreateCells, Permissions.UpdateCells,
                Permissions.ViewNetworks, Permissions.CreateNetworks, Permissions.UpdateNetworks,
                Permissions.ViewProviders, Permissions.ViewReports, Permissions.CreateReports
            };
            await AssignPermissionsToRole(context, logger, managerRole, managerPermissions);
        }

        // Regional Manager gets regional permissions
        var regionalManagerRole = roles.FirstOrDefault(r => r.Key == Roles.RegionalManager);
        if (regionalManagerRole != null)
        {
            var regionalPermissions = new[]
            {
                Permissions.ViewFiles, Permissions.UploadFiles,
                Permissions.ViewUsers,
                Permissions.ViewCells, Permissions.CreateCells, Permissions.UpdateCells,
                Permissions.ViewNetworks, Permissions.ViewProviders,
                Permissions.ViewReports, Permissions.CreateReports
            };
            await AssignPermissionsToRole(context, logger, regionalManagerRole, regionalPermissions);
        }

        // Technical Staff gets technical permissions
        var technicalStaffRole = roles.FirstOrDefault(r => r.Key == Roles.TechnicalStaff);
        if (technicalStaffRole != null)
        {
            var technicalPermissions = new[]
            {
                Permissions.ViewFiles, Permissions.UploadFiles, Permissions.ProcessFiles,
                Permissions.ViewCells, Permissions.CreateCells, Permissions.UpdateCells,
                Permissions.ViewNetworks, Permissions.CreateNetworks, Permissions.UpdateNetworks,
                Permissions.ViewProviders, Permissions.ViewReports
            };
            await AssignPermissionsToRole(context, logger, technicalStaffRole, technicalPermissions);
        }

        // User gets basic permissions
        var userRole = roles.FirstOrDefault(r => r.Key == Roles.User);
        if (userRole != null)
        {
            var userPermissions = new[]
            {
                Permissions.ViewFiles, Permissions.UploadFiles,
                Permissions.ViewCells, Permissions.ViewNetworks, Permissions.ViewProviders,
                Permissions.ViewReports
            };
            await AssignPermissionsToRole(context, logger, userRole, userPermissions);
        }

        // Viewer gets read-only permissions
        var viewerRole = roles.FirstOrDefault(r => r.Key == Roles.Viewer);
        if (viewerRole != null)
        {
            var viewerPermissions = new[]
            {
                Permissions.ViewFiles, Permissions.ViewCells, Permissions.ViewNetworks,
                Permissions.ViewProviders, Permissions.ViewReports
            };
            await AssignPermissionsToRole(context, logger, viewerRole, viewerPermissions);
        }

        await context.SaveChangesAsync();
        logger.LogInformation("Role permissions seeding completed.");
    }

    private static async Task AssignPermissionsToRole(ApplicationDbContext context, ILogger logger, Domain.Entities.Role role, string[] permissionKeys)
    {
        foreach (var permissionKey in permissionKeys)
        {
            var permission = await context.Set<Domain.Entities.Permission>()
                .FirstOrDefaultAsync(p => p.Key == permissionKey);

            if (permission != null)
            {
                var exists = await context.Set<Domain.Aggregates.RolePermission>()
                    .AnyAsync(rp => rp.RoleId == role.Id && rp.PermissionId == permission.Id);

                if (!exists)
                {
                    context.Set<Domain.Aggregates.RolePermission>().Add(new Domain.Aggregates.RolePermission
                    {
                        RoleId = role.Id,
                        PermissionId = permission.Id,
                        CreatedBy = "System",
                        UpdatedBy = "System"
                    });

                    logger.LogDebug("Assigned permission {PermissionKey} to role {RoleKey}", permissionKey, role.Key);
                }
            }
            else
            {
                logger.LogWarning("Permission {PermissionKey} not found when assigning to role {RoleKey}", permissionKey, role.Key);
            }
        }
    }
}