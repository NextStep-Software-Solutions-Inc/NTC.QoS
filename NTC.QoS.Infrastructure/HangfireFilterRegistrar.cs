using Hangfire;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NTC.QoS.Infrastructure.Configuration;

namespace NTC.QoS.Infrastructure;

public interface IHangfireFilterRegistrar
{
    void RegisterGlobalFilters();
}

public sealed class HangfireFilterRegistrar : IHangfireFilterRegistrar
{
    private static int _initialized; // 0 = not, 1 = done
    private readonly ILogger<HangfireFilterRegistrar> _logger;
    private readonly HangfireOptions _options;

    public HangfireFilterRegistrar(ILogger<HangfireFilterRegistrar> logger, IOptions<HangfireOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }

    public void RegisterGlobalFilters()
    {
        if (Interlocked.Exchange(ref _initialized, 1) == 1)
        {
            _logger.LogDebug("Hangfire global filters already registered, skipping");
            return;
        }

        _logger.LogInformation("Registering Hangfire global filters");

        try
        {
            RegisterAutomaticRetryFilter();
            // Add more filters as needed
            // RegisterCustomLoggingFilter();
            // RegisterPerformanceFilter();

            _logger.LogInformation("Hangfire global filters registered successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register Hangfire global filters");
            throw;
        }
    }

    private void RegisterAutomaticRetryFilter()
    {
        if (!GlobalJobFilters.Filters.Any(f => f.Instance is AutomaticRetryAttribute))
        {
            var retryAttribute = new AutomaticRetryAttribute
            {
                Attempts = _options.AutoRetry.Attempts,
                LogEvents = _options.AutoRetry.LogEvents,
                OnAttemptsExceeded = ParseAttemptsExceededAction(_options.AutoRetry.OnAttemptsExceeded)
            };

            GlobalJobFilters.Filters.Add(retryAttribute);

            _logger.LogDebug("AutomaticRetryAttribute registered with {Attempts} attempts, LogEvents: {LogEvents}, OnAttemptsExceeded: {OnAttemptsExceeded}",
                retryAttribute.Attempts, retryAttribute.LogEvents, retryAttribute.OnAttemptsExceeded);
        }
        else
        {
            _logger.LogDebug("AutomaticRetryAttribute already registered, skipping");
        }
    }

    private static AttemptsExceededAction ParseAttemptsExceededAction(string action)
    {
        return action?.ToUpperInvariant() switch
        {
            "DELETE" => AttemptsExceededAction.Delete,
            "FAIL" => AttemptsExceededAction.Fail,
            _ => AttemptsExceededAction.Delete // Default
        };
    }
}
