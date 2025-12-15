# Verbose API Activity Logging

## Overview

This document describes the comprehensive API request/response logging system implemented in the NTC.QoS application. The system provides 100% verbose logging of all API activity, including requests, responses, headers, bodies, and metadata for complete traceability and debugging capabilities.

## Features

- **Complete Request/Response Logging**: Captures all HTTP requests and responses passing through the API
- **Correlation IDs**: Automatic generation and tracking of correlation IDs for request tracing
- **Sensitive Data Redaction**: Automatic masking of sensitive information (passwords, tokens, API keys, etc.)
- **Configurable Verbosity**: Toggle logging components (headers, bodies) via configuration
- **File-Based Persistence**: Logs written to rolling files with configurable retention
- **Structured Logging**: JSON-formatted logs for easy parsing and analysis
- **Performance Tracking**: Automatic elapsed time tracking for each request
- **Flexible Exclusions**: Configure paths to exclude from logging (e.g., health checks)

## Architecture

### Components

1. **VerboseApiLoggingSettings**: Configuration model for logging behavior
2. **ApiRequestLog**: Data model representing logged request information
3. **ApiResponseLog**: Data model representing logged response information
4. **RequestResponseLoggingMiddleware**: ASP.NET Core middleware that intercepts and logs all HTTP traffic
5. **Serilog Integration**: File-based logging with rolling intervals and retention policies

### Middleware Pipeline Position

The logging middleware is positioned early in the request pipeline (after security headers) to ensure all requests are captured before other middleware processes them.

## Configuration

### appsettings.json

Add the following configuration section to your `appsettings.json`:

```json
{
  "VerboseApiLogging": {
    "Enabled": true,
    "LogRequestBody": true,
    "LogResponseBody": true,
    "LogRequestHeaders": true,
    "LogResponseHeaders": true,
    "MaxBodySizeToLog": 10485760,
    "HeadersToRedact": [
      "Authorization",
      "Cookie",
      "Set-Cookie",
      "X-API-Key",
      "X-Auth-Token"
    ],
    "QueryParamsToRedact": [
      "password",
      "token",
      "apiKey",
      "api_key",
      "secret"
    ],
    "BodyPropertiesToRedact": [
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
    ],
    "RedactionText": "[REDACTED]",
    "ExcludedPaths": [
      "/health",
      "/metrics",
      "/favicon.ico"
    ],
    "LogFilePath": "logs/api-requests-.txt",
    "RetainedFileCountLimit": 30,
    "FileSizeLimitBytes": 104857600
  }
}
```

### Configuration Options

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Enabled` | bool | `true` | Master switch to enable/disable verbose logging |
| `LogRequestBody` | bool | `true` | Enable logging of request bodies |
| `LogResponseBody` | bool | `true` | Enable logging of response bodies |
| `LogRequestHeaders` | bool | `true` | Enable logging of request headers |
| `LogResponseHeaders` | bool | `true` | Enable logging of response headers |
| `MaxBodySizeToLog` | int | `10485760` (10MB) | Maximum body size to log before truncation |
| `HeadersToRedact` | string[] | See config | List of header names to redact (case-insensitive) |
| `QueryParamsToRedact` | string[] | See config | List of query parameter names to redact (case-insensitive) |
| `BodyPropertiesToRedact` | string[] | See config | List of JSON property names to redact in bodies (case-insensitive) |
| `RedactionText` | string | `"[REDACTED]"` | Text to display for redacted values |
| `ExcludedPaths` | string[] | `["/health", "/metrics", "/favicon.ico"]` | Paths to exclude from logging |
| `LogFilePath` | string | `"logs/api-requests-.txt"` | Path pattern for log files (date will be inserted) |
| `RetainedFileCountLimit` | int | `30` | Number of days to retain log files |
| `FileSizeLimitBytes` | long | `104857600` (100MB) | Maximum file size before rolling |

## How to Use

### Enable/Disable Logging

To **disable** verbose logging, set `Enabled` to `false` in your configuration:

```json
{
  "VerboseApiLogging": {
    "Enabled": false
  }
}
```

To **enable** it again, set `Enabled` to `true`.

### Environment-Specific Configuration

Override settings in environment-specific configuration files:

**appsettings.Development.json**:
```json
{
  "VerboseApiLogging": {
    "Enabled": true,
    "LogRequestBody": true,
    "LogResponseBody": true
  }
}
```

**appsettings.Production.json**:
```json
{
  "VerboseApiLogging": {
    "Enabled": false,
    "LogRequestBody": false,
    "LogResponseBody": false
  }
}
```

### Using Environment Variables

You can override any configuration setting using environment variables:

```bash
export VerboseApiLogging__Enabled=true
export VerboseApiLogging__LogRequestBody=false
export VerboseApiLogging__MaxBodySizeToLog=5242880
```

## Log Format

### Example Log Entry

```json
{
  "request": {
    "correlationId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "timestamp": "2025-12-15T10:30:45.123Z",
    "method": "POST",
    "path": "/providers",
    "queryString": "",
    "headers": {
      "Content-Type": "application/json",
      "Authorization": "[REDACTED]",
      "User-Agent": "Mozilla/5.0 ...",
      "Accept": "application/json"
    },
    "body": "{\"name\":\"Test Provider\",\"apiKey\":\"[REDACTED]\"}",
    "contentType": "application/json",
    "bodySize": 45,
    "clientIp": "192.168.1.100",
    "userAgent": "Mozilla/5.0 ...",
    "userId": "user_2X9Y1ZkLm3n4O5pQ",
    "scheme": "https",
    "host": "api.example.com"
  },
  "response": {
    "correlationId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "timestamp": "2025-12-15T10:30:45.456Z",
    "statusCode": 201,
    "headers": {
      "Content-Type": "application/json",
      "X-Correlation-ID": "a1b2c3d4-e5f6-7890-abcd-ef1234567890"
    },
    "body": "{\"succeeded\":true,\"data\":{\"id\":\"...\"},\"message\":\"Provider created successfully\"}",
    "contentType": "application/json",
    "bodySize": 89,
    "elapsedMilliseconds": 333,
    "hasError": false,
    "errorMessage": null
  }
}
```

## Security and Sensitive Data Redaction

The logging system automatically redacts sensitive information to protect user privacy and security:

### What is Redacted

1. **Headers**: Authorization, Cookie, Set-Cookie, API keys, auth tokens
2. **Query Parameters**: password, token, apiKey, secret
3. **JSON Body Properties**: passwords, API keys, tokens, credit card numbers, SSN

### Default Redaction Lists

**Headers**:
- Authorization
- Cookie
- Set-Cookie
- X-API-Key
- X-Auth-Token

**Query Parameters**:
- password
- token
- apiKey
- api_key
- secret

**Body Properties**:
- password, confirmPassword, currentPassword, newPassword, oldPassword
- apiKey, api_key, secretKey, secret_key
- token, accessToken, access_token, refreshToken, refresh_token
- creditCard, cardNumber, cvv, ssn

### Customizing Redaction

Add or remove items from redaction lists in your configuration:

```json
{
  "VerboseApiLogging": {
    "HeadersToRedact": [
      "Authorization",
      "Cookie",
      "X-Custom-Secret-Header"
    ],
    "BodyPropertiesToRedact": [
      "password",
      "apiKey",
      "customSensitiveField"
    ]
  }
}
```

## Log Files

### Location

Logs are written to the `logs/` directory in the application root by default:
- `logs/api-requests-20251215.txt`
- `logs/api-requests-20251216.txt`
- etc.

### Retention

- By default, log files are retained for 30 days
- Older files are automatically deleted
- Each file has a maximum size of 100MB before rolling to a new file

### File Format

Logs are written in a structured format with the following template:
```
2025-12-15 10:30:45.123 +00:00 [INF] API Request/Response: {...json...}
```

## Correlation IDs

### Automatic Generation

If a request doesn't include an `X-Correlation-ID` header, the middleware automatically generates a new GUID and adds it to the response headers.

### Client-Provided IDs

Clients can provide their own correlation IDs by including the `X-Correlation-ID` header in requests:

```http
GET /api/providers HTTP/1.1
X-Correlation-ID: my-custom-trace-id-12345
```

The same ID will be used for logging and returned in the response headers.

### Tracing Requests

Use correlation IDs to trace requests through:
1. Log files
2. Distributed systems
3. External monitoring tools
4. Error tracking systems

## Performance Considerations

### Impact

The logging middleware adds minimal overhead to request processing:
- Typical overhead: 1-5ms per request
- Memory: Request/response bodies are buffered for logging
- Disk I/O: Asynchronous file writes minimize blocking

### Optimization Tips

1. **Disable in Production**: Set `Enabled: false` in production if not needed
2. **Reduce Body Logging**: Set `LogRequestBody` and `LogResponseBody` to `false` for large payloads
3. **Exclude High-Traffic Paths**: Add frequently-called endpoints to `ExcludedPaths`
4. **Adjust Max Body Size**: Lower `MaxBodySizeToLog` to reduce memory usage

## Troubleshooting

### Logs Not Appearing

1. Check that `Enabled` is set to `true`
2. Verify the log file path exists and is writable
3. Check the `ExcludedPaths` configuration
4. Ensure the log level includes Information level

### Sensitive Data Not Redacted

1. Verify the property name is in `BodyPropertiesToRedact` (case-insensitive)
2. Ensure the body content is valid JSON
3. Check that the header/query param is in the appropriate redaction list

### Performance Issues

1. Disable body logging for large payloads
2. Increase `MaxBodySizeToLog` threshold to truncate large bodies
3. Add high-traffic endpoints to `ExcludedPaths`
4. Consider disabling logging in production environments

## Monitoring and Analysis

### Querying Logs

Use standard text processing tools to analyze logs:

```bash
# Find all errors (status code >= 500)
grep '"statusCode": 5' logs/api-requests-*.txt

# Find slow requests (> 1 second)
grep -E '"elapsedMilliseconds": [0-9]{4,}' logs/api-requests-*.txt

# Find requests from specific IP
grep '"clientIp": "192.168.1.100"' logs/api-requests-*.txt

# Find requests by correlation ID
grep 'a1b2c3d4-e5f6-7890-abcd-ef1234567890' logs/api-requests-*.txt
```

### Log Aggregation

Consider integrating with log aggregation tools:
- **Elasticsearch + Kibana**: Parse JSON logs and visualize
- **Splunk**: Index and search logs
- **Azure Application Insights**: Send logs to cloud monitoring
- **Datadog**: Aggregate and analyze logs

## Development and Testing

### Local Development

Verbose logging is enabled by default in development. View logs in:
1. Console output
2. `logs/api-requests-*.txt` files

### Testing Redaction

Create a test request with sensitive data:

```bash
curl -X POST http://localhost:5000/api/test \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer my-secret-token" \
  -d '{"username":"test","password":"secret123"}'
```

Verify in logs that:
- Authorization header shows `[REDACTED]`
- password field shows `[REDACTED]`

## Best Practices

1. **Enable in Development**: Always enable verbose logging during development
2. **Disable in Production**: Consider disabling or limiting logging in production
3. **Monitor Log Size**: Set up alerts for log file sizes
4. **Rotate Regularly**: Ensure log rotation is working properly
5. **Protect Sensitive Data**: Regularly review and update redaction lists
6. **Use Correlation IDs**: Include correlation IDs in error messages and monitoring
7. **Regular Cleanup**: Archive or delete old logs based on retention policies

## Support

For issues or questions about verbose API logging:
1. Check this documentation
2. Review the configuration in `appsettings.json`
3. Check the log files for error messages
4. Contact the development team

## Version History

- **v1.0.0** (2025-12-15): Initial implementation
  - Complete request/response logging
  - Sensitive data redaction
  - File-based persistence with rotation
  - Correlation ID support
  - Configurable verbosity levels
