namespace NTC.QoS.Domain.Interfaces;

public interface IUserContextService
{
    string? GetCurrentUserId(); // Or GetCurrentUserName(), GetCurrentUserEmail(), etc.
}