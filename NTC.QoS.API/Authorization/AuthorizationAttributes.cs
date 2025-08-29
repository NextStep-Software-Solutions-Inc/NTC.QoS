using Microsoft.AspNetCore.Authorization;
using NTC.QoS.Domain.Authorization;

namespace NTC.QoS.API.Authorization;

/// <summary>
/// Attribute for permission-based authorization
/// </summary>
public class RequirePermissionAttribute : AuthorizeAttribute
{
    public RequirePermissionAttribute(string permission)
    {
        Policy = $"Permission:{permission}";
    }
}

/// <summary>
/// Attribute for role-based authorization
/// </summary>
public class RequireRoleAttribute : AuthorizeAttribute
{
    public RequireRoleAttribute(string role)
    {
        Policy = $"Role:{role}";
    }
}

/// <summary>
/// Attribute for organization-based authorization
/// </summary>
public class RequireOrganizationAttribute : AuthorizeAttribute
{
    public RequireOrganizationAttribute(string? organization = null)
    {
        Policy = $"Organization:{organization ?? "SameOrganization"}";
    }
}

/// <summary>
/// Attribute for region-based authorization
/// </summary>
public class RequireRegionAccessAttribute : AuthorizeAttribute
{
    public RequireRegionAccessAttribute(params string[] regionIds)
    {
        Policy = $"Region:{string.Join(",", regionIds)}";
    }
}