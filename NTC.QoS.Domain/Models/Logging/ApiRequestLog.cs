namespace NTC.QoS.Domain.Models.Logging;

/// <summary>
/// Represents a logged API request with all relevant metadata
/// </summary>
public class ApiRequestLog
{
    /// <summary>
    /// Unique correlation ID for tracing this request through the system
    /// </summary>
    public string CorrelationId { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when the request was received (UTC)
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// HTTP method (GET, POST, PUT, DELETE, etc.)
    /// </summary>
    public string Method { get; set; } = string.Empty;

    /// <summary>
    /// Request path (e.g., /api/providers)
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Query string parameters
    /// </summary>
    public string? QueryString { get; set; }

    /// <summary>
    /// Request headers (with sensitive data redacted)
    /// </summary>
    public Dictionary<string, string>? Headers { get; set; }

    /// <summary>
    /// Request body content (with sensitive data redacted)
    /// </summary>
    public string? Body { get; set; }

    /// <summary>
    /// Content type of the request
    /// </summary>
    public string? ContentType { get; set; }

    /// <summary>
    /// Size of the request body in bytes
    /// </summary>
    public long? BodySize { get; set; }

    /// <summary>
    /// Client IP address
    /// </summary>
    public string? ClientIp { get; set; }

    /// <summary>
    /// User agent string
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Authenticated user identifier (if available)
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Request scheme (http or https)
    /// </summary>
    public string? Scheme { get; set; }

    /// <summary>
    /// Host header value
    /// </summary>
    public string? Host { get; set; }
}
