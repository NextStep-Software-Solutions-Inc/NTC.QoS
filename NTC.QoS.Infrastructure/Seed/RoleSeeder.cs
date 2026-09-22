using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NTC.QoS.Domain.Authorization;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Infrastructure.Persistence;

namespace NTC.QoS.Infrastructure.Seed;

public static class RoleSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context, ILogger logger)
    {
        logger.LogInformation("Starting Role seeding process...");
        await SeedRolesAsync(context, logger);
        logger.LogInformation("Role seeding process completed.");
    }

    private static async Task SeedRolesAsync(ApplicationDbContext context, ILogger logger)
    {
        var allPermissions = PermissionsRegistry.All.Select(p => p.Key).ToList();

        // National permissions (oversight, all analytics, AI reports, reading all assets, minus direct low-level system admin)
        var nationalPermissions = allPermissions.Where(p =>
            p != Permissions.SystemAdministration &&
            p != Permissions.ManageSettings
        ).ToList();

        // Regional Admin permissions (full regional administrative and asset management)
        var regionalAdminPermissions = new List<string>
        {
            Permissions.AccessDashboard,
            Permissions.ViewAnalytics, Permissions.CreateAnalytics, Permissions.UpdateAnalytics, Permissions.ExportAnalytics,
            Permissions.ViewFiles, Permissions.UploadFiles, Permissions.DeleteFiles, Permissions.ProcessFiles, Permissions.DownloadFiles, Permissions.UpdateFiles,
            Permissions.ViewUsers, Permissions.CreateUsers, Permissions.UpdateUsers, Permissions.InviteUsers,
            Permissions.ViewRoles, Permissions.CreateRoles, Permissions.UpdateRoles,
            Permissions.ViewUserRegions, Permissions.CreateUserRegions, Permissions.UpdateUserRegions, Permissions.DeleteUserRegions,
            Permissions.ViewCells, Permissions.CreateCells, Permissions.UpdateCells, Permissions.DeleteCells,
            Permissions.ViewNetworks, Permissions.CreateNetworks, Permissions.UpdateNetworks, Permissions.DeleteNetworks,
            Permissions.ViewSectors, Permissions.CreateSectors, Permissions.UpdateSectors, Permissions.DeleteSectors,
            Permissions.ViewCellSectors, Permissions.CreateCellSectors, Permissions.UpdateCellSectors, Permissions.DeleteCellSectors,
            Permissions.ViewProviders, Permissions.CreateProviders, Permissions.UpdateProviders,
            Permissions.ViewServices, Permissions.CreateServices, Permissions.UpdateServices,
            Permissions.ViewServiceProviders, Permissions.CreateServiceProviders, Permissions.UpdateServiceProviders,
            Permissions.ViewTechnologies, Permissions.CreateTechnologies, Permissions.UpdateTechnologies,
            Permissions.ViewBands, Permissions.CreateBands, Permissions.UpdateBands,
            Permissions.ViewGenerations, Permissions.CreateGenerations, Permissions.UpdateGenerations,
            Permissions.ViewSystemTypes, Permissions.CreateSystemTypes, Permissions.UpdateSystemTypes,
            Permissions.ViewAreaStudyTypes, Permissions.CreateAreaStudyTypes, Permissions.UpdateAreaStudyTypes,
            Permissions.ViewSignalLevels, Permissions.CreateSignalLevels, Permissions.UpdateSignalLevels,
            Permissions.ViewSignalLevelEntries, Permissions.CreateSignalLevelEntries, Permissions.UpdateSignalLevelEntries,
            Permissions.ViewFrequencyAllocations, Permissions.CreateFrequencyAllocations, Permissions.UpdateFrequencyAllocations,
            Permissions.ViewRegulatoryAllocations, Permissions.CreateRegulatoryAllocations, Permissions.UpdateRegulatoryAllocations,
            Permissions.ViewZones, Permissions.CreateZones, Permissions.UpdateZones,
            Permissions.ViewBarangays, Permissions.CreateBarangays, Permissions.UpdateBarangays,
            Permissions.ViewMunicipalities, Permissions.CreateMunicipalities, Permissions.UpdateMunicipalities,
            Permissions.ViewProvinces, Permissions.CreateProvinces, Permissions.UpdateProvinces,
            Permissions.ViewRegions,
            Permissions.ViewBarangayCoverage, Permissions.CreateBarangayCoverage, Permissions.UpdateBarangayCoverage,
            Permissions.ViewNAPs, Permissions.CreateNAPs, Permissions.UpdateNAPs, Permissions.DeleteNAPs,
            Permissions.ViewSpeedtests, Permissions.CreateSpeedtests, Permissions.UpdateSpeedtests, Permissions.DeleteSpeedtests,
            Permissions.ViewCellulars, Permissions.CreateCellulars, Permissions.DeleteCellulars,
            Permissions.ViewReports, Permissions.CreateReports, Permissions.ExportReports,
            Permissions.ChatWithAI, Permissions.GenerateReports
        };

        // Regional Staff permissions (operational upload, validation, view reports)
        var regionalStaffPermissions = new List<string>
        {
            Permissions.AccessDashboard,
            Permissions.ViewAnalytics, Permissions.ExportAnalytics,
            Permissions.ViewFiles, Permissions.UploadFiles, Permissions.ProcessFiles, Permissions.DownloadFiles, Permissions.UpdateFiles,
            Permissions.ViewCells, Permissions.CreateCells, Permissions.UpdateCells,
            Permissions.ViewNetworks, Permissions.ViewSectors, Permissions.ViewCellSectors,
            Permissions.ViewProviders, Permissions.ViewServices, Permissions.ViewTechnologies, Permissions.ViewBands, Permissions.ViewGenerations,
            Permissions.ViewSystemTypes, Permissions.ViewAreaStudyTypes, Permissions.ViewSignalLevels, Permissions.ViewSignalLevelEntries,
            Permissions.ViewFrequencyAllocations, Permissions.ViewRegulatoryAllocations,
            Permissions.ViewZones, Permissions.ViewBarangays, Permissions.ViewMunicipalities, Permissions.ViewProvinces, Permissions.ViewRegions,
            Permissions.ViewBarangayCoverage, Permissions.CreateBarangayCoverage, Permissions.UpdateBarangayCoverage,
            Permissions.ViewNAPs, Permissions.CreateNAPs, Permissions.UpdateNAPs,
            Permissions.ViewSpeedtests, Permissions.CreateSpeedtests, Permissions.UpdateSpeedtests,
            Permissions.ViewCellulars, Permissions.CreateCellulars,
            Permissions.ViewReports, Permissions.CreateReports, Permissions.ExportReports,
            Permissions.ChatWithAI
        };

        // Field Engineer permissions (operational field drive tests, speedtests, cell inspection)
        var fieldEngineerPermissions = new List<string>
        {
            Permissions.AccessDashboard,
            Permissions.ViewFiles, Permissions.UploadFiles, Permissions.ProcessFiles, Permissions.DownloadFiles,
            Permissions.ViewCells, Permissions.CreateCells, Permissions.UpdateCells,
            Permissions.ViewNetworks, Permissions.ViewSectors, Permissions.ViewCellSectors,
            Permissions.ViewProviders, Permissions.ViewTechnologies, Permissions.ViewBands,
            Permissions.ViewZones, Permissions.ViewCellulars, Permissions.CreateCellulars,
            Permissions.ViewSpeedtests, Permissions.CreateSpeedtests, Permissions.UpdateSpeedtests,
            Permissions.ViewNAPs, Permissions.CreateNAPs, Permissions.UpdateNAPs,
            Permissions.ViewSignalLevels, Permissions.ViewSignalLevelEntries,
            Permissions.ViewReports, Permissions.CreateReports, Permissions.ExportReports
        };

        // Public / Basic User permissions (read-only public coverage and speedtests)
        var publicPermissions = new List<string>
        {
            Permissions.ViewCells,
            Permissions.ViewCellulars,
            Permissions.ViewSpeedtests,
            Permissions.ViewNAPs,
            Permissions.ViewRegions,
            Permissions.ViewUserRegions,
            Permissions.CreateUserRegions
        };

        var systemRoles = new[]
        {
            new Role
            {
                Name = "SuperAdmin",
                Key = Roles.SuperAdmin,
                Description = "Super administrator with full system access across all platform modules",
                IsSystemRole = true,
                Scope = Roles.ScopePlatform,
                RegionId = null,
                Permissions = allPermissions,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "System",
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = "System",
                IsDeleted = false
            },
            new Role
            {
                Name = "NTC National",
                Key = Roles.NtcNational,
                Description = "Central Office administrator with national visibility and reporting oversight",
                IsSystemRole = true,
                Scope = Roles.ScopePlatform,
                RegionId = null,
                Permissions = nationalPermissions,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "System",
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = "System",
                IsDeleted = false
            },
            new Role
            {
                Name = "Regional Admin",
                Key = Roles.RegionalAdmin,
                Description = "Regional administrator with authority over regional assets, users, and custom roles",
                IsSystemRole = true,
                Scope = Roles.ScopeRegional,
                RegionId = null,
                Permissions = regionalAdminPermissions,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "System",
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = "System",
                IsDeleted = false
            },
            new Role
            {
                Name = "Admin User",
                Key = Roles.Admin,
                Description = "Administrative user with elevated privileges",
                IsSystemRole = true,
                Scope = Roles.ScopeRegional,
                RegionId = null,
                Permissions = regionalAdminPermissions,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "System",
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = "System",
                IsDeleted = false
            },
            new Role
            {
                Name = "Regional Staff",
                Key = Roles.RegionalStaff,
                Description = "Regional operational staff with report generation and data validation access",
                IsSystemRole = true,
                Scope = Roles.ScopeRegional,
                RegionId = null,
                Permissions = regionalStaffPermissions,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "System",
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = "System",
                IsDeleted = false
            },
            new Role
            {
                Name = "Field Engineer",
                Key = Roles.FieldEngineer,
                Description = "Field engineer conducting drive tests, speedtests, and cell inspections",
                IsSystemRole = true,
                Scope = Roles.ScopeRegional,
                RegionId = null,
                Permissions = fieldEngineerPermissions,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "System",
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = "System",
                IsDeleted = false
            },
            new Role
            {
                Name = "Field Officer",
                Key = Roles.FieldOfficer,
                Description = "Field officer with limited operational access",
                IsSystemRole = true,
                Scope = Roles.ScopeRegional,
                RegionId = null,
                Permissions = fieldEngineerPermissions,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "System",
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = "System",
                IsDeleted = false
            },
            new Role
            {
                Name = "User",
                Key = Roles.User,
                Description = "Standard public user with basic access",
                IsSystemRole = true,
                Scope = Roles.ScopePlatform,
                RegionId = null,
                Permissions = publicPermissions,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "System",
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = "System",
                IsDeleted = false
            }
        };

        try
        {
            var existingRoles = await context.Roles.ToListAsync();

            foreach (var systemRole in systemRoles)
            {
                var existing = existingRoles.FirstOrDefault(r => r.Key == systemRole.Key);
                if (existing == null)
                {
                    context.Roles.Add(systemRole);
                    logger.LogInformation("Added new system role: {RoleKey} ({RoleName})", systemRole.Key, systemRole.Name);
                }
                else
                {
                    // Ensure system role flags and permissions are up to date
                    existing.IsSystemRole = true;
                    existing.Scope = systemRole.Scope;
                    if (existing.Permissions == null || existing.Permissions.Count == 0)
                    {
                        existing.Permissions = systemRole.Permissions;
                    }
                    existing.UpdatedAt = DateTime.UtcNow;
                    existing.UpdatedBy = "System";
                }
            }

            await context.SaveChangesAsync();
            logger.LogInformation("Role seeding completed successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during role seeding: {Message}", ex.Message);
            throw;
        }
    }
}
