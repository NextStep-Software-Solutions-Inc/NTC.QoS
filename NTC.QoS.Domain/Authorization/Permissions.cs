namespace NTC.QoS.Domain.Authorization;

/// <summary>
/// Static class containing permission keys that correspond to database permission records
/// </summary>
public static class Permissions
{
    // Dashboard access permission
    public const string AccessDashboard = "dashboard.access";

    // Analytics access permission
    public const string ViewAnalytics = "analytics.view";
    public const string CreateAnalytics = "analytics.create";
    public const string UpdateAnalytics = "analytics.update";
    public const string DeleteAnalytics = "analytics.delete";
    public const string ExportAnalytics = "analytics.export";
    public const string ConfigureAnalytics = "analytics.configure";

    // File management permissions
    public const string ViewFiles = "files.view";
    public const string UploadFiles = "files.upload";
    public const string DeleteFiles = "files.delete";
    public const string ProcessFiles = "files.process";
    public const string DownloadFiles = "files.download";
    public const string UpdateFiles = "files.edit";

    // User management permissions
    public const string ViewUsers = "users.view";
    public const string CreateUsers = "users.create";
    public const string UpdateUsers = "users.update";
    public const string DeleteUsers = "users.delete";
    public const string InviteUsers = "users.invite";

    // Role management permissions
    public const string ViewRoles = "roles.view";
    public const string CreateRoles = "roles.create";
    public const string UpdateRoles = "roles.update";
    public const string DeleteRoles = "roles.delete";

    // Permission management permissions
    public const string ViewPermissions = "permissions.view";
    public const string CreatePermissions = "permissions.create";
    public const string UpdatePermissions = "permissions.update";
    public const string DeletePermissions = "permissions.delete";

    // Role-Permission management permissions
    public const string ViewRolePermissions = "role_permissions.view";
    public const string CreateRolePermissions = "role_permissions.create";
    public const string DeleteRolePermissions = "role_permissions.delete";

    // Region management permissions
    public const string ViewRegions = "regions.view";
    public const string CreateRegions = "regions.create";
    public const string UpdateRegions = "regions.update";
    public const string DeleteRegions = "regions.delete";

    // Province management permissions
    public const string ViewProvinces = "provinces.view";
    public const string CreateProvinces = "provinces.create";
    public const string UpdateProvinces = "provinces.update";
    public const string DeleteProvinces = "provinces.delete";

    // Municipality management permissions
    public const string ViewMunicipalities = "municipalities.view";
    public const string CreateMunicipalities = "municipalities.create";
    public const string UpdateMunicipalities = "municipalities.update";
    public const string DeleteMunicipalities = "municipalities.delete";

    // Barangay management permissions
    public const string ViewBarangays = "barangays.view";
    public const string CreateBarangays = "barangays.create";
    public const string UpdateBarangays = "barangays.update";
    public const string DeleteBarangays = "barangays.delete";

    // Barangay Coverage management permissions
    public const string ViewBarangayCoverage = "barangay_coverage.view";
    public const string CreateBarangayCoverage = "barangay_coverage.create";
    public const string UpdateBarangayCoverage = "barangay_coverage.update";
    public const string DeleteBarangayCoverage = "barangay_coverage.delete";

    // Zone management permissions
    public const string ViewZones = "zones.view";
    public const string CreateZones = "zones.create";
    public const string UpdateZones = "zones.update";
    public const string DeleteZones = "zones.delete";

    // User-Region management permissions
    public const string ViewUserRegions = "user_regions.view";
    public const string CreateUserRegions = "user_regions.create";
    public const string UpdateUserRegions = "user_regions.update";
    public const string DeleteUserRegions = "user_regions.delete";

    // Network configuration permissions
    public const string ViewProviders = "providers.view";
    public const string CreateProviders = "providers.create";
    public const string UpdateProviders = "providers.update";
    public const string DeleteProviders = "providers.delete";

    public const string ViewServices = "services.view";
    public const string CreateServices = "services.create";
    public const string UpdateServices = "services.update";
    public const string DeleteServices = "services.delete";

    public const string ViewServiceProviders = "service_providers.view";
    public const string CreateServiceProviders = "service_providers.create";
    public const string UpdateServiceProviders = "service_providers.update";
    public const string DeleteServiceProviders = "service_providers.delete";

