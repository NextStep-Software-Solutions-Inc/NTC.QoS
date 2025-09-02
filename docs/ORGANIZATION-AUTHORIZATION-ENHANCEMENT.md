# Organization Authorization Enhancement

## Overview

The organization authorization system has been enhanced with comprehensive organization name matching logic and flexible authorization options. This document describes the new capabilities and how to use them.

## Enhanced Features

### 1. Organization Name Matching
- **Direct Name Comparison**: Fast in-memory comparison using cached user data
- **Database Verification**: Robust verification against the database for accuracy
- **Case Sensitivity Control**: Configurable case-sensitive or case-insensitive matching
- **Organization ID Support**: Direct organization ID matching for performance

### 2. Flexible Authorization Requirements

The `OrganizationRequirement` class now supports multiple authorization scenarios:

```csharp
public class OrganizationRequirement
{
    public string? Organization { get; }           // Organization name to match
    public Guid? OrganizationId { get; }          // Organization ID to match
    public bool RequireSameOrganization { get; }  // User must belong to their org
    public bool CaseSensitive { get; }            // Case-sensitive name matching
}
```

### 3. Enhanced Authorization Handler

The `OrganizationAuthorizationHandler` now implements:
- **Multi-level Verification**: Cached data + database verification
- **Performance Optimization**: Fast path for cached data, fallback to database
- **Error Resilience**: Graceful handling of database lookup failures
- **Clear Logic Flow**: Organized authorization decision process

## Usage Examples

### 1. Basic Organization Authorization

```csharp
// Requires user to belong to any organization (their own)
[RequireOrganization]
public async Task<ActionResult> GetOrganizationData() { ... }

// Dynamic policy - same as above
[Authorize(Policy = "Organization:SameOrganization")]
public async Task<ActionResult> GetOrgData() { ... }
```

### 2. Specific Organization Name

```csharp
// Requires user to belong to "National" organization (case-insensitive)
[RequireOrganization("National")]
public async Task<ActionResult> GetNationalData() { ... }

// Case-sensitive organization name matching
[RequireOrganization("Regional", caseSensitive: true)]
public async Task<ActionResult> GetRegionalData() { ... }

// Dynamic policies
[Authorize(Policy = "Organization:National")]
[Authorize(Policy = "Organization:Regional:CaseSensitive")]
public async Task<ActionResult> GetSpecificOrgData() { ... }
```

### 3. Organization ID-Based Authorization

```csharp
// Requires user to belong to specific organization by ID
[RequireOrganizationId("33333333-3333-3333-3333-333333333333")]
public async Task<ActionResult> GetPublicData() { ... }

// Dynamic policy
[Authorize(Policy = "OrganizationId:33333333-3333-3333-3333-333333333333")]
public async Task<ActionResult> GetOrgByIdData() { ... }
```

### 4. Manual Authorization Checks

```csharp
public class MyController : ControllerBase
{
    private readonly IUserAuthorizationService _authorizationService;

    public async Task<ActionResult> MyAction()
    {
        // Check if user belongs to specific organization
        var belongsToNational = await _authorizationService.BelongsToOrganizationAsync(
            OrganizationSeeder.NationalOrganizationId);

        if (!belongsToNational)
            return Forbid("You don't belong to the National organization");

        // Get current user's organization
        var currentUser = await _authorizationService.GetCurrentUserAsync();
        var userOrgName = currentUser?.Organization?.Name;

        return Ok($"Welcome, {userOrgName} organization member!");
    }
}
```

## Authorization Decision Flow

The `OrganizationAuthorizationHandler` follows this decision process:

1. **User Validation**: Verify user has an organization
2. **Same Organization Check**: If `RequireSameOrganization` is true and no specific org is required
3. **Organization ID Check**: If `OrganizationId` is specified, compare directly
4. **Organization Name Check**: If `Organization` name is specified:
   - Compare with cached user organization data
   - Verify against database for accuracy
   - Respect case sensitivity setting
5. **Authorization Result**: Succeed if any condition is met, fail otherwise

## Performance Considerations

### Fast Path (Cached Data)
- Uses cached user organization data from `IUserAuthorizationService`
- No database queries for basic organization membership checks
- Ideal for high-frequency authorization decisions

### Verification Path (Database)
- Performs database lookup for name-to-ID mapping
- Ensures accuracy when cached data might be stale
- Graceful fallback if database query fails

### Best Practices

1. **Use Organization IDs when possible** for better performance
2. **Cache organization data** in user context to minimize database calls
3. **Handle case sensitivity appropriately** based on your organization naming conventions
4. **Test both authorization paths** to ensure reliability

## Database Integration

The system integrates with the existing organization structure:

```csharp
// Organization entity
public class Organization : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public virtual List<User> Users { get; set; } = new();
}

// Seeded organizations
- "National" (ID: 11111111-1111-1111-1111-111111111111)
- "Regional" (ID: 22222222-2222-2222-2222-222222222222)  
- "Public" (ID: 33333333-3333-3333-3333-333333333333)
```

## Security Considerations

1. **Organization Data Integrity**: Database verification ensures cached data accuracy
2. **Case Sensitivity**: Choose appropriate setting based on organization naming policies
3. **Access Control**: Combine with permission-based authorization for granular control
4. **Audit Trail**: Authorization decisions are logged for security monitoring

## Error Handling

The system handles various error scenarios:

- **Missing User Organization**: Authorization fails gracefully
- **Database Connection Issues**: Falls back to cached data comparison
- **Invalid Organization Names/IDs**: Authorization fails without exceptions
- **Case Mismatch**: Configurable handling based on case sensitivity setting

## Testing Examples

```csharp
[Test]
public async Task OrganizationAuthorization_ShouldSucceed_WhenUserBelongsToNationalOrg()
{
    // Arrange
    var user = CreateUserWithOrganization("National");
    var requirement = new OrganizationRequirement("National");
    
    // Act
    var result = await AuthorizeAsync(user, requirement);
    
    // Assert
    Assert.That(result.Succeeded, Is.True);
}

[Test]
public async Task OrganizationAuthorization_ShouldRespectCaseSensitivity()
{
    // Arrange
    var user = CreateUserWithOrganization("Regional");
    var requirement = new OrganizationRequirement("REGIONAL", caseSensitive: true);
    
    // Act
    var result = await AuthorizeAsync(user, requirement);
    
    // Assert
    Assert.That(result.Succeeded, Is.False);
}
```

## Migration Notes

- **Backward Compatibility**: Existing organization authorization continues to work
- **No Breaking Changes**: All existing APIs and policies remain functional
- **Enhanced Capabilities**: New features are additive and optional
- **Performance**: No performance degradation for existing authorization patterns

This enhanced organization authorization system provides robust, flexible, and performant access control while maintaining full backward compatibility with existing implementations.