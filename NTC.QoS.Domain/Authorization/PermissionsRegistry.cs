namespace NTC.QoS.Domain.Authorization;

/// <summary>
/// Represents a static metadata definition for a system permission.
/// </summary>
public record PermissionDefinition(string Key, string Name, string Category, string Description);

/// <summary>
/// In-memory static catalog of all valid permissions in the system.
/// Serves as the single source of truth for permission keys, names, categories, and descriptions.
/// </summary>
public static class PermissionsRegistry
{
    public static readonly IReadOnlyList<PermissionDefinition> All = new[]
    {
        // Dashboard
        new PermissionDefinition(Permissions.AccessDashboard, "Access Dashboard", "Dashboard", "Can access system dashboard"),

        // Analytics
        new PermissionDefinition(Permissions.ViewAnalytics, "View Analytics", "Analytics", "Can view analytics dashboards and data"),
        new PermissionDefinition(Permissions.CreateAnalytics, "Create Analytics", "Analytics", "Can create custom analytics reports"),
        new PermissionDefinition(Permissions.UpdateAnalytics, "Update Analytics", "Analytics", "Can modify analytics configurations"),
        new PermissionDefinition(Permissions.DeleteAnalytics, "Delete Analytics", "Analytics", "Can delete analytics reports"),
        new PermissionDefinition(Permissions.ExportAnalytics, "Export Analytics", "Analytics", "Can export analytics data and reports"),
        new PermissionDefinition(Permissions.ConfigureAnalytics, "Configure Analytics", "Analytics", "Can configure analytics settings and parameters"),

        // Files
        new PermissionDefinition(Permissions.ViewFiles, "View Files", "Files", "Can view file listings"),
        new PermissionDefinition(Permissions.UploadFiles, "Upload Files", "Files", "Can upload files"),
        new PermissionDefinition(Permissions.DeleteFiles, "Delete Files", "Files", "Can delete files"),
        new PermissionDefinition(Permissions.ProcessFiles, "Process Files", "Files", "Can process uploaded files"),
        new PermissionDefinition(Permissions.DownloadFiles, "Download Files", "Files", "Can download files"),
        new PermissionDefinition(Permissions.UpdateFiles, "Update Files", "Files", "Can update file metadata"),

        // Users
        new PermissionDefinition(Permissions.ViewUsers, "View Users", "User Management", "Can view user listings"),
        new PermissionDefinition(Permissions.CreateUsers, "Create Users", "User Management", "Can create new users"),
        new PermissionDefinition(Permissions.UpdateUsers, "Update Users", "User Management", "Can update user information"),
        new PermissionDefinition(Permissions.DeleteUsers, "Delete Users", "User Management", "Can delete users"),
        new PermissionDefinition(Permissions.InviteUsers, "Invite Users", "User Management", "Can invite new users via email"),

        // Roles
        new PermissionDefinition(Permissions.ViewRoles, "View Roles", "Role Management", "Can view role listings"),
        new PermissionDefinition(Permissions.CreateRoles, "Create Roles", "Role Management", "Can create new roles"),
        new PermissionDefinition(Permissions.UpdateRoles, "Update Roles", "Role Management", "Can update role information"),
        new PermissionDefinition(Permissions.DeleteRoles, "Delete Roles", "Role Management", "Can delete roles"),

        // Permissions
        new PermissionDefinition(Permissions.ViewPermissions, "View Permissions", "Permission Management", "Can view permission listings"),
        new PermissionDefinition(Permissions.CreatePermissions, "Create Permissions", "Permission Management", "Can create new permissions"),
        new PermissionDefinition(Permissions.UpdatePermissions, "Update Permissions", "Permission Management", "Can update permission information"),
        new PermissionDefinition(Permissions.DeletePermissions, "Delete Permissions", "Permission Management", "Can delete permissions"),

        // Role Permissions
        new PermissionDefinition(Permissions.ViewRolePermissions, "View Role Permissions", "Role Permission Management", "Can view role permission assignments"),
        new PermissionDefinition(Permissions.CreateRolePermissions, "Create Role Permissions", "Role Permission Management", "Can assign permissions to roles"),
        new PermissionDefinition(Permissions.DeleteRolePermissions, "Delete Role Permissions", "Role Permission Management", "Can remove permissions from roles"),

        // Regions
        new PermissionDefinition(Permissions.ViewRegions, "View Regions", "Region Management", "Can view region listings"),
        new PermissionDefinition(Permissions.CreateRegions, "Create Regions", "Region Management", "Can create new regions"),
        new PermissionDefinition(Permissions.UpdateRegions, "Update Regions", "Region Management", "Can update region information"),
        new PermissionDefinition(Permissions.DeleteRegions, "Delete Regions", "Region Management", "Can delete regions"),

        // Provinces
        new PermissionDefinition(Permissions.ViewProvinces, "View Provinces", "Location Management", "Can view province listings"),
        new PermissionDefinition(Permissions.CreateProvinces, "Create Provinces", "Location Management", "Can create new provinces"),
        new PermissionDefinition(Permissions.UpdateProvinces, "Update Provinces", "Location Management", "Can update province information"),
        new PermissionDefinition(Permissions.DeleteProvinces, "Delete Provinces", "Location Management", "Can delete provinces"),

        // Municipalities
        new PermissionDefinition(Permissions.ViewMunicipalities, "View Municipalities", "Location Management", "Can view municipality listings"),
        new PermissionDefinition(Permissions.CreateMunicipalities, "Create Municipalities", "Location Management", "Can create new municipalities"),
        new PermissionDefinition(Permissions.UpdateMunicipalities, "Update Municipalities", "Location Management", "Can update municipality information"),
        new PermissionDefinition(Permissions.DeleteMunicipalities, "Delete Municipalities", "Location Management", "Can delete municipalities"),

        // Barangays
        new PermissionDefinition(Permissions.ViewBarangays, "View Barangays", "Location Management", "Can view barangay listings"),
        new PermissionDefinition(Permissions.CreateBarangays, "Create Barangays", "Location Management", "Can create new barangays"),
        new PermissionDefinition(Permissions.UpdateBarangays, "Update Barangays", "Location Management", "Can update barangay information"),
        new PermissionDefinition(Permissions.DeleteBarangays, "Delete Barangays", "Location Management", "Can delete barangays"),

        // Barangay Coverage
        new PermissionDefinition(Permissions.ViewBarangayCoverage, "View Barangay Coverage", "Coverage Management", "Can view barangay coverage records"),
        new PermissionDefinition(Permissions.CreateBarangayCoverage, "Create Barangay Coverage", "Coverage Management", "Can create barangay coverage records"),
        new PermissionDefinition(Permissions.UpdateBarangayCoverage, "Update Barangay Coverage", "Coverage Management", "Can update barangay coverage records"),
        new PermissionDefinition(Permissions.DeleteBarangayCoverage, "Delete Barangay Coverage", "Coverage Management", "Can delete barangay coverage records"),

        // Zones
        new PermissionDefinition(Permissions.ViewZones, "View Zones", "Location Management", "Can view zone listings"),
        new PermissionDefinition(Permissions.CreateZones, "Create Zones", "Location Management", "Can create new zones"),
        new PermissionDefinition(Permissions.UpdateZones, "Update Zones", "Location Management", "Can update zone information"),
        new PermissionDefinition(Permissions.DeleteZones, "Delete Zones", "Location Management", "Can delete zones"),

        // User Regions
        new PermissionDefinition(Permissions.ViewUserRegions, "View User Regions", "User Region Management", "Can view user region assignments"),
        new PermissionDefinition(Permissions.CreateUserRegions, "Create User Regions", "User Region Management", "Can assign users to regions"),
        new PermissionDefinition(Permissions.UpdateUserRegions, "Update User Regions", "User Region Management", "Can update user region assignments"),
        new PermissionDefinition(Permissions.DeleteUserRegions, "Delete User Regions", "User Region Management", "Can remove user region assignments"),

        // Network Configuration
        new PermissionDefinition(Permissions.ViewProviders, "View Providers", "Network Configuration", "Can view service provider listings"),
        new PermissionDefinition(Permissions.CreateProviders, "Create Providers", "Network Configuration", "Can create new service providers"),
        new PermissionDefinition(Permissions.UpdateProviders, "Update Providers", "Network Configuration", "Can update service provider information"),
        new PermissionDefinition(Permissions.DeleteProviders, "Delete Providers", "Network Configuration", "Can delete service providers"),

        new PermissionDefinition(Permissions.ViewServices, "View Services", "Network Configuration", "Can view service listings"),
        new PermissionDefinition(Permissions.CreateServices, "Create Services", "Network Configuration", "Can create new services"),
        new PermissionDefinition(Permissions.UpdateServices, "Update Services", "Network Configuration", "Can update service information"),
        new PermissionDefinition(Permissions.DeleteServices, "Delete Services", "Network Configuration", "Can delete services"),

        new PermissionDefinition(Permissions.ViewServiceProviders, "View Service Providers", "Network Configuration", "Can view service provider mappings"),
        new PermissionDefinition(Permissions.CreateServiceProviders, "Create Service Providers", "Network Configuration", "Can create service provider mappings"),
        new PermissionDefinition(Permissions.UpdateServiceProviders, "Update Service Providers", "Network Configuration", "Can update service provider mappings"),
        new PermissionDefinition(Permissions.DeleteServiceProviders, "Delete Service Providers", "Network Configuration", "Can delete service provider mappings"),

        new PermissionDefinition(Permissions.ViewGenerations, "View Generations", "Network Configuration", "Can view technology generations"),
        new PermissionDefinition(Permissions.CreateGenerations, "Create Generations", "Network Configuration", "Can create new technology generations"),
        new PermissionDefinition(Permissions.UpdateGenerations, "Update Generations", "Network Configuration", "Can update technology generations"),
        new PermissionDefinition(Permissions.DeleteGenerations, "Delete Generations", "Network Configuration", "Can delete technology generations"),

        new PermissionDefinition(Permissions.ViewTechnologies, "View Technologies", "Network Configuration", "Can view technology listings"),
        new PermissionDefinition(Permissions.CreateTechnologies, "Create Technologies", "Network Configuration", "Can create new technologies"),
        new PermissionDefinition(Permissions.UpdateTechnologies, "Update Technologies", "Network Configuration", "Can update technology information"),
        new PermissionDefinition(Permissions.DeleteTechnologies, "Delete Technologies", "Network Configuration", "Can delete technologies"),

        new PermissionDefinition(Permissions.ViewBands, "View Bands", "Network Configuration", "Can view frequency band listings"),
        new PermissionDefinition(Permissions.CreateBands, "Create Bands", "Network Configuration", "Can create new frequency bands"),
        new PermissionDefinition(Permissions.UpdateBands, "Update Bands", "Network Configuration", "Can update frequency band information"),
        new PermissionDefinition(Permissions.DeleteBands, "Delete Bands", "Network Configuration", "Can delete frequency bands"),

        new PermissionDefinition(Permissions.ViewSystemTypes, "View System Types", "Network Configuration", "Can view system type listings"),
        new PermissionDefinition(Permissions.CreateSystemTypes, "Create System Types", "Network Configuration", "Can create new system types"),
        new PermissionDefinition(Permissions.UpdateSystemTypes, "Update System Types", "Network Configuration", "Can update system type information"),
        new PermissionDefinition(Permissions.DeleteSystemTypes, "Delete System Types", "Network Configuration", "Can delete system types"),

        new PermissionDefinition(Permissions.ViewAreaStudyTypes, "View Area Study Types", "Network Configuration", "Can view area study type listings"),
        new PermissionDefinition(Permissions.CreateAreaStudyTypes, "Create Area Study Types", "Network Configuration", "Can create new area study types"),
        new PermissionDefinition(Permissions.UpdateAreaStudyTypes, "Update Area Study Types", "Network Configuration", "Can update area study type information"),
        new PermissionDefinition(Permissions.DeleteAreaStudyTypes, "Delete Area Study Types", "Network Configuration", "Can delete area study types"),

        new PermissionDefinition(Permissions.ViewSignalLevels, "View Signal Levels", "Network Configuration", "Can view signal level listings"),
        new PermissionDefinition(Permissions.CreateSignalLevels, "Create Signal Levels", "Network Configuration", "Can create signal levels"),
        new PermissionDefinition(Permissions.UpdateSignalLevels, "Update Signal Levels", "Network Configuration", "Can update signal levels"),
        new PermissionDefinition(Permissions.DeleteSignalLevels, "Delete Signal Levels", "Network Configuration", "Can delete signal levels"),

        new PermissionDefinition(Permissions.ViewSignalLevelEntries, "View Signal Level Entries", "Network Configuration", "Can view signal level entry listings"),
        new PermissionDefinition(Permissions.CreateSignalLevelEntries, "Create Signal Level Entries", "Network Configuration", "Can create signal level entries"),
        new PermissionDefinition(Permissions.UpdateSignalLevelEntries, "Update Signal Level Entries", "Network Configuration", "Can update signal level entries"),
        new PermissionDefinition(Permissions.DeleteSignalLevelEntries, "Delete Signal Level Entries", "Network Configuration", "Can delete signal level entries"),

        // Links & Allocations
        new PermissionDefinition(Permissions.ViewFrequencyAllocations, "View Frequency Allocations", "Frequency Management", "Can view frequency allocation listings"),
        new PermissionDefinition(Permissions.CreateFrequencyAllocations, "Create Frequency Allocations", "Frequency Management", "Can create new frequency allocations"),
        new PermissionDefinition(Permissions.UpdateFrequencyAllocations, "Update Frequency Allocations", "Frequency Management", "Can update frequency allocation information"),
        new PermissionDefinition(Permissions.DeleteFrequencyAllocations, "Delete Frequency Allocations", "Frequency Management", "Can delete frequency allocations"),

        new PermissionDefinition(Permissions.ViewRegulatoryAllocations, "View Regulatory Allocations", "Frequency Management", "Can view regulatory allocations"),
        new PermissionDefinition(Permissions.CreateRegulatoryAllocations, "Create Regulatory Allocations", "Frequency Management", "Can create regulatory allocations"),
        new PermissionDefinition(Permissions.UpdateRegulatoryAllocations, "Update Regulatory Allocations", "Frequency Management", "Can update regulatory allocations"),
        new PermissionDefinition(Permissions.DeleteRegulatoryAllocations, "Delete Regulatory Allocations", "Frequency Management", "Can delete regulatory allocations"),

        // Networks & Sectors
        new PermissionDefinition(Permissions.ViewNetworks, "View Networks", "Infrastructure", "Can view network listings"),
        new PermissionDefinition(Permissions.CreateNetworks, "Create Networks", "Infrastructure", "Can create new networks"),
        new PermissionDefinition(Permissions.UpdateNetworks, "Update Networks", "Infrastructure", "Can update network information"),
        new PermissionDefinition(Permissions.DeleteNetworks, "Delete Networks", "Infrastructure", "Can delete networks"),

        new PermissionDefinition(Permissions.ViewSectors, "View Sectors", "Infrastructure", "Can view sector listings"),
        new PermissionDefinition(Permissions.CreateSectors, "Create Sectors", "Infrastructure", "Can create new sectors"),
        new PermissionDefinition(Permissions.UpdateSectors, "Update Sectors", "Infrastructure", "Can update sector information"),
        new PermissionDefinition(Permissions.DeleteSectors, "Delete Sectors", "Infrastructure", "Can delete sectors"),

        // Broadband & Cellular
        new PermissionDefinition(Permissions.ViewCells, "View Cells", "Broadband", "Can view cell tower listings"),
        new PermissionDefinition(Permissions.CreateCells, "Create Cells", "Broadband", "Can create new cell towers"),
        new PermissionDefinition(Permissions.UpdateCells, "Update Cells", "Broadband", "Can update cell tower information"),
        new PermissionDefinition(Permissions.DeleteCells, "Delete Cells", "Broadband", "Can delete cell towers"),

        new PermissionDefinition(Permissions.ViewCellSectors, "View Cell Sectors", "Broadband", "Can view cell sector listings"),
        new PermissionDefinition(Permissions.CreateCellSectors, "Create Cell Sectors", "Broadband", "Can create new cell sectors"),
        new PermissionDefinition(Permissions.UpdateCellSectors, "Update Cell Sectors", "Broadband", "Can update cell sector information"),
        new PermissionDefinition(Permissions.DeleteCellSectors, "Delete Cell Sectors", "Broadband", "Can delete cell sectors"),

        new PermissionDefinition(Permissions.ViewNAPs, "View NAPs", "Broadband", "Can view NAP listings"),
        new PermissionDefinition(Permissions.CreateNAPs, "Create NAPs", "Broadband", "Can create new NAPs"),
        new PermissionDefinition(Permissions.UpdateNAPs, "Update NAPs", "Broadband", "Can update NAP information"),
        new PermissionDefinition(Permissions.DeleteNAPs, "Delete NAPs", "Broadband", "Can delete NAPs"),

        new PermissionDefinition(Permissions.ViewSpeedtests, "View Speedtests", "Broadband", "Can view speedtest listings"),
        new PermissionDefinition(Permissions.CreateSpeedtests, "Create Speedtests", "Broadband", "Can create new speedtests"),
        new PermissionDefinition(Permissions.UpdateSpeedtests, "Update Speedtests", "Broadband", "Can update speedtest information"),
        new PermissionDefinition(Permissions.DeleteSpeedtests, "Delete Speedtests", "Broadband", "Can delete speedtests"),

        new PermissionDefinition(Permissions.ViewCellulars, "View Cellulars", "Broadband", "Can view cellular listings"),
        new PermissionDefinition(Permissions.CreateCellulars, "Create Cellulars", "Broadband", "Can create new cellular records"),
        new PermissionDefinition(Permissions.DeleteCellulars, "Delete Cellulars", "Broadband", "Can delete cellular records"),

        // Reports
        new PermissionDefinition(Permissions.ViewReports, "View Reports", "Reports", "Can view reports"),
        new PermissionDefinition(Permissions.CreateReports, "Create Reports", "Reports", "Can create reports"),
        new PermissionDefinition(Permissions.ExportReports, "Export Reports", "Reports", "Can export reports"),

        // System
        new PermissionDefinition(Permissions.SystemAdministration, "System Administration", "System Administration", "Full system administration access"),
        new PermissionDefinition(Permissions.ViewLogs, "View Logs", "System Administration", "Can view system logs"),
        new PermissionDefinition(Permissions.ManageSettings, "Manage Settings", "System Administration", "Can manage system settings"),

        // GenAI
        new PermissionDefinition(Permissions.ChatWithAI, "Chat with AI", "AI Assistant", "Can access AI chat assistant"),
        new PermissionDefinition(Permissions.GenerateReports, "Generate AI Reports", "AI Assistant", "Can generate reports using AI")
    };

    public static readonly HashSet<string> AllKeys = All.Select(p => p.Key).ToHashSet();

    /// <summary>
    /// Checks if the given permission key is a recognized system permission.
    /// </summary>
    public static bool IsValidPermission(string key) => !string.IsNullOrEmpty(key) && AllKeys.Contains(key);

    /// <summary>
    /// Platform-level permissions that cannot be granted by Regional Admins.
    /// </summary>
    public static readonly HashSet<string> PlatformOnlyPermissions = new(StringComparer.OrdinalIgnoreCase)
    {
        Permissions.SystemAdministration,
        Permissions.ViewLogs,
        Permissions.ManageSettings,
        Permissions.CreateRegions,
        Permissions.UpdateRegions,
        Permissions.DeleteRegions,
        Permissions.CreateRoles,
        Permissions.UpdateRoles,
        Permissions.DeleteRoles,
        Permissions.CreatePermissions,
        Permissions.UpdatePermissions,
        Permissions.DeletePermissions,
        Permissions.CreateRolePermissions,
        Permissions.DeleteRolePermissions
    };

    /// <summary>
    /// Checks if a permission is safe to be granted within a regional custom role.
    /// </summary>
    public static bool IsAllowedForRegionalScope(string key) =>
        IsValidPermission(key) && !PlatformOnlyPermissions.Contains(key);
}
