namespace NTC.QoS.Domain.Interfaces;

/// <summary>
/// Service for user region assignment and management.
/// </summary>
public interface IUserRegionService
{
    /// <summary>
    /// Assigns all active regions to the specified user with Active status.
    /// Also transitions any existing non-active region assignments for the user to Active.
    /// </summary>
    Task AssignAllRegionsToUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
