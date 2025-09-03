using NTC.QoS.Domain.Interfaces;

namespace NTC.QoS.Infrastructure.Services;

public class SystemTimeProvider : ITimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
