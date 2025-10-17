using Hangfire;
using Microsoft.Extensions.Logging;

namespace NTC.QoS.Infrastructure.Services;

/// <summary>
/// Interface for registering Hangfire global filters
/// </summary>
public interface IHangfireFilterRegistrar
{
    /// <summary>
    /// Registers global Hangfire filters
    /// </summary>
    void RegisterGlobalFilters();
}

/// <summary>
/// Service for registering Hangfire global filters
/// </summary>
internal class HangfireFilterRegistrar : IHangfireFilterRegistrar
{
    private readonly ILogger<HangfireFilterRegistrar> _logger;
    private static bool _filtersRegistered = false;
    private static readonly object _lock = new();

    public HangfireFilterRegistrar(ILogger<HangfireFilterRegistrar> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Registers global Hangfire filters in a thread-safe manner
    /// </summary>
    public void RegisterGlobalFilters()
    {
        lock (_lock)
        {
            if (_filtersRegistered)
            {
                _logger.LogDebug("Hangfire global filters are already registered, skipping");
                return;
            }

            try
            {
                // Register global filters here if needed
                // Example: GlobalJobFilters.Filters.Add(new AutomaticRetryAttribute { Attempts = 3 });
                
                _logger.LogInformation("Hangfire global filters registered successfully");
                _filtersRegistered = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to register Hangfire global filters");
                throw;
            }
        }
    }
}
