using Microsoft.AspNetCore.Authorization;

namespace NTC.QoS.Domain.Authorization;

/// <summary>
/// Requirement for organization-based authorization
/// </summary>
public class OrganizationRequirement : Microsoft.AspNetCore.Authorization.IAuthorizationRequirement
{
    public string? Organization { get; }
    public Guid? OrganizationId { get; }
    public bool RequireSameOrganization { get; }
    public bool CaseSensitive { get; }

    /// <summary>
    /// Creates an organization requirement with flexible matching options
    /// </summary>
    /// <param name="organization">Organization name to match (optional)</param>
    /// <param name="organizationId">Organization ID to match (optional)</param>
    /// <param name="requireSameOrganization">If true, requires user to belong to their own organization</param>
    /// <param name="caseSensitive">If true, organization name matching is case-sensitive</param>
    public OrganizationRequirement(
        string? organization = null,
        Guid? organizationId = null,
        bool requireSameOrganization = true,
        bool caseSensitive = false)
    {
        Organization = organization;
        OrganizationId = organizationId;
        RequireSameOrganization = requireSameOrganization;
        CaseSensitive = caseSensitive;
    }
}