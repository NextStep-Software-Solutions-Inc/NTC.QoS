namespace NTC.QoS.Infrastructure.Configuration;

/// <summary>
/// Configuration options for database operations
/// </summary>
public class DatabaseOptions
{
    public const string SectionName = "Database";

    /// <summary>
    /// Whether to run migrations automatically on startup
    /// </summary>
    public bool RunMigrationsOnStartup { get; set; } = true;

    /// <summary>
    /// Maximum retry count for database operations
    /// </summary>
    public int MaxRetryCount { get; set; } = 5;

    /// <summary>
    /// Maximum retry delay in seconds
    /// </summary>
    public int MaxRetryDelaySeconds { get; set; } = 5;

    /// <summary>
    /// Advisory lock key for migrations
    /// </summary>
    public long AdvisoryLockKey { get; set; } = 987654321012345678;
}