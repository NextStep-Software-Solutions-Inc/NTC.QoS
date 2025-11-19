using Microsoft.AspNetCore.Authorization;
using NTC.QoS.Domain.Authorization;
using NTC.QoS.Domain.Interfaces;

namespace NTC.QoS.Infrastructure.Authorization;

/// <summary>
/// Authorization handler for permission-based requirements
/// </summary>
public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IUserAuthorizationService _userAuthorizationService;

    public PermissionAuthorizationHandler(IUserAuthorizationService userAuthorizationService)
    {
        _userAuthorizationService = userAuthorizationService;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var hasPermission = await _userAuthorizationService.HasPermissionAsync(requirement.Permission);

        if (hasPermission)
        {
            context.Succeed(requirement);
        }
    }
}