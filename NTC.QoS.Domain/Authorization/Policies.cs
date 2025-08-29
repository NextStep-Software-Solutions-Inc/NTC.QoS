namespace NTC.QoS.Domain.Authorization;

/// <summary>
/// Static class containing authorization policy names
/// </summary>
public static class Policies
{
    // Permission-based policies
    public const string CanViewFiles = "CanViewFiles";
    public const string CanUploadFiles = "CanUploadFiles";
    public const string CanDeleteFiles = "CanDeleteFiles";
    public const string CanManageUsers = "CanManageUsers";
    public const string CanManageRoles = "CanManageRoles";
    public const string CanManagePermissions = "CanManagePermissions";
    public const string CanViewReports = "CanViewReports";
    public const string CanManageOrganizations = "CanManageOrganizations";
    public const string CanViewCells = "CanViewCells";
    public const string CanManageCells = "CanManageCells";
    public const string CanViewNetworks = "CanViewNetworks";
    public const string CanManageNetworks = "CanManageNetworks";
    public const string CanViewProviders = "CanViewProviders";
    public const string CanManageProviders = "CanManageProviders";

    // Role-based policies
    public const string AdminOnly = "AdminOnly";
    public const string ManagerOnly = "ManagerOnly";
    public const string UserOnly = "UserOnly";
    public const string AdminOrManager = "AdminOrManager";

    // Organization-based policies
    public const string SameOrganization = "SameOrganization";
    public const string SpecificOrganization = "SpecificOrganization";

    // Region-based policies
    public const string RegionAccess = "RegionAccess";
    public const string SpecificRegion = "SpecificRegion";

    // Combined policies
    public const string AdminOrSameOrganization = "AdminOrSameOrganization";
    public const string ManagerWithRegionAccess = "ManagerWithRegionAccess";
}