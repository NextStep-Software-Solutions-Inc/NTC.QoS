namespace NTC.QoS.Domain.Interfaces;

public interface IUserContextService
{
    string? GetCurrentUserId(); // Or GetCurrentUserName(), GetCurrentUserEmail(), etc.
    
    /// <summary>
    /// Gets the current region ID from the request context (X-Region-Id header).
    /// This is used to determine which region the user is currently working in.
    /// </summary>
    Guid? GetCurrentRegionId();
}