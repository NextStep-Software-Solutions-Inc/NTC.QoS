using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Authorization;
using NTC.QoS.Infrastructure.Persistence;

namespace NTC.QoS.Infrastructure.Seed;

/// <summary>
/// Seeds authorization data (permissions, roles, and role permissions)
/// Aligned with existing RoleSeeder for consistency and includes all controller permissions
/// </summary>
public static class AuthorizationSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context, ILogger logger)
    {
        try
        {
            logger.LogInformation("Starting authorization data seeding...");

            await SeedPermissionsAsync(context, logger);
            //await SeedAdditionalRolesAsync(context, logger);
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
            // Dashboard permissions
            new { Key = Permissions.AccessDashboard, Name = "Access Dashboard", Description = "Can access system dashboard" },

            // Analytics permissions
            new { Key = Permissions.ViewAnalytics, Name = "View Analytics", Description = "Can view analytics dashboards and data" },
            new { Key = Permissions.CreateAnalytics, Name = "Create Analytics", Description = "Can create custom analytics reports" },
            new { Key = Permissions.UpdateAnalytics, Name = "Update Analytics", Description = "Can modify analytics configurations" },
            new { Key = Permissions.DeleteAnalytics, Name = "Delete Analytics", Description = "Can delete analytics reports" },
            new { Key = Permissions.ExportAnalytics, Name = "Export Analytics", Description = "Can export analytics data and reports" },
            new { Key = Permissions.ConfigureAnalytics, Name = "Configure Analytics", Description = "Can configure analytics settings and parameters" },

            // File permissions
            new { Key = Permissions.ViewFiles, Name = "View Files", Description = "Can view file listings" },
            new { Key = Permissions.UploadFiles, Name = "Upload Files", Description = "Can upload files" },
            new { Key = Permissions.DeleteFiles, Name = "Delete Files", Description = "Can delete files" },
            new { Key = Permissions.ProcessFiles, Name = "Process Files", Description = "Can process uploaded files" },
            new { Key = Permissions.DownloadFiles, Name = "Download Files", Description = "Can download files" },
            new { Key = Permissions.UpdateFiles, Name = "Update Files", Description = "Can update file metadata" },

            // User permissions
            new { Key = Permissions.ViewUsers, Name = "View Users", Description = "Can view user listings" },
            new { Key = Permissions.CreateUsers, Name = "Create Users", Description = "Can create new users" },
            new { Key = Permissions.UpdateUsers, Name = "Update Users", Description = "Can update user information" },
            new { Key = Permissions.DeleteUsers, Name = "Delete Users", Description = "Can delete users" },
            new { Key = Permissions.InviteUsers, Name = "Invite Users", Description = "Can invite new users via email" },

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

            // Role-Permission management permissions
            new { Key = Permissions.ViewRolePermissions, Name = "View Role Permissions", Description = "Can view role permission assignments" },
            new { Key = Permissions.CreateRolePermissions, Name = "Create Role Permissions", Description = "Can assign permissions to roles" },
            new { Key = Permissions.DeleteRolePermissions, Name = "Delete Role Permissions", Description = "Can remove permissions from roles" },

            // Organization permissions
            new { Key = Permissions.ViewOrganizations, Name = "View Organizations", Description = "Can view organization listings" },
            new { Key = Permissions.CreateOrganizations, Name = "Create Organizations", Description = "Can create new organizations" },
            new { Key = Permissions.UpdateOrganizations, Name = "Update Organizations", Description = "Can update organization information" },
            new { Key = Permissions.DeleteOrganizations, Name = "Delete Organizations", Description = "Can delete organizations" },

            // Region permissions
            new { Key = Permissions.ViewRegions, Name = "View Regions", Description = "Can view region listings" },
            new { Key = Permissions.CreateRegions, Name = "Create Regions", Description = "Can create new regions" },
            new { Key = Permissions.UpdateRegions, Name = "Update Regions", Description = "Can update region information" },
            new { Key = Permissions.DeleteRegions, Name = "Delete Regions", Description = "Can delete regions" },

            // Province permissions
            new { Key = Permissions.ViewProvinces, Name = "View Provinces", Description = "Can view province listings" },
            new { Key = Permissions.CreateProvinces, Name = "Create Provinces", Description = "Can create new provinces" },
            new { Key = Permissions.UpdateProvinces, Name = "Update Provinces", Description = "Can update province information" },
            new { Key = Permissions.DeleteProvinces, Name = "Delete Provinces", Description = "Can delete provinces" },

            // Municipality permissions
            new { Key = Permissions.ViewMunicipalities, Name = "View Municipalities", Description = "Can view municipality listings" },
            new { Key = Permissions.CreateMunicipalities, Name = "Create Municipalities", Description = "Can create new municipalities" },
            new { Key = Permissions.UpdateMunicipalities, Name = "Update Municipalities", Description = "Can update municipality information" },
            new { Key = Permissions.DeleteMunicipalities, Name = "Delete Municipalities", Description = "Can delete municipalities" },

            // Barangay permissions
            new { Key = Permissions.ViewBarangays, Name = "View Barangays", Description = "Can view barangay listings" },
            new { Key = Permissions.CreateBarangays, Name = "Create Barangays", Description = "Can create new barangays" },
            new { Key = Permissions.UpdateBarangays, Name = "Update Barangays", Description = "Can update barangay information" },
            new { Key = Permissions.DeleteBarangays, Name = "Delete Barangays", Description = "Can delete barangays" },

            // Zone permissions
            new { Key = Permissions.ViewZones, Name = "View Zones", Description = "Can view zone listings" },
            new { Key = Permissions.CreateZones, Name = "Create Zones", Description = "Can create new zones" },
            new { Key = Permissions.UpdateZones, Name = "Update Zones", Description = "Can update zone information" },
            new { Key = Permissions.DeleteZones, Name = "Delete Zones", Description = "Can delete zones" },

            // User-Region permissions
            new { Key = Permissions.ViewUserRegions, Name = "View User Regions", Description = "Can view user region assignments" },
            new { Key = Permissions.CreateUserRegions, Name = "Create User Regions", Description = "Can assign users to regions" },
            new { Key = Permissions.UpdateUserRegions, Name = "Update User Regions", Description = "Can update user region assignments" },
            new { Key = Permissions.DeleteUserRegions, Name = "Delete User Regions", Description = "Can remove user region assignments" },

            // Provider permissions
            new { Key = Permissions.ViewProviders, Name = "View Providers", Description = "Can view service provider listings" },
            new { Key = Permissions.CreateProviders, Name = "Create Providers", Description = "Can create new service providers" },
            new { Key = Permissions.UpdateProviders, Name = "Update Providers", Description = "Can update service provider information" },
            new { Key = Permissions.DeleteProviders, Name = "Delete Providers", Description = "Can delete service providers" },

            // Service permissions
            new { Key = Permissions.ViewServices, Name = "View Services", Description = "Can view service listings" },
            new { Key = Permissions.CreateServices, Name = "Create Services", Description = "Can create new services" },
            new { Key = Permissions.UpdateServices, Name = "Update Services", Description = "Can update service information" },
            new { Key = Permissions.DeleteServices, Name = "Delete Services", Description = "Can delete services" },

            // Service Provider permissions
            new { Key = Permissions.ViewServiceProviders, Name = "View Service Providers", Description = "Can view service provider mappings" },
            new { Key = Permissions.CreateServiceProviders, Name = "Create Service Providers", Description = "Can create service provider mappings" },
            new { Key = Permissions.UpdateServiceProviders, Name = "Update Service Providers", Description = "Can update service provider mappings" },
            new { Key = Permissions.DeleteServiceProviders, Name = "Delete Service Providers", Description = "Can delete service provider mappings" },

            // Generation permissions
            new { Key = Permissions.ViewGenerations, Name = "View Generations", Description = "Can view technology generation listings" },
            new { Key = Permissions.CreateGenerations, Name = "Create Generations", Description = "Can create new technology generations" },
            new { Key = Permissions.UpdateGenerations, Name = "Update Generations", Description = "Can update technology generation information" },
            new { Key = Permissions.DeleteGenerations, Name = "Delete Generations", Description = "Can delete technology generations" },

            // Technology permissions
            new { Key = Permissions.ViewTechnologies, Name = "View Technologies", Description = "Can view technology listings" },
            new { Key = Permissions.CreateTechnologies, Name = "Create Technologies", Description = "Can create new technologies" },
            new { Key = Permissions.UpdateTechnologies, Name = "Update Technologies", Description = "Can update technology information" },
            new { Key = Permissions.DeleteTechnologies, Name = "Delete Technologies", Description = "Can delete technologies" },

            // Band permissions
            new { Key = Permissions.ViewBands, Name = "View Bands", Description = "Can view frequency band listings" },
            new { Key = Permissions.CreateBands, Name = "Create Bands", Description = "Can create new frequency bands" },
            new { Key = Permissions.UpdateBands, Name = "Update Bands", Description = "Can update frequency band information" },
            new { Key = Permissions.DeleteBands, Name = "Delete Bands", Description = "Can delete frequency bands" },

            // Link/Frequency Allocation permissions
            new { Key = Permissions.ViewFrequencyAllocations, Name = "View Frequency Allocations", Description = "Can view frequency allocation listings" },
            new { Key = Permissions.CreateFrequencyAllocations, Name = "Create Frequency Allocations", Description = "Can create new frequency allocations" },
            new { Key = Permissions.UpdateFrequencyAllocations, Name = "Update Frequency Allocations", Description = "Can update frequency allocation information" },
            new { Key = Permissions.DeleteFrequencyAllocations, Name = "Delete Frequency Allocations", Description = "Can delete frequency allocations" },

            // Network permissions
            new { Key = Permissions.ViewNetworks, Name = "View Networks", Description = "Can view network listings" },
            new { Key = Permissions.CreateNetworks, Name = "Create Networks", Description = "Can create new networks" },
            new { Key = Permissions.UpdateNetworks, Name = "Update Networks", Description = "Can update network information" },
            new { Key = Permissions.DeleteNetworks, Name = "Delete Networks", Description = "Can delete networks" },

            // Sector permissions
            new { Key = Permissions.ViewSectors, Name = "View Sectors", Description = "Can view sector listings" },
            new { Key = Permissions.CreateSectors, Name = "Create Sectors", Description = "Can create new sectors" },
            new { Key = Permissions.UpdateSectors, Name = "Update Sectors", Description = "Can update sector information" },
            new { Key = Permissions.DeleteSectors, Name = "Delete Sectors", Description = "Can delete sectors" },

            // System Type permissions
            new { Key = Permissions.ViewSystemTypes, Name = "View System Types", Description = "Can view system type listings" },
            new { Key = Permissions.CreateSystemTypes, Name = "Create System Types", Description = "Can create new system types" },
            new { Key = Permissions.UpdateSystemTypes, Name = "Update System Types", Description = "Can update system type information" },
            new { Key = Permissions.DeleteSystemTypes, Name = "Delete System Types", Description = "Can delete system types" },

            // Area Study Type permissions
            new { Key = Permissions.ViewAreaStudyTypes, Name = "View Area Study Types", Description = "Can view area study type listings" },
            new { Key = Permissions.CreateAreaStudyTypes, Name = "Create Area Study Types", Description = "Can create new area study types" },
            new { Key = Permissions.UpdateAreaStudyTypes, Name = "Update Area Study Types", Description = "Can update area study type information" },
            new { Key = Permissions.DeleteAreaStudyTypes, Name = "Delete Area Study Types", Description = "Can delete area study types" },

            // Cell permissions
            new { Key = Permissions.ViewCells, Name = "View Cells", Description = "Can view cell tower listings" },
            new { Key = Permissions.CreateCells, Name = "Create Cells", Description = "Can create new cell towers" },
            new { Key = Permissions.UpdateCells, Name = "Update Cells", Description = "Can update cell tower information" },
            new { Key = Permissions.DeleteCells, Name = "Delete Cells", Description = "Can delete cell towers" },

            // Cell Sector permissions
            new { Key = Permissions.ViewCellSectors, Name = "View Cell Sectors", Description = "Can view cell sector listings" },
            new { Key = Permissions.CreateCellSectors, Name = "Create Cell Sectors", Description = "Can create new cell sectors" },
            new { Key = Permissions.UpdateCellSectors, Name = "Update Cell Sectors", Description = "Can update cell sector information" },
            new { Key = Permissions.DeleteCellSectors, Name = "Delete Cell Sectors", Description = "Can delete cell sectors" },

            // NAP permissions
            new { Key = Permissions.ViewNAPs, Name = "View NAPs", Description = "Can view NAP listings" },
            new { Key = Permissions.CreateNAPs, Name = "Create NAPs", Description = "Can create new NAPs" },
            new { Key = Permissions.UpdateNAPs, Name = "Update NAPs", Description = "Can update NAP information" },
            new { Key = Permissions.DeleteNAPs, Name = "Delete NAPs", Description = "Can delete NAPs" },

            // Speedtest permissions
            new { Key = Permissions.ViewSpeedtests, Name = "View Speedtests", Description = "Can view speedtest listings" },
            new { Key = Permissions.CreateSpeedtests, Name = "Create Speedtests", Description = "Can create new speedtests" },
            new { Key = Permissions.UpdateSpeedtests, Name = "Update Speedtests", Description = "Can update speedtest information" },
            new { Key = Permissions.DeleteSpeedtests, Name = "Delete Speedtests", Description = "Can delete speedtests" },

            // Cellular permissions
            new { Key = Permissions.ViewCellulars, Name = "View Cellulars", Description = "Can view cellular listings" },
            new { Key = Permissions.CreateCellulars, Name = "Create Cellulars", Description = "Can create new cellular records" },
            new { Key = Permissions.DeleteCellulars, Name = "Delete Cellulars", Description = "Can delete cellular records" },

            // Report permissions
            new { Key = Permissions.ViewReports, Name = "View Reports", Description = "Can view reports" },
            new { Key = Permissions.CreateReports, Name = "Create Reports", Description = "Can create reports" },
            new { Key = Permissions.ExportReports, Name = "Export Reports", Description = "Can export reports" },

            // System permissions
            new { Key = Permissions.SystemAdministration, Name = "System Administration", Description = "Full system administration access" },
            new { Key = Permissions.ViewLogs, Name = "View Logs", Description = "Can view system logs" },
            new { Key = Permissions.ManageSettings, Name = "Manage Settings", Description = "Can manage system settings" }
        };


        var existingPermissions = await context.Permissions.AsNoTracking().Where(p => permissions.Select(x => x.Key).Contains(p.Key)).ToListAsync();
        var newPermissions = permissions.Where(p => !existingPermissions.Any(ep => ep.Key == p.Key))
            .Select(p => new Domain.Entities.Permission
            {
                Key = p.Key,
                Name = p.Name,
                Description = p.Description,
                CreatedBy = "System",
                UpdatedBy = "System"
            }).ToList();
        if (newPermissions.Count == 0)
        {
            logger.LogInformation("No new permissions to seed. All base permissions already exist.");
            return;
        }

        await context.BulkInsertAsync(newPermissions);
        await context.SaveChangesAsync();
        logger.LogInformation("Permissions seeding completed.");
    }

    /// <summary>
    /// Seeds additional roles that complement the existing RoleSeeder roles
    /// Does not conflict with existing "admin", "super_admin", "field_officer", "user" roles
    /// </summary>
    private static async Task SeedAdditionalRolesAsync(ApplicationDbContext context, ILogger logger)
    {
        // Additional roles that complement existing ones
        var additionalRoles = new[]
        {
            new { Key = "manager", Name = "Manager", Description = "Management access with elevated permissions" },
            new { Key = "regional_manager", Name = "Regional Manager", Description = "Regional management access" },
            new { Key = "viewer", Name = "Viewer", Description = "Read-only access" }
        };

        foreach (var role in additionalRoles)
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

                logger.LogInformation("Added additional role: {RoleKey} - {RoleName}", role.Key, role.Name);
            }
        }

        await context.SaveChangesAsync();
        logger.LogInformation("Additional roles seeding completed.");
    }

    /// <summary>
    /// Seeds role permissions for both existing and additional roles
    /// Uses the actual database role keys (snake_case from RoleSeeder)
    /// </summary>
    private static async Task SeedRolePermissionsAsync(ApplicationDbContext context, ILogger logger)
    {
        // Get all roles and permissions
        var roles = await context.Set<Domain.Entities.Role>().ToListAsync();
        var permissions = await context.Set<Domain.Entities.Permission>().ToListAsync();

        // Map existing roles to authorization roles
        await AssignPermissionsForExistingRoles(context, logger, roles, permissions);
        //await AssignPermissionsForAdditionalRoles(context, logger, roles, permissions);

        await context.SaveChangesAsync();
        logger.LogInformation("Role permissions seeding completed.");
    }

    private static async Task AssignPermissionsForExistingRoles(ApplicationDbContext context, ILogger logger, List<Domain.Entities.Role> roles, List<Domain.Entities.Permission> permissions)
    {
        // Super Admin (super_admin) gets all permissions
        var superAdminRole = roles.FirstOrDefault(r => r.Key == "super_admin");
        if (superAdminRole != null)
        {
            await AssignPermissionsToRole(context, logger, superAdminRole, permissions.Select(p => p.Key).ToArray());
        }

        // Admin gets most permissions (excluding super admin specific ones)
        var adminRole = roles.FirstOrDefault(r => r.Key == "admin");
        if (adminRole != null)
        {
            var adminPermissions = permissions.Where(p => p.Key != Permissions.SystemAdministration).Select(p => p.Key).ToArray();
            await AssignPermissionsToRole(context, logger, adminRole, adminPermissions);
        }

        // Field Officer (existing role) gets technical/operational permissions
        var fieldOfficerRole = roles.FirstOrDefault(r => r.Key == "field_officer");
        if (fieldOfficerRole != null)
        {
            var fieldOfficerPermissions = new[]
            {
                Permissions.ViewFiles, Permissions.UploadFiles, Permissions.ProcessFiles, Permissions.DownloadFiles, Permissions.UpdateFiles,
                Permissions.ViewCells, Permissions.CreateCells, Permissions.UpdateCells,
                Permissions.ViewNetworks, Permissions.CreateNetworks, Permissions.UpdateNetworks,
                Permissions.ViewSectors, Permissions.CreateSectors, Permissions.UpdateSectors, 
                Permissions.ViewCellSectors, Permissions.CreateCellSectors, Permissions.UpdateCellSectors,
                Permissions.ViewProviders, Permissions.ViewTechnologies, Permissions.ViewBands,
                Permissions.ViewZones, Permissions.ViewCellulars, Permissions.CreateCellulars,
                Permissions.ViewSpeedtests, Permissions.CreateSpeedtests, Permissions.UpdateSpeedtests,
                Permissions.ViewNAPs, Permissions.CreateNAPs, Permissions.UpdateNAPs,
                Permissions.ViewAnalytics
            };
            await AssignPermissionsToRole(context, logger, fieldOfficerRole, fieldOfficerPermissions);
        }

        // User gets basic permissions
        var userRole = roles.FirstOrDefault(r => r.Key == "user");
        if (userRole != null)
        {
            var userPermissions = new[]
            {
                Permissions.ViewCells, Permissions.ViewCellulars, Permissions.ViewSpeedtests, Permissions.ViewNAPs
            };
            await AssignPermissionsToRole(context, logger, userRole, userPermissions);
        }
    }

    private static async Task AssignPermissionsForAdditionalRoles(ApplicationDbContext context, ILogger logger, List<Domain.Entities.Role> roles, List<Domain.Entities.Permission> permissions)
    {
        // Manager gets management permissions
        var managerRole = roles.FirstOrDefault(r => r.Key == "manager");
        if (managerRole != null)
        {
            var managerPermissions = new[]
            {
                Permissions.AccessDashboard,
                Permissions.ViewAnalytics, Permissions.CreateAnalytics, Permissions.UpdateAnalytics, Permissions.ExportAnalytics,
                Permissions.ViewFiles, Permissions.UploadFiles,
                Permissions.ViewUsers, Permissions.CreateUsers, Permissions.UpdateUsers,
                Permissions.ViewRoles, Permissions.ViewPermissions, Permissions.ViewRolePermissions,
                Permissions.ViewOrganizations, Permissions.UpdateOrganizations,
                Permissions.ViewRegions, Permissions.CreateRegions, Permissions.UpdateRegions,
                Permissions.ViewZones, Permissions.CreateZones, Permissions.UpdateZones,
                Permissions.ViewUserRegions, Permissions.CreateUserRegions, Permissions.UpdateUserRegions,
                Permissions.ViewCells, Permissions.CreateCells, Permissions.UpdateCells,
                Permissions.ViewNetworks, Permissions.CreateNetworks, Permissions.UpdateNetworks,
                Permissions.ViewSectors, Permissions.CreateSectors, Permissions.UpdateSectors,
                Permissions.ViewProviders, Permissions.CreateProviders, Permissions.UpdateProviders,
                Permissions.ViewTechnologies, Permissions.ViewBands, Permissions.ViewGenerations,
                Permissions.ViewReports, Permissions.CreateReports, Permissions.ExportReports
            };
            await AssignPermissionsToRole(context, logger, managerRole, managerPermissions);
        }

        // Regional Manager gets regional permissions
        var regionalManagerRole = roles.FirstOrDefault(r => r.Key == "regional_manager");
        if (regionalManagerRole != null)
        {
            var regionalPermissions = new[]
            {
                Permissions.AccessDashboard,
                Permissions.ViewAnalytics,
                Permissions.ViewFiles, Permissions.UploadFiles,
                Permissions.ViewUsers,
                Permissions.ViewRegions, Permissions.UpdateRegions,
                Permissions.ViewZones, Permissions.CreateZones, Permissions.UpdateZones,
                Permissions.ViewUserRegions, Permissions.CreateUserRegions, Permissions.UpdateUserRegions,
                Permissions.ViewCells, Permissions.CreateCells, Permissions.UpdateCells,
                Permissions.ViewNetworks, Permissions.ViewProviders,
                Permissions.ViewReports, Permissions.CreateReports
            };
            await AssignPermissionsToRole(context, logger, regionalManagerRole, regionalPermissions);
        }

        // Viewer gets read-only permissions
        var viewerRole = roles.FirstOrDefault(r => r.Key == "viewer");
        if (viewerRole != null)
        {
            var viewerPermissions = new[]
            {
                Permissions.AccessDashboard,
                Permissions.ViewAnalytics,
                Permissions.ViewFiles,
                Permissions.ViewUsers, Permissions.ViewRoles, Permissions.ViewPermissions,
                Permissions.ViewOrganizations, Permissions.ViewRegions, Permissions.ViewZones,
                Permissions.ViewCells, Permissions.ViewNetworks, Permissions.ViewSectors,
                Permissions.ViewProviders, Permissions.ViewTechnologies, Permissions.ViewBands,
                Permissions.ViewReports
            };
            await AssignPermissionsToRole(context, logger, viewerRole, viewerPermissions);
        }
    }

    private static async Task AssignPermissionsToRole(ApplicationDbContext context, ILogger logger, Domain.Entities.Role role, string[] permissionKeys)
    {
        // Get existing role-permission assignments for this role
        var existingAssignments = await context.RolePermissions
            .Where(rp => rp.RoleId == role.Id)
            .Select(rp => rp.PermissionId)
            .ToHashSetAsync();

        // Get permissions that are not yet assigned
        var permissionsToAssign = await context.Permissions
            .Where(p => permissionKeys.Contains(p.Key) && !existingAssignments.Contains(p.Id))
            .ToListAsync();

        if (permissionsToAssign.Count == 0)
        {
            logger.LogDebug("No new permissions to assign to role {RoleKey}", role.Key);
            return;
        }

        // Create new role-permission assignments
        var newAssignments = permissionsToAssign.Select(p => new RolePermission
        {
            RoleId = role.Id,
            PermissionId = p.Id,
            CreatedBy = "System",
            UpdatedBy = "System"
        }).ToList();

        // Bulk insert new assignments
        await context.BulkInsertAsync(newAssignments);
        await context.SaveChangesAsync();

        logger.LogDebug("Assigned {Count} new permissions to role {RoleKey}", newAssignments.Count, role.Key);
    }
}