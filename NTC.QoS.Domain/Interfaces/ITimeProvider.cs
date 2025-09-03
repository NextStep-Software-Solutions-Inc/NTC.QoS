namespace NTC.QoS.Domain.Interfaces;

public interface ITimeProvider
{
    DateTime UtcNow { get; }
}
