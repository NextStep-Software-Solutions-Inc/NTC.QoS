namespace NTC.QoS.Domain.Models.Logging;

/// <summary>
/// Configuration settings for verbose API logging
/// </summary>
public class VerboseApiLoggingSettings
{
    /// <summary>
    /// Enable or disable verbose API logging. Default is true.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Enable or disable request body logging. Default is true.
    /// </summary>
    public bool LogRequestBody { get; set; } = true;

    /// <summary>
    /// Enable or disable response body logging. Default is true.
    /// </summary>
    public bool LogResponseBody { get; set; } = true;

    /// <summary>
    /// Enable or disable request headers logging. Default is true.
    /// </summary>
    public bool LogRequestHeaders { get; set; } = true;

    /// <summary>
    /// Enable or disable response headers logging. Default is true.
    /// </summary>
    public bool LogResponseHeaders { get; set; } = true;

    /// <summary>
    /// Maximum body size to log in bytes. Default is 10MB.
    /// Bodies larger than this will be truncated.
    /// </summary>
    public int MaxBodySizeToLog { get; set; } = 10 * 1024 * 1024; // 10MB

    /// <summary>
    /// List of header names to redact (case-insensitive)
    /// </summary>
    public List<string> HeadersToRedact { get; set; } = new()
    {
        "Authorization",
        "Cookie",
        "Set-Cookie",
        "X-API-Key",
        "X-Auth-Token"
    };

    /// <summary>
    /// List of query parameter names to redact (case-insensitive)
    /// </summary>
    public List<string> QueryParamsToRedact { get; set; } = new()
    {
        "password",
        "token",
        "apiKey",
        "api_key",
        "secret"
    };

    /// <summary>
    /// List of JSON property paths to redact in request/response bodies (case-insensitive)
    /// </summary>
    public List<string> BodyPropertiesToRedact { get; set; } = new()
    {
        "password",
        "confirmPassword",
        "currentPassword",
        "newPassword",
        "oldPassword",
        "apiKey",
        "api_key",
        "secretKey",
        "secret_key",
        "token",
        "accessToken",
        "access_token",
        "refreshToken",
        "refresh_token",
        "creditCard",
        "cardNumber",
        "cvv",
        "ssn"
    };

    /// <summary>
    /// String to use when redacting sensitive data. Default is "[REDACTED]".
    /// </summary>
    public string RedactionText { get; set; } = "[REDACTED]";

    /// <summary>
    /// List of paths to exclude from logging (e.g., health checks, metrics endpoints)
    /// </summary>
    public List<string> ExcludedPaths { get; set; } = new()
    {
        "/health",
        "/metrics",
        "/favicon.ico"
    };

    /// <summary>
    /// Log file path. If null or empty, logs to console only.
    /// </summary>
    public string? LogFilePath { get; set; } = "logs/api-requests-.txt";

    /// <summary>
    /// Number of days to retain log files. Default is 30 days.
    /// </summary>
    public int RetainedFileCountLimit { get; set; } = 30;

    /// <summary>
    /// Maximum log file size in bytes before rolling. Default is 100MB.
    /// </summary>
    public long FileSizeLimitBytes { get; set; } = 100 * 1024 * 1024; // 100MB
}
