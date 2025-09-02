namespace NTC.QoS.Domain.Authorization;

/// <summary>
/// Static class containing permission keys that correspond to database permission records
/// </summary>
public static class Permissions
{
    // Dashboard access permission
    public const string AccessDashboard = "dashboard.access";

    // Analytics access permission

    // File management permissions
    public const string ViewFiles = "files.view";
    public const string UploadFiles = "files.upload";
    public const string DeleteFiles = "files.delete";
    public const string ProcessFiles = "files.process";

    // User management permissions
    public const string ViewUsers = "users.view";
    public const string CreateUsers = "users.create";
    public const string UpdateUsers = "users.update";
    public const string DeleteUsers = "users.delete";

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

    // Organization management permissions
    public const string ViewOrganizations = "organizations.view";
    public const string CreateOrganizations = "organizations.create";
    public const string UpdateOrganizations = "organizations.update";
    public const string DeleteOrganizations = "organizations.delete";

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

    public const string ViewFrequencyAllocations = "links.view";
    public const string CreateFrequencyAllocations = "links.create";
    public const string UpdateFrequencyAllocations = "links.update";
    public const string DeleteFrequencyAllocations = "links.delete";

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
    public const string UpdateCellulars = "cellulars.update";
    public const string DeleteCellulars = "cellulars.delete";

    // Reporting permissions
    public const string ViewReports = "reports.view";
    public const string CreateReports = "reports.create";
    public const string ExportReports = "reports.export";

    // System administration permissions
    public const string SystemAdministration = "system.admin";
    public const string ViewLogs = "logs.view";
    public const string ManageSettings = "settings.manage";
}