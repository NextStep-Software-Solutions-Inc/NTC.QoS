using Microsoft.AspNetCore.Authorization;

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
    public RequireOrganizationAttribute(string? organizationName = null, bool caseSensitive = false)
    {
        if (string.IsNullOrEmpty(organizationName))
        {
            Policy = "Organization:SameOrganization";
        }
        else
        {
            Policy = $"Organization:{organizationName}{(caseSensitive ? ":CaseSensitive" : "")}";
        }
    }
}

/// <summary>
/// Attribute for organization ID-based authorization
/// </summary>
public class RequireOrganizationIdAttribute : AuthorizeAttribute
{
    public RequireOrganizationIdAttribute(string organizationId)
    {
        Policy = $"OrganizationId:{organizationId}";
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