namespace NTC.QoS.Application.Interfaces;

public interface ICacheableRequest
{
    string CacheKey { get; }
    TimeSpan? AbsoluteExpirationRelativeToNow { get; }
    TimeSpan? SlidingExpiration { get; }
}
