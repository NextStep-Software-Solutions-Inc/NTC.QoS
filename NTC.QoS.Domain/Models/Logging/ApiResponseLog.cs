namespace NTC.QoS.Domain.Models.Logging;

/// <summary>
/// Represents a logged API response with all relevant metadata
/// </summary>
public class ApiResponseLog
{
    /// <summary>
    /// Correlation ID matching the corresponding request
    /// </summary>
    public string CorrelationId { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when the response was sent (UTC)
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// HTTP status code (200, 404, 500, etc.)
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// Response headers (with sensitive data redacted)
    /// </summary>
    public Dictionary<string, string>? Headers { get; set; }

    /// <summary>
    /// Response body content (with sensitive data redacted)
    /// </summary>
    public string? Body { get; set; }

    /// <summary>
    /// Content type of the response
    /// </summary>
    public string? ContentType { get; set; }

    /// <summary>
    /// Size of the response body in bytes
    /// </summary>
    public long? BodySize { get; set; }

    /// <summary>
    /// Time taken to process the request in milliseconds
    /// </summary>
    public long ElapsedMilliseconds { get; set; }

    /// <summary>
    /// Indicates if an exception occurred during request processing
    /// </summary>
    public bool HasError { get; set; }

    /// <summary>
    /// Error message if an exception occurred
    /// </summary>
    public string? ErrorMessage { get; set; }
}
