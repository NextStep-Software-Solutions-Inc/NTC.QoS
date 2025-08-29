namespace NTC.QoS.Domain.Authorization;

/// <summary>
/// Static class containing role keys that correspond to database role records
/// Aligned with existing RoleSeeder (using snake_case for existing roles)
/// </summary>
public static class Roles
{
    // Existing roles from RoleSeeder (snake_case)
    public const string SuperAdmin = "super_admin";
    public const string Admin = "admin";
    public const string FieldOfficer = "field_officer";
    public const string User = "user";
    
    // Additional roles (snake_case for consistency)
    public const string Manager = "manager";
    public const string RegionalManager = "regional_manager";
    public const string Viewer = "viewer";
    
    // Legacy kebab-case constants for backward compatibility (deprecated)
    [Obsolete("Use SuperAdmin instead")]
    public const string SuperAdminLegacy = "super-admin";
    [Obsolete("Use FieldOfficer instead")]
    public const string TechnicalStaff = "technical-staff";
    [Obsolete("Use RegionalManager instead")]
    public const string RegionalManagerLegacy = "regional-manager";
}