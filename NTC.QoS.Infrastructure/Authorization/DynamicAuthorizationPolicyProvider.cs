using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using NTC.QoS.Domain.Authorization;

namespace NTC.QoS.Infrastructure.Authorization;

/// <summary>
/// Custom authorization policy provider for dynamic policy creation
/// </summary>
public class DynamicAuthorizationPolicyProvider : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _fallbackPolicyProvider;

    public DynamicAuthorizationPolicyProvider(IOptions<AuthorizationOptions> options)
    {
        _fallbackPolicyProvider = new DefaultAuthorizationPolicyProvider(options);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
    {
        return _fallbackPolicyProvider.GetDefaultPolicyAsync();
    }

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync()
    {
        return _fallbackPolicyProvider.GetFallbackPolicyAsync();
    }

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        // Handle permission-based policies dynamically
        if (policyName.StartsWith("Permission:", StringComparison.OrdinalIgnoreCase))
        {
            var permission = policyName.Substring("Permission:".Length);
            var policy = new AuthorizationPolicyBuilder()
                .AddRequirements(new PermissionRequirement(permission))
                .Build();
            return Task.FromResult<AuthorizationPolicy?>(policy);
        }

        // Handle role-based policies dynamically
        if (policyName.StartsWith("Role:", StringComparison.OrdinalIgnoreCase))
        {
            var role = policyName.Substring("Role:".Length);
            var policy = new AuthorizationPolicyBuilder()
                .AddRequirements(new RoleRequirement(role))
                .Build();
            return Task.FromResult<AuthorizationPolicy?>(policy);
        }


        // Handle region-based policies dynamically
        if (policyName.StartsWith("Region:", StringComparison.OrdinalIgnoreCase))
        {
            var regionIds = policyName.Substring("Region:".Length).Split(',')
                .Where(id => Guid.TryParse(id, out _))
                .Select(Guid.Parse)
                .ToList();

            var policy = new AuthorizationPolicyBuilder()
                .AddRequirements(new RegionRequirement(regionIds))
                .Build();
            return Task.FromResult<AuthorizationPolicy?>(policy);
        }

        // Fall back to the default policy provider for predefined policies
        return _fallbackPolicyProvider.GetPolicyAsync(policyName);
    }
}