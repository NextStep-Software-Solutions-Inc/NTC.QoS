namespace NTC.QoS.Domain.Authorization;

/// <summary>
/// Static class containing permission keys that correspond to database permission records
/// </summary>
public static class Permissions
{
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

    // Network infrastructure permissions
    public const string ViewCells = "cells.view";
    public const string CreateCells = "cells.create";
    public const string UpdateCells = "cells.update";
    public const string DeleteCells = "cells.delete";

    public const string ViewNetworks = "networks.view";
    public const string CreateNetworks = "networks.create";
    public const string UpdateNetworks = "networks.update";
    public const string DeleteNetworks = "networks.delete";

    public const string ViewProviders = "providers.view";
    public const string CreateProviders = "providers.create";
    public const string UpdateProviders = "providers.update";
    public const string DeleteProviders = "providers.delete";

    // Reporting permissions
    public const string ViewReports = "reports.view";
    public const string CreateReports = "reports.create";
    public const string ExportReports = "reports.export";

    // System administration permissions
    public const string SystemAdministration = "system.admin";
    public const string ViewLogs = "logs.view";
    public const string ManageSettings = "settings.manage";
}