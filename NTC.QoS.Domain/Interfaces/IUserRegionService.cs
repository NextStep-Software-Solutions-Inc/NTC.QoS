namespace NTC.QoS.Domain.Interfaces;

/// <summary>
/// Service for user region assignment and management.
/// </summary>
public interface IUserRegionService
{
    /// <summary>
    /// Assigns all active regions to the specified user with Active status and optional role assignment.
    /// Also transitions any existing non-active region assignments for the user to Active.
    /// </summary>
    Task AssignAllRegionsToUserAsync(Guid userId, Guid? roleId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all existing user region assignments for the specified user.
    /// </summary>
    Task DeleteAllRegionsForUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
