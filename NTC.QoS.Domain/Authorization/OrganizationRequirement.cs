using Microsoft.AspNetCore.Authorization;

namespace NTC.QoS.Domain.Authorization;

/// <summary>
/// Requirement for organization-based authorization
/// </summary>
public class OrganizationRequirement : Microsoft.AspNetCore.Authorization.IAuthorizationRequirement
{
    public string? Organization { get; }
    public bool RequireSameOrganization { get; }

    public OrganizationRequirement(string? organization = null, bool requireSameOrganization = true)
    {
        Organization = organization;
        RequireSameOrganization = requireSameOrganization;
    }
}