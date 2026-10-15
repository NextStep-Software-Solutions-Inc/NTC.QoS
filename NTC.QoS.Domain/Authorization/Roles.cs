using NTC.QoS.Domain.Entities;

namespace NTC.QoS.Domain.Authorization;

/// <summary>
/// Static class containing role keys that correspond to database role records
/// Aligned with existing RoleSeeder (using snake_case for existing roles)
/// </summary>
public static class Roles
{
    // Role scopes
    public const string ScopePlatform = "Platform";
    public const string ScopeRegional = "Regional";

    // Platform-level roles
    public const string SuperAdmin = "super_admin";
    public const string NtcNational = "ntc_national";

    // Regional-level roles
    public const string RegionalAdmin = "regional_admin";
    public const string Admin = "admin"; // Backward-compatible alias for existing seeded admin
    public const string RegionalStaff = "regional_staff";
    public const string FieldEngineer = "field_engineer";
    public const string FieldOfficer = "field_officer"; // Backward-compatible alias for existing field officer

    // Additional standard roles
    public const string Manager = "manager";
    public const string RegionalManager = "regional_manager";
    public const string Viewer = "viewer";

    // Public basic role
    public const string User = "user";

    // Legacy kebab-case constants for backward compatibility (deprecated)
    [Obsolete("Use SuperAdmin instead")]
    public const string SuperAdminLegacy = "super-admin";
    [Obsolete("Use FieldOfficer instead")]
    public const string TechnicalStaff = "technical-staff";
    [Obsolete("Use RegionalManager instead")]
    public const string RegionalManagerLegacy = "regional-manager";

    /// <summary>
    /// Determines whether the specified role key represents a SuperAdmin role.
    /// Supports both standard snake_case ('super_admin') and legacy kebab-case ('super-admin').
    /// </summary>
    public static bool IsSuperAdmin(string? roleKey)
    {
        if (string.IsNullOrWhiteSpace(roleKey))
            return false;

#pragma warning disable CS0618 // Type or member is obsolete - used for backward compatibility
        return string.Equals(roleKey, SuperAdmin, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(roleKey, SuperAdminLegacy, StringComparison.OrdinalIgnoreCase);
#pragma warning restore CS0618
    }

    /// <summary>
    /// Determines whether the specified Role entity represents a SuperAdmin role.
    /// </summary>
    public static bool IsSuperAdmin(Role? role) => role != null && IsSuperAdmin(role.Key);

    /// <summary>
    /// Determines whether the specified role key represents a Platform-level role
    /// with implicit nationwide jurisdiction (Super Admin or NTC National).
    /// </summary>
    public static bool IsPlatformRole(string? roleKey)
    {
        if (string.IsNullOrWhiteSpace(roleKey))
            return false;

        return IsSuperAdmin(roleKey) ||
               string.Equals(roleKey, NtcNational, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Determines whether the specified Role entity represents a Platform-level role.
    /// </summary>
    public static bool IsPlatformRole(Role? role)
    {
        if (role == null) return false;
        if (string.Equals(role.Scope, ScopePlatform, StringComparison.OrdinalIgnoreCase)) return true;
        return IsPlatformRole(role.Key);
    }
}