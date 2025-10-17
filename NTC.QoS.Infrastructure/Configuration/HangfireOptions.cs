namespace NTC.QoS.Infrastructure.Configuration;

/// <summary>
/// Configuration options for Hangfire background job processing
/// </summary>
public class HangfireOptions
{
    public const string SectionName = "Hangfire";

    /// <summary>
    /// Number of worker threads for processing jobs
    /// </summary>
    public int? WorkerCount { get; set; }

    /// <summary>
    /// Maximum number of workers allowed
    /// </summary>
    public int MaxWorkerCount { get; set; } = 32;

    /// <summary>
    /// Server name override. If null, will use application name + machine name
    /// </summary>
    public string? ServerName { get; set; }

    /// <summary>
    /// Enable dashboard metrics
    /// </summary>
    public bool EnableDashboardMetrics { get; set; } = true;

    /// <summary>
    /// Automatic retry configuration
    /// </summary>
    public AutoRetryOptions AutoRetry { get; set; } = new();
}

public class AutoRetryOptions
{
    /// <summary>
    /// Number of retry attempts
    /// </summary>
    public int Attempts { get; set; } = 3;

    /// <summary>
    /// Whether to log retry events
    /// </summary>
    public bool LogEvents { get; set; } = true;

    /// <summary>
    /// Action to take when max attempts are exceeded
    /// </summary>
    public string OnAttemptsExceeded { get; set; } = "Delete"; // Delete, Fail
}