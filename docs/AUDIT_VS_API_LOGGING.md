# Audit Logging vs API Request/Response Logging

## Overview

This repository implements two complementary types of logging:

1. **Audit Logging** (Database Entity Tracking)
2. **API Request/Response Logging** (HTTP Traffic Tracking)

Both are independent systems serving different purposes and should coexist.

## 1. Audit Logging (Pre-existing)

### Purpose
Track **database changes** at the entity level - who created, updated, or deleted entities and when.

### Implementation
- **File**: `NTC.QoS.Infrastructure/AuditInterceptor.cs`
- **Mechanism**: Entity Framework Core interceptor
- **Scope**: Database operations only
- **What it tracks**:
  - `CreatedAt` - When entity was created
  - `CreatedBy` - User who created entity
  - `UpdatedAt` - When entity was last updated
  - `UpdatedBy` - User who last updated entity
  - `DeletedAt` - When entity was soft-deleted
  - `DeletedBy` - User who soft-deleted entity

### How it works
```csharp
public class AuditInterceptor : ISaveChangesInterceptor
{
    // Intercepts SaveChanges operations
    // Automatically sets CreatedBy, UpdatedBy, CreatedAt, UpdatedAt
    // On all entities inheriting from BaseEntity
}
```

### Registration
Registered in `DatabaseServiceExtensions.cs`:
```csharp
services.AddScoped<AuditInterceptor>();
optionsBuilder.AddInterceptors(audit);
```

### Use Cases
- "Who created this provider?"
- "When was this NAP last modified?"
- "Who deleted this cell tower?"
- Compliance and regulatory requirements
- Data lineage tracking

## 2. API Request/Response Logging (Newly Implemented)

### Purpose
Track **HTTP API traffic** - complete request/response details for debugging, monitoring, and traceability.

### Implementation
- **Files**: 
  - `NTC.QoS.API/Middleware/RequestResponseLoggingMiddleware.cs`
  - `NTC.QoS.Domain/Models/Logging/VerboseApiLoggingSettings.cs`
  - `NTC.QoS.Domain/Models/Logging/ApiRequestLog.cs`
  - `NTC.QoS.Domain/Models/Logging/ApiResponseLog.cs`
- **Mechanism**: ASP.NET Core middleware
- **Scope**: All HTTP requests/responses
- **What it tracks**:
  - Request method, path, query string
  - Request/response headers
  - Request/response bodies
  - Status codes
  - Elapsed time
  - Client IP, user agent
  - Correlation IDs for distributed tracing

### How it works
```csharp
public class RequestResponseLoggingMiddleware
{
    // Intercepts ALL HTTP requests early in pipeline
    // Captures request details
    // Buffers response for logging
    // Redacts sensitive data (passwords, tokens, etc.)
    // Writes to structured log files
}
```

### Registration
Registered in `Program.cs`:
```csharp
builder.Services.AddVerboseApiLogging(builder.Configuration);
app.UseVerboseApiLogging();
```

### Use Cases
- "What was the exact request that caused this error?"
- "How long did this API call take?"
- "What was the response for correlation ID xyz?"
- Performance monitoring
- Security incident investigation
- API usage analytics

## Key Differences

| Feature | Audit Logging | API Request/Response Logging |
|---------|---------------|------------------------------|
| **Scope** | Database entities only | All HTTP traffic |
| **Level** | Data model level | HTTP protocol level |
| **Storage** | Entity fields (in database) | Log files (Serilog) |
| **Granularity** | Entity changes | Complete request/response |
| **Performance** | Minimal (entity-level) | Low (middleware-level) |
| **Sensitive Data** | N/A | Automatically redacted |
| **Configuration** | Always on | Configurable on/off |
| **Use Case** | Compliance, data lineage | Debugging, monitoring |

## Both Systems Work Together

### Example Scenario: Creating a Provider

**Audit Logging captures:**
```
Entity: Provider
Id: abc-123
CreatedAt: 2025-12-15T10:30:45Z
CreatedBy: user_xyz
```

**API Request/Response Logging captures:**
```json
{
  "request": {
    "correlationId": "req-456",
    "method": "POST",
    "path": "/providers",
    "body": "{\"name\":\"New Provider\",\"apiKey\":\"[REDACTED]\"}",
    "userId": "user_xyz"
  },
  "response": {
    "correlationId": "req-456",
    "statusCode": 201,
    "body": "{\"succeeded\":true,\"data\":{\"id\":\"abc-123\"}}",
    "elapsedMilliseconds": 245
  }
}
```

### Combined Benefits
1. **Audit logging** tells you WHO changed WHAT and WHEN at the data level
2. **API logging** tells you HOW they did it (what request they sent, what response they got)
3. Together they provide complete traceability from HTTP request → business logic → database change

## Configuration

### Audit Logging
- Always enabled
- No configuration needed
- Automatically applies to all entities inheriting from `BaseEntity`

### API Request/Response Logging
Configured in `appsettings.json`:
```json
{
  "VerboseApiLogging": {
    "Enabled": true,
    "LogRequestBody": true,
    "LogResponseBody": true,
    "ExcludedPaths": ["/health", "/metrics"]
  }
}
```

## Security Considerations

### Audit Logging
- User IDs are stored as-is
- No sensitive data handling (relies on entity design)

### API Request/Response Logging
- Automatic redaction of:
  - Authorization headers
  - Passwords in request bodies
  - API keys and tokens
  - Credit card numbers, SSN
- Configurable redaction lists
- See `docs/VERBOSE_API_LOGGING.md` for details

## Documentation

- **Audit Logging**: No specific documentation (standard EF Core pattern)
- **API Logging**: 
  - `docs/VERBOSE_API_LOGGING.md` - Complete guide
  - `docs/API_LOGGING_QUICK_REFERENCE.md` - Quick reference
  - `docs/IMPLEMENTATION_SUMMARY.md` - Implementation details

## Conclusion

Both logging systems are **complementary and should remain enabled**:

- **Audit logging** provides data-level accountability
- **API logging** provides request-level visibility

Together they provide comprehensive observability for compliance, debugging, and monitoring.
