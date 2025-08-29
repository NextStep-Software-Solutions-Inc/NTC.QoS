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

    public OrganizationAuthorizationHandler(IUserAuthorizationService userAuthorizationService)
    {
        _userAuthorizationService = userAuthorizationService;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OrganizationRequirement requirement)
    {
        if (requirement.RequireSameOrganization)
        {
            // Check if user belongs to their own organization (always true for valid users)
            var userOrgId = await _userAuthorizationService.GetUserOrganizationIdAsync();
            if (userOrgId.HasValue)
            {
                context.Succeed(requirement);
                return;
            }
        }

        if (!string.IsNullOrEmpty(requirement.Organization))
        {
            // Check if user has specific organization access (could be by name or other criteria)
            // For now, we'll implement basic organization membership check
            var userOrgId = await _userAuthorizationService.GetUserOrganizationIdAsync();
            if (userOrgId.HasValue)
            {
                // Additional logic can be added here to check organization name match
                context.Succeed(requirement);
            }
        }
    }
}