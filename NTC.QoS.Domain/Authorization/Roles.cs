namespace NTC.QoS.Domain.Authorization;

/// <summary>
/// Static class containing role keys that correspond to database role records
/// </summary>
public static class Roles
{
    public const string SuperAdmin = "super-admin";
    public const string Admin = "admin";
    public const string Manager = "manager";
    public const string User = "user";
    public const string Viewer = "viewer";
    public const string RegionalManager = "regional-manager";
    public const string TechnicalStaff = "technical-staff";
}