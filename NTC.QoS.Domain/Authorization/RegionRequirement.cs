using Microsoft.AspNetCore.Authorization;

namespace NTC.QoS.Domain.Authorization;

/// <summary>
/// Requirement for region-based authorization
/// </summary>
public class RegionRequirement : Microsoft.AspNetCore.Authorization.IAuthorizationRequirement
{
    public List<Guid> AllowedRegionIds { get; }
    public bool RequireAnyRegion { get; }

    public RegionRequirement(List<Guid>? allowedRegionIds = null, bool requireAnyRegion = false)
    {
        AllowedRegionIds = allowedRegionIds ?? new List<Guid>();
        RequireAnyRegion = requireAnyRegion;
    }
}