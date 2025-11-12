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
    /// Whether to force reseeding even if data already exists
    /// Set to false in development to skip seeding when data is already present
    /// </summary>
    public bool ForceReseed { get; set; } = false;

    /// <summary>
    /// Maximum retry count for database operations
    /// </summary>
    public int MaxRetryCount { get; set; } = 5;

    /// <summary>
    /// Maximum retry delay in seconds
    /// </summary>
    public int MaxRetryDelaySeconds { get; set; } = 5;
}