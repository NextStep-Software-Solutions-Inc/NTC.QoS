# Dynamic Authorization System

This document describes the dynamic authorization system implemented for the NTC QoS application, which provides flexible permission, role, organization, and region-based access control.

## Overview

The authorization system is built on top of ASP.NET Core's authorization framework and provides:

- **Permission-based authorization**: Fine-grained access control using permission keys
- **Role-based authorization**: Access control based on user roles
- **Organization-based authorization**: Access control based on user's organization
- **Region-based authorization**: Access control based on user's accessible regions
- **Dynamic policy creation**: Policies can be created dynamically at runtime
- **Predefined policies**: Common authorization scenarios are pre-configured

## Architecture

### Core Components

1. **Authorization Requirements** (`NTC.QoS.Domain.Authorization`)
   - `PermissionRequirement`: For permission-based checks
   - `RoleRequirement`: For role-based checks
   - `OrganizationRequirement`: For organization-based checks
   - `RegionRequirement`: For region-based checks

2. **Authorization Handlers** (`NTC.QoS.Infrastructure.Authorization`)
   - `PermissionAuthorizationHandler`: Handles permission requirements
   - `RoleAuthorizationHandler`: Handles role requirements
   - `OrganizationAuthorizationHandler`: Handles organization requirements
   - `RegionAuthorizationHandler`: Handles region requirements

3. **Authorization Service** (`NTC.QoS.Infrastructure.Services`)
   - `IUserAuthorizationService`: Interface for user authorization operations
   - `UserAuthorizationService`: Implementation providing user permission/role/organization/region checks

4. **Dynamic Policy Provider** (`NTC.QoS.Infrastructure.Authorization`)
   - `DynamicAuthorizationPolicyProvider`: Creates policies dynamically based on naming conventions

5. **Authorization Attributes** (`NTC.QoS.API.Authorization`)
   - `RequirePermissionAttribute`: Attribute for permission-based authorization
   - `RequireRoleAttribute`: Attribute for role-based authorization
   - `RequireOrganizationAttribute`: Attribute for organization-based authorization
   - `RequireRegionAccessAttribute`: Attribute for region-based authorization

## Configuration

### 1. Service Registration

Add the dynamic authorization services in your `InfrastructureDependencyInjection.cs`:

```csharp
services.AddDynamicAuthorization();
```

### 2. Database Setup

Ensure your database has the following tables with appropriate relationships:
- `Users`: User records with `AuthUserId`, `RoleId`, `OrganizationId`
- `Roles`: Role definitions with `Key` field
- `Permissions`: Permission definitions with `Key` field
- `RolePermissions`: Many-to-many relationship between roles and permissions
- `UserRegions`: Many-to-many relationship between users and regions
- `Organizations`: Organization definitions
- `Regions`: Region definitions

## Usage Examples

### 1. Using Authorization Attributes

#### Permission-based Authorization
```csharp
[RequirePermission(Permissions.ViewFiles)]
public async Task<ActionResult> GetFiles()
{
    // Only users with "files.view" permission can access this
    return Ok("Files data");
}
```

#### Role-based Authorization
```csharp
[RequireRole(Roles.Admin)]
public async Task<ActionResult> GetAdminData()
{
    // Only users with "admin" role can access this
    return Ok("Admin data");
}
```

#### Organization-based Authorization
```csharp
[RequireOrganization]
public async Task<ActionResult> GetOrganizationData()
{
    // Only users within the same organization can access this
    return Ok("Organization data");
}
```

### 2. Using Predefined Policies

```csharp
[Authorize(Policy = Policies.CanViewFiles)]
public async Task<ActionResult> ViewFiles() { ... }

[Authorize(Policy = Policies.AdminOnly)]
public async Task<ActionResult> AdminFunction() { ... }

[Authorize(Policy = Policies.CanManageUsers)]
public async Task<ActionResult> ManageUsers() { ... }
```

### 3. Using Dynamic Policies

```csharp
// Permission-based dynamic policy
[Authorize(Policy = "Permission:files.view")]
public async Task<ActionResult> ViewFiles() { ... }

// Role-based dynamic policy
[Authorize(Policy = "Role:admin")]
public async Task<ActionResult> AdminFunction() { ... }

// Organization-based dynamic policy
[Authorize(Policy = "Organization:SpecificOrgName")]
public async Task<ActionResult> OrgFunction() { ... }

// Region-based dynamic policy (multiple regions)
[Authorize(Policy = "Region:guid1,guid2,guid3")]
public async Task<ActionResult> RegionFunction() { ... }
```

### 4. Manual Authorization Checks

```csharp
public class MyController : ControllerBase
{
    private readonly IUserAuthorizationService _authorizationService;

    public MyController(IUserAuthorizationService authorizationService)
    {
        _authorizationService = authorizationService;
    }

    public async Task<ActionResult> MyAction()
    {
        // Check single permission
        var hasPermission = await _authorizationService.HasPermissionAsync(Permissions.ViewFiles);
        if (!hasPermission)
            return Forbid();

        // Check multiple permissions (all required)
        var permissions = new[] { Permissions.ViewFiles, Permissions.UploadFiles };
        var hasAllPermissions = await _authorizationService.HasAllPermissionsAsync(permissions);

        // Check multiple permissions (any required)
        var hasAnyPermission = await _authorizationService.HasAnyPermissionAsync(permissions);

        // Check role
        var isAdmin = await _authorizationService.HasRoleAsync(Roles.Admin);

        // Check organization membership
        var belongsToOrg = await _authorizationService.BelongsToOrganizationAsync(organizationId);

        // Check region access
        var hasRegionAccess = await _authorizationService.HasRegionAccessAsync(regionId);

        // Get user info
        var currentUser = await _authorizationService.GetCurrentUserAsync();
        var userPermissions = await _authorizationService.GetUserPermissionsAsync();
        var accessibleRegions = await _authorizationService.GetAccessibleRegionsAsync();

        return Ok("Authorized");
    }
}
```

## Permission Keys

The system uses the following permission key format: `{resource}.{action}`

### File Management
- `files.view`: View files
- `files.upload`: Upload files
- `files.delete`: Delete files
- `files.process`: Process files

### User Management
- `users.view`: View users
- `users.create`: Create users
- `users.update`: Update users
- `users.delete`: Delete users

### Role Management
- `roles.view`: View roles
- `roles.create`: Create roles
- `roles.update`: Update roles
- `roles.delete`: Delete roles

### Network Infrastructure
- `cells.view`: View cells
- `cells.create`: Create cells
- `cells.update`: Update cells
- `cells.delete`: Delete cells

(See `NTC.QoS.Domain.Authorization.Permissions` for complete list)

## Role Keys

- `super-admin`: Super administrator
- `admin`: Administrator
- `manager`: Manager
- `user`: Regular user
- `viewer`: Read-only user
- `regional-manager`: Regional manager
- `technical-staff`: Technical staff

## Predefined Policies

### Permission-based Policies
- `CanViewFiles`: Requires `files.view` permission
- `CanUploadFiles`: Requires `files.upload` permission
- `CanDeleteFiles`: Requires `files.delete` permission
- `CanManageUsers`: Requires user management permissions
- `CanManageRoles`: Requires role management permissions
- `CanViewReports`: Requires `reports.view` permission

### Role-based Policies
- `AdminOnly`: Requires `admin` role
- `ManagerOnly`: Requires `manager` role
- `UserOnly`: Requires `user` role
- `AdminOrManager`: Requires either `admin` or `manager` role

### Organization-based Policies
- `SameOrganization`: Requires user to belong to same organization

### Region-based Policies
- `RegionAccess`: Requires access to any region

### Combined Policies
- `AdminOrSameOrganization`: Admin users or users from same organization
- `ManagerWithRegionAccess`: Manager role with region access

## Best Practices

1. **Use Permission-based Authorization**: Prefer permission-based over role-based for fine-grained control
2. **Combine Requirements**: Use multiple requirements for complex authorization scenarios
3. **Cache User Data**: The authorization service caches user data within a request scope
4. **Handle Authorization Failures**: Always handle `Forbid()` responses appropriately
5. **Test Authorization**: Write unit tests for authorization requirements and handlers
6. **Document Permissions**: Keep permission documentation up to date
7. **Audit Authorization**: Log authorization decisions for security auditing

## Troubleshooting

### Common Issues

1. **User Not Found**: Ensure the JWT token contains the correct `sub` claim matching `AuthUserId`
2. **No Permissions**: Verify user has the correct role and role has the required permissions
3. **Region Access**: Ensure user has `UserRegion` records for required regions
4. **Organization Access**: Verify user belongs to the correct organization

### Debugging

1. Enable logging for authorization decisions
2. Use the `/authorization-example/manual-check` endpoint to inspect user permissions
3. Check database relationships between User, Role, Permission, and RolePermission tables

## Security Considerations

1. **Principle of Least Privilege**: Grant minimum required permissions
2. **Regular Audits**: Regularly review user permissions and roles
3. **Secure Defaults**: Deny access by default, explicitly grant permissions
4. **Input Validation**: Validate all authorization-related inputs
5. **Session Management**: Ensure authorization data is refreshed appropriately