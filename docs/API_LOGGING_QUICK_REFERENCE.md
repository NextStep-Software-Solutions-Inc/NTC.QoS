# API Logging Quick Reference

## Quick Start

### Enable/Disable Logging

**Via appsettings.json:**
```json
{
  "VerboseApiLogging": {
    "Enabled": true
  }
}
```

**Via Environment Variable:**
```bash
export VerboseApiLogging__Enabled=true
```

### Common Configurations

#### Development (Full Logging)
```json
{
  "VerboseApiLogging": {
    "Enabled": true,
    "LogRequestBody": true,
    "LogResponseBody": true,
    "LogRequestHeaders": true,
    "LogResponseHeaders": true
  }
}
```

#### Production (Minimal Logging)
```json
{
  "VerboseApiLogging": {
    "Enabled": false,
    "LogRequestBody": false,
    "LogResponseBody": false
  }
}
```

#### Debug Mode (Everything Logged)
```json
{
  "VerboseApiLogging": {
    "Enabled": true,
    "LogRequestBody": true,
    "LogResponseBody": true,
    "LogRequestHeaders": true,
    "LogResponseHeaders": true,
    "ExcludedPaths": []
  }
}
```

## Log Files

**Location:** `logs/api-requests-YYYYMMDD.txt`

**Format:** Structured JSON with timestamp

**Retention:** 30 days by default

## Viewing Logs

### Tail Live Logs
```bash
tail -f logs/api-requests-20251215.txt
```

### Find Errors
```bash
grep '"statusCode": 5' logs/api-requests-*.txt
```

### Find by Correlation ID
```bash
grep 'YOUR-CORRELATION-ID' logs/api-requests-*.txt
```

### Find Slow Requests (>1s)
```bash
grep -E '"elapsedMilliseconds": [0-9]{4,}' logs/api-requests-*.txt
```

## Correlation IDs

### Client Sends
```http
GET /api/endpoint HTTP/1.1
X-Correlation-ID: my-trace-id-123
```

### Server Returns
```http
HTTP/1.1 200 OK
X-Correlation-ID: my-trace-id-123
```

Use the same ID to trace requests through logs!

## Adding Custom Redaction

### Redact Custom Header
```json
{
  "VerboseApiLogging": {
    "HeadersToRedact": [
      "Authorization",
      "X-My-Custom-Secret"
    ]
  }
}
```

### Redact Custom Body Field
```json
{
  "VerboseApiLogging": {
    "BodyPropertiesToRedact": [
      "password",
      "myCustomSensitiveField"
    ]
  }
}
```

## Excluding Paths from Logging

```json
{
  "VerboseApiLogging": {
    "ExcludedPaths": [
      "/health",
      "/metrics",
      "/swagger",
      "/hangfire"
    ]
  }
}
```

## Troubleshooting

### No Logs Appearing
- ✅ Check `Enabled: true`
- ✅ Check path not in `ExcludedPaths`
- ✅ Check `logs/` directory exists and is writable

### Too Many Logs
- ✅ Set `Enabled: false`
- ✅ Set `LogRequestBody: false` and `LogResponseBody: false`
- ✅ Add paths to `ExcludedPaths`

### Sensitive Data in Logs
- ✅ Add field to `BodyPropertiesToRedact`
- ✅ Add header to `HeadersToRedact`
- ✅ Add param to `QueryParamsToRedact`

## Performance Tips

1. Disable body logging for large payloads
2. Exclude high-traffic health check endpoints
3. Reduce `MaxBodySizeToLog` to minimize memory usage
4. Disable in production if not needed

## See Also

- Full documentation: [VERBOSE_API_LOGGING.md](./VERBOSE_API_LOGGING.md)
- Configuration schema: `VerboseApiLoggingSettings.cs`
- Middleware: `RequestResponseLoggingMiddleware.cs`