    public const string ViewGenerations = "generations.view";
    public const string CreateGenerations = "generations.create";
    public const string UpdateGenerations = "generations.update";
    public const string DeleteGenerations = "generations.delete";

    public const string ViewTechnologies = "technologies.view";
    public const string CreateTechnologies = "technologies.create";
    public const string UpdateTechnologies = "technologies.update";
    public const string DeleteTechnologies = "technologies.delete";

    public const string ViewBands = "bands.view";
    public const string CreateBands = "bands.create";
    public const string UpdateBands = "bands.update";
    public const string DeleteBands = "bands.delete";

    public const string ViewSystemTypes = "system_types.view";
    public const string CreateSystemTypes = "system_types.create";
    public const string UpdateSystemTypes = "system_types.update";
    public const string DeleteSystemTypes = "system_types.delete";

    public const string ViewAreaStudyTypes = "area_study_types.view";
    public const string CreateAreaStudyTypes = "area_study_types.create";
    public const string UpdateAreaStudyTypes = "area_study_types.update";
    public const string DeleteAreaStudyTypes = "area_study_types.delete";

    public const string ViewSignalLevels = "signal_levels.view";
    public const string CreateSignalLevels = "signal_levels.create";
    public const string UpdateSignalLevels = "signal_levels.update";
    public const string DeleteSignalLevels = "signal_levels.delete";

    public const string ViewSignalLevelEntries = "signal_level_entries.view";
    public const string CreateSignalLevelEntries = "signal_level_entries.create";
    public const string UpdateSignalLevelEntries = "signal_level_entries.update";
    public const string DeleteSignalLevelEntries = "signal_level_entries.delete";

    // Links/Frequency Allocations permissions
    public const string ViewFrequencyAllocations = "links.view";
    public const string CreateFrequencyAllocations = "links.create";
    public const string UpdateFrequencyAllocations = "links.update";
    public const string DeleteFrequencyAllocations = "links.delete";

    // Regulatory Frequency Allocation permissions
    public const string ViewRegulatoryAllocations = "regulatory_allocations.view";
    public const string CreateRegulatoryAllocations = "regulatory_allocations.create";
    public const string UpdateRegulatoryAllocations = "regulatory_allocations.update";
    public const string DeleteRegulatoryAllocations = "regulatory_allocations.delete";

    // Network infrastructure permissions
    public const string ViewNetworks = "networks.view";
    public const string CreateNetworks = "networks.create";
    public const string UpdateNetworks = "networks.update";
    public const string DeleteNetworks = "networks.delete";

    public const string ViewSectors = "sectors.view";
    public const string CreateSectors = "sectors.create";
    public const string UpdateSectors = "sectors.update";
    public const string DeleteSectors = "sectors.delete";

    // Broadband permissions
    public const string ViewCells = "cells.view";
    public const string CreateCells = "cells.create";
    public const string UpdateCells = "cells.update";
    public const string DeleteCells = "cells.delete";

    public const string ViewCellSectors = "cellsectors.view";
    public const string CreateCellSectors = "cellsectors.create";
    public const string UpdateCellSectors = "cellsectors.update";
    public const string DeleteCellSectors = "cellsectors.delete";

    public const string ViewNAPs = "naps.view";
    public const string CreateNAPs = "naps.create";
    public const string UpdateNAPs = "naps.update";
    public const string DeleteNAPs = "naps.delete";

    public const string ViewSpeedtests = "speedtests.view";
    public const string CreateSpeedtests = "speedtests.create";
    public const string UpdateSpeedtests = "speedtests.update";
    public const string DeleteSpeedtests = "speedtests.delete";

    public const string ViewCellulars = "cellulars.view";
    public const string CreateCellulars = "cellulars.create";
    public const string DeleteCellulars = "cellulars.delete";

    // Reporting permissions
    public const string ViewReports = "reports.view";
    public const string CreateReports = "reports.create";
    public const string ExportReports = "reports.export";

    // System administration permissions
    public const string SystemAdministration = "system.admin";
    public const string ViewLogs = "logs.view";
    public const string ManageSettings = "settings.manage";

    // GenAI permissions
    public const string ChatWithAI = "genai.chat";
    public const string GenerateReports = "genai.generate_reports";
}