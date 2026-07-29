using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NTC.QoS.Domain.Models.Logging;
using System.Diagnostics;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace NTC.QoS.API.Middleware;

/// <summary>
/// Middleware that logs all API requests and responses with comprehensive details
/// </summary>
public class RequestResponseLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestResponseLoggingMiddleware> _logger;
    private readonly VerboseApiLoggingSettings _settings;

    public RequestResponseLoggingMiddleware(
        RequestDelegate next,
        ILogger<RequestResponseLoggingMiddleware> logger,
        IOptions<VerboseApiLoggingSettings> settings)
    {
        _next = next;
        _logger = logger;
        _settings = settings.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip logging if disabled or path is excluded
        if (!_settings.Enabled || IsPathExcluded(context.Request.Path))
        {
            await _next(context);
            return;
        }

        // Generate or retrieve correlation ID
        var correlationId = GetOrCreateCorrelationId(context);
        context.Items["CorrelationId"] = correlationId;

        var stopwatch = Stopwatch.StartNew();

        // Log the request
        var requestLog = await LogRequestAsync(context, correlationId);

        // Prepare to capture the response
        var originalBodyStream = context.Response.Body;
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        ApiResponseLog? responseLog = null;
        try
        {
            // Continue processing the request
            await _next(context);

            // Log the response
            responseLog = await LogResponseAsync(context, correlationId, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            // Log the error response
            responseLog = await LogErrorResponseAsync(context, correlationId, stopwatch.ElapsedMilliseconds, ex);
            throw; // Re-throw to let the exception handler deal with it
        }
        finally
        {
            // Copy response body back to original stream
            responseBody.Seek(0, SeekOrigin.Begin);
            await responseBody.CopyToAsync(originalBodyStream);

            // Log the combined request/response
            LogRequestResponse(requestLog, responseLog);
        }
    }

    private string GetOrCreateCorrelationId(HttpContext context)
    {
        // Check if correlation ID exists in headers
        if (context.Request.Headers.TryGetValue("X-Correlation-ID", out var correlationId) &&
            !string.IsNullOrEmpty(correlationId))
        {
            return correlationId.ToString();
        }

        // Generate new correlation ID
        var newCorrelationId = Guid.NewGuid().ToString();
        context.Response.Headers["X-Correlation-ID"] = newCorrelationId;
        return newCorrelationId;
    }

    private bool IsPathExcluded(PathString path)
    {
        return _settings.ExcludedPaths.Any(excluded =>
            path.StartsWithSegments(excluded, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<ApiRequestLog> LogRequestAsync(HttpContext context, string correlationId)
    {
        var request = context.Request;

        // Enable buffering so we can read the body multiple times
        request.EnableBuffering();

        var requestLog = new ApiRequestLog
        {
            CorrelationId = correlationId,
            Timestamp = DateTime.UtcNow,
            Method = request.Method,
            Path = request.Path,
            QueryString = RedactQueryString(request.QueryString.ToString()),
            ContentType = request.ContentType,
            Scheme = request.Scheme,
            Host = request.Host.ToString(),
            ClientIp = GetClientIpAddress(context),
            UserAgent = request.Headers.UserAgent.ToString(),
            UserId = GetUserId(context)
        };

        // Log headers if enabled
        if (_settings.LogRequestHeaders)
        {
            requestLog.Headers = RedactHeaders(request.Headers);
        }

        // Log body if enabled and present
        if (_settings.LogRequestBody && request.ContentLength > 0)
        {
            try
            {
                request.Body.Position = 0;
                using var reader = new StreamReader(
                    request.Body,
                    encoding: Encoding.UTF8,
                    detectEncodingFromByteOrderMarks: false,
                    bufferSize: 1024,
                    leaveOpen: true);

                var body = await reader.ReadToEndAsync();
                request.Body.Position = 0;

                requestLog.BodySize = body.Length;

                if (body.Length <= _settings.MaxBodySizeToLog)
                {
                    requestLog.Body = RedactSensitiveData(body);
                }
                else
                {
                    requestLog.Body = $"[Body too large: {body.Length} bytes - truncated]";
                }
            }
            catch (NotSupportedException)
            {
                // Stream doesn't support seeking, skip body logging
                requestLog.Body = "[Body not available - stream does not support seeking]";
            }
        }

        return requestLog;
    }

    private async Task<ApiResponseLog> LogResponseAsync(HttpContext context, string correlationId, long elapsedMs)
    {
        var response = context.Response;

        var responseLog = new ApiResponseLog
        {
            CorrelationId = correlationId,
            Timestamp = DateTime.UtcNow,
            StatusCode = response.StatusCode,
            ContentType = response.ContentType,
            ElapsedMilliseconds = elapsedMs,
            HasError = false
        };

        // Log headers if enabled
        if (_settings.LogResponseHeaders)
        {
            responseLog.Headers = RedactHeaders(response.Headers);
        }

        // Log body if enabled
        if (_settings.LogResponseBody && response.Body.CanRead && response.Body.CanSeek)
        {
            try
            {
                response.Body.Seek(0, SeekOrigin.Begin);
                using var reader = new StreamReader(response.Body, Encoding.UTF8, leaveOpen: true);
                var body = await reader.ReadToEndAsync();
                response.Body.Seek(0, SeekOrigin.Begin);

                responseLog.BodySize = body.Length;

                if (body.Length <= _settings.MaxBodySizeToLog)
                {
                    responseLog.Body = RedactSensitiveData(body);
                }
                else
                {
                    responseLog.Body = $"[Body too large: {body.Length} bytes - truncated]";
                }
            }
            catch (NotSupportedException)
            {
                // Stream doesn't support seeking, skip body logging
                responseLog.Body = "[Body not available - stream does not support seeking]";
            }
        }
        else if (_settings.LogResponseBody && response.Body.CanRead && !response.Body.CanSeek)
        {
            responseLog.Body = "[Body not available - stream does not support seeking]";
        }

        return responseLog;
    }

    private async Task<ApiResponseLog> LogErrorResponseAsync(
        HttpContext context,
        string correlationId,
        long elapsedMs,
        Exception exception)
    {
        var responseLog = new ApiResponseLog
        {
            CorrelationId = correlationId,
            Timestamp = DateTime.UtcNow,
            StatusCode = context.Response.StatusCode,
            ContentType = context.Response.ContentType,
            ElapsedMilliseconds = elapsedMs,
            HasError = true,
            ErrorMessage = exception.Message
        };

        return await Task.FromResult(responseLog);
    }

    private void LogRequestResponse(ApiRequestLog requestLog, ApiResponseLog? responseLog)
    {
        var logEntry = new
        {
            Request = requestLog,
            Response = responseLog
        };

        var jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var logMessage = JsonSerializer.Serialize(logEntry, jsonOptions);

        // Use appropriate log level based on status code
        if (responseLog != null)
        {
            if (responseLog.HasError || responseLog.StatusCode >= 500)
            {
                _logger.LogError("API Request/Response: {LogEntry}", logMessage);
            }
            else if (responseLog.StatusCode >= 400)
            {
                _logger.LogWarning("API Request/Response: {LogEntry}", logMessage);
            }
            else
            {
                _logger.LogInformation("API Request/Response: {LogEntry}", logMessage);
            }
        }
        else
        {
            _logger.LogInformation("API Request: {LogEntry}", logMessage);
        }
    }

    private Dictionary<string, string> RedactHeaders(IHeaderDictionary headers)
    {
        var result = new Dictionary<string, string>();

        foreach (var header in headers)
        {
            var shouldRedact = _settings.HeadersToRedact
                .Any(h => h.Equals(header.Key, StringComparison.OrdinalIgnoreCase));

            result[header.Key] = shouldRedact ? _settings.RedactionText : header.Value.ToString();
        }

        return result;
    }

    private string RedactQueryString(string queryString)
    {
        if (string.IsNullOrEmpty(queryString))
            return string.Empty;

        foreach (var param in _settings.QueryParamsToRedact)
        {
            // Case-insensitive regex to redact query parameters
            var pattern = $@"({Regex.Escape(param)}=)[^&]*";
            queryString = Regex.Replace(
                queryString,
                pattern,
                $"$1{_settings.RedactionText}",
                RegexOptions.IgnoreCase);
        }

        return queryString;
    }

    private string RedactSensitiveData(string content)
    {
        if (string.IsNullOrEmpty(content))
            return content;

        try
        {
            // Try to parse as JSON and redact properties
            var jsonDoc = JsonDocument.Parse(content);
            var redacted = RedactJsonProperties(jsonDoc.RootElement);
            return JsonSerializer.Serialize(redacted, new JsonSerializerOptions { WriteIndented = false });
        }
        catch
        {
            // Not JSON, return as-is (could be form data, XML, etc.)
            return content;
        }
    }

    private object? RedactJsonProperties(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                var obj = new Dictionary<string, object?>();
                foreach (var property in element.EnumerateObject())
                {
                    var shouldRedact = _settings.BodyPropertiesToRedact
                        .Any(prop => prop.Equals(property.Name, StringComparison.OrdinalIgnoreCase));

                    obj[property.Name] = shouldRedact
                        ? _settings.RedactionText
                        : RedactJsonProperties(property.Value);
                }
                return obj;

            case JsonValueKind.Array:
                var arr = new List<object?>();
                foreach (var item in element.EnumerateArray())
                {
                    arr.Add(RedactJsonProperties(item));
                }
                return arr;

            case JsonValueKind.String:
                return element.GetString();

            case JsonValueKind.Number:
                // Preserve number precision by trying integer first, then decimal
                if (element.TryGetInt64(out var longValue))
                    return longValue;
                if (element.TryGetDecimal(out var decimalValue))
                    return decimalValue;
                return element.GetDouble();

            case JsonValueKind.True:
                return true;

            case JsonValueKind.False:
                return false;

            case JsonValueKind.Null:
                return null;

            default:
                return element.ToString();
        }
    }

    private string? GetClientIpAddress(HttpContext context)
    {
        // Check for forwarded IP first (for proxies/load balancers)
        if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
        {
            return forwardedFor.ToString().Split(',').FirstOrDefault()?.Trim();
        }

        return context.Connection.RemoteIpAddress?.ToString();
    }

    private string? GetUserId(HttpContext context)
    {
        // Try to get user ID from claims
        var userIdClaim = context.User?.FindFirst(ClaimTypes.NameIdentifier)
            ?? context.User?.FindFirst("sub")
            ?? context.User?.FindFirst("userId");

        return userIdClaim?.Value;
    }
}
