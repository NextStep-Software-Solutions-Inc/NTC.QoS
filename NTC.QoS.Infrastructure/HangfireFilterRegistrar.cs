using Hangfire;

namespace NTC.QoS.Infrastructure;

public interface IHangfireFilterRegistrar
{
    void RegisterGlobalFilters();
}

public sealed class HangfireFilterRegistrar : IHangfireFilterRegistrar
{
    private static int _initialized; // 0 = not, 1 = done

    public void RegisterGlobalFilters()
    {
        if (Interlocked.Exchange(ref _initialized, 1) == 1) return;

        if (!GlobalJobFilters.Filters.Any(f => f.Instance is AutomaticRetryAttribute))
        {
            GlobalJobFilters.Filters.Add(new AutomaticRetryAttribute
            {
                Attempts = 3,
                LogEvents = true,
                OnAttemptsExceeded = AttemptsExceededAction.Delete
            });
        }
    }
}
