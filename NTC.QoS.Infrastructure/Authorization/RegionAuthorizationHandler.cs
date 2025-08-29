using Microsoft.AspNetCore.Authorization;
using NTC.QoS.Domain.Authorization;
using NTC.QoS.Domain.Interfaces;

namespace NTC.QoS.Infrastructure.Authorization;

/// <summary>
/// Authorization handler for region-based requirements
/// </summary>
public class RegionAuthorizationHandler : AuthorizationHandler<RegionRequirement>
{
    private readonly IUserAuthorizationService _userAuthorizationService;

    public RegionAuthorizationHandler(IUserAuthorizationService userAuthorizationService)
    {
        _userAuthorizationService = userAuthorizationService;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        RegionRequirement requirement)
    {
        var accessibleRegions = await _userAuthorizationService.GetAccessibleRegionsAsync();

        if (requirement.RequireAnyRegion && accessibleRegions.Count > 0)
        {
            context.Succeed(requirement);
            return;
        }

        if (requirement.AllowedRegionIds.Count > 0)
        {
            var hasAccess = requirement.AllowedRegionIds.Any(regionId => 
                accessibleRegions.Contains(regionId));
            
            if (hasAccess)
            {
                context.Succeed(requirement);
            }
        }
        else if (requirement.RequireAnyRegion && accessibleRegions.Count > 0)
        {
            context.Succeed(requirement);
        }
    }
}