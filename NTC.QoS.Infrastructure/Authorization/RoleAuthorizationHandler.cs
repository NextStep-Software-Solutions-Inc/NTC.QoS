using Microsoft.AspNetCore.Authorization;
using NTC.QoS.Domain.Authorization;
using NTC.QoS.Domain.Interfaces;

namespace NTC.QoS.Infrastructure.Authorization;

/// <summary>
/// Authorization handler for role-based requirements
/// </summary>
public class RoleAuthorizationHandler : AuthorizationHandler<RoleRequirement>
{
    private readonly IUserAuthorizationService _userAuthorizationService;

    public RoleAuthorizationHandler(IUserAuthorizationService userAuthorizationService)
    {
        _userAuthorizationService = userAuthorizationService;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        RoleRequirement requirement)
    {
        var hasRole = await _userAuthorizationService.HasRoleAsync(requirement.Role);

        if (hasRole)
        {
            context.Succeed(requirement);
        }
    }
}