using Microsoft.AspNetCore.Authorization;
using NTC.QoS.Domain.Authorization;
using NTC.QoS.Domain.Interfaces;

namespace NTC.QoS.Infrastructure.Authorization;

/// <summary>
/// Authorization handler for organization-based requirements
/// </summary>
public class OrganizationAuthorizationHandler : AuthorizationHandler<OrganizationRequirement>
{
    private readonly IUserAuthorizationService _userAuthorizationService;
    private readonly IRepository<Domain.Entities.Organization> _organizationRepository;

    public OrganizationAuthorizationHandler(
        IUserAuthorizationService userAuthorizationService,
        IRepository<Domain.Entities.Organization> organizationRepository)
    {
        _userAuthorizationService = userAuthorizationService;
        _organizationRepository = organizationRepository;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OrganizationRequirement requirement)
    {
        var currentUser = await _userAuthorizationService.GetCurrentUserAsync();

        // Early exit if user has no organization
        if (currentUser?.Organization == null)
        {
            return; // Authorization fails
        }

        // Handle same organization requirement
        if (requirement.RequireSameOrganization &&
            !string.IsNullOrEmpty(requirement.Organization) == false &&
            !requirement.OrganizationId.HasValue)
        {
            // User just needs to belong to any organization (their own)
            context.Succeed(requirement);
            return;
        }

        // Handle specific organization ID matching
        if (requirement.OrganizationId.HasValue)
        {
            if (currentUser.Organization.Id == requirement.OrganizationId.Value)
            {
                context.Succeed(requirement);
                return;
            }
        }

        // Handle specific organization name matching
        if (!string.IsNullOrEmpty(requirement.Organization))
        {
            var comparisonType = requirement.CaseSensitive
                ? StringComparison.Ordinal
                : StringComparison.OrdinalIgnoreCase;

            // Direct name comparison using current user's organization data
            if (string.Equals(currentUser.Organization.Name, requirement.Organization, comparisonType))
            {
                context.Succeed(requirement);
                return;
            }

            // Alternative: Database lookup for more robust verification
            // This is useful when the cached user data might be stale
            try
            {
                var organizationQuery = requirement.CaseSensitive
                    ? _organizationRepository.Query(org => org.Name == requirement.Organization)
                    : _organizationRepository.Query(org => org.Name.ToLower() == requirement.Organization.ToLower());

                var targetOrganization = await _organizationRepository.FirstOrDefaultAsync(organizationQuery);

                if (targetOrganization != null && currentUser.Organization.Id == targetOrganization.Id)
                {
                    context.Succeed(requirement);
                    return;
                }
            }
            catch (Exception)
            {
                // If database lookup fails, fall back to the direct comparison result
                // We already checked the direct comparison above, so if we reach here, authorization fails
            }
        }

        // If we reach here, authorization failed
        // The requirement was not satisfied
    }
}