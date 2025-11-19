using Microsoft.AspNetCore.Authorization;

namespace NTC.QoS.Domain.Authorization;

/// <summary>
/// Requirement for role-based authorization
/// </summary>
public class RoleRequirement : Microsoft.AspNetCore.Authorization.IAuthorizationRequirement
{
    public string Role { get; }

    public RoleRequirement(string role)
    {
        Role = role ?? throw new ArgumentNullException(nameof(role));
    }
}