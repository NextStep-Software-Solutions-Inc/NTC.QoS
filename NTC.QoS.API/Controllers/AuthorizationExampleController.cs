using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.API.Authorization;
using NTC.QoS.Domain.Authorization;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.API.Controllers;

/// <summary>
/// Example controller demonstrating dynamic authorization usage
/// </summary>
[ApiController]
[Route("authorization-example", Name = "authorization-example")]
[Authorize]
public class AuthorizationExampleController : ControllerBase
{
    private readonly IUserAuthorizationService _authorizationService;
    private readonly IMediator _mediator;

    public AuthorizationExampleController(
        IUserAuthorizationService authorizationService,
        IMediator mediator)
    {
        _authorizationService = authorizationService;
        _mediator = mediator;
    }

    /// <summary>
    /// Example using permission-based authorization attribute
    /// </summary>
    [HttpGet("files")]
    [RequirePermission(Permissions.ViewFiles)]
    public async Task<ActionResult<ApiResponse<string>>> GetFiles()
    {
        // Simulate async operation to avoid CS1998 warning
        await Task.CompletedTask;
        return Ok(new ApiResponse<string>
        {
            Data = "Files data - accessible with ViewFiles permission",
            Succeeded = true,
            Message = "Files retrieved successfully"
        });
    }

    /// <summary>
    /// Example using role-based authorization attribute
    /// </summary>
    [HttpGet("admin-only")]
    [RequireRole(Roles.Admin)]
    public async Task<ActionResult<ApiResponse<string>>> GetAdminData()
    {
        // Simulate async operation to avoid CS1998 warning
        await Task.CompletedTask;
        return Ok(new ApiResponse<string>
        {
            Data = "Admin-only data",
            Succeeded = true,
            Message = "Admin data retrieved successfully"
        });
    }

    /// <summary>
    /// Example using organization-based authorization attribute
    /// </summary>
    [HttpGet("organization-data")]
    [RequireOrganization]
    public async Task<ActionResult<ApiResponse<string>>> GetOrganizationData()
    {
        // Simulate async operation to avoid CS1998 warning
        await Task.CompletedTask;
        return Ok(new ApiResponse<string>
        {
            Data = "Organization-specific data",
            Succeeded = true,
            Message = "Organization data retrieved successfully"
        });
    }

    /// <summary>
    /// Example using specific organization name authorization
    /// </summary>
    [HttpGet("national-data")]
    [RequireOrganization("National")]
    public async Task<ActionResult<ApiResponse<string>>> GetNationalData()
    {
        // Simulate async operation to avoid CS1998 warning
        await Task.CompletedTask;
        return Ok(new ApiResponse<string>
        {
            Data = "National organization data",
            Succeeded = true,
            Message = "National data retrieved successfully"
        });
    }

    /// <summary>
    /// Example using case-sensitive organization name authorization
    /// </summary>
    [HttpGet("regional-data")]
    [RequireOrganization("Regional", caseSensitive: true)]
    public async Task<ActionResult<ApiResponse<string>>> GetRegionalData()
    {
        // Simulate async operation to avoid CS1998 warning
        await Task.CompletedTask;
        return Ok(new ApiResponse<string>
        {
            Data = "Regional organization data (case-sensitive match)",
            Succeeded = true,
            Message = "Regional data retrieved successfully"
        });
    }

    /// <summary>
    /// Example using organization ID authorization
    /// </summary>
    [HttpGet("public-data")]
    [RequireOrganizationId("33333333-3333-3333-3333-333333333333")] // Public organization ID
    public async Task<ActionResult<ApiResponse<string>>> GetPublicData()
    {
        // Simulate async operation to avoid CS1998 warning
        await Task.CompletedTask;
        return Ok(new ApiResponse<string>
        {
            Data = "Public organization data",
            Succeeded = true,
            Message = "Public data retrieved successfully"
        });
    }

    /// <summary>
    /// Example using predefined policy
    /// </summary>
    [HttpPost("upload")]
    [Authorize(Policy = Policies.CanUploadFiles)]
    public async Task<ActionResult<ApiResponse<string>>> UploadFile()
    {
        // Simulate async operation to avoid CS1998 warning
        await Task.CompletedTask;
        return Ok(new ApiResponse<string>
        {
            Data = "File uploaded successfully",
            Succeeded = true,
            Message = "Upload completed"
        });
    }

    /// <summary>
    /// Example using dynamic policy with permission
    /// </summary>
    [HttpDelete("delete-file/{id}")]
    [Authorize(Policy = "Permission:" + Permissions.DeleteFiles)]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteFile(Guid id)
    {
        // Simulate async operation to avoid CS1998 warning
        await Task.CompletedTask;
        return Ok(new ApiResponse<bool>
        {
            Data = true,
            Succeeded = true,
            Message = $"File {id} deleted successfully"
        });
    }

    /// <summary>
    /// Example using manual authorization checks in code
    /// </summary>
    [HttpGet("manual-check")]
    public async Task<ActionResult<ApiResponse<object>>> ManualAuthorizationCheck()
    {
        // Check if user has specific permission
        var hasViewFiles = await _authorizationService.HasPermissionAsync(Permissions.ViewFiles);
        var hasAdminRole = await _authorizationService.HasRoleAsync(Roles.Admin);
        var userPermissions = await _authorizationService.GetUserPermissionsAsync();
        var accessibleRegions = await _authorizationService.GetAccessibleRegionsAsync();
        var currentUser = await _authorizationService.GetCurrentUserAsync();

        if (!hasViewFiles)
        {
            return Forbid("You don't have permission to view files");
        }

        return Ok(new ApiResponse<object>
        {
            Data = new
            {
                HasViewFiles = hasViewFiles,
                HasAdminRole = hasAdminRole,
                UserPermissions = userPermissions,
                AccessibleRegions = accessibleRegions,
                CurrentUser = new
                {
                    currentUser?.Id,
                    currentUser?.AuthUserId,
                    Role = currentUser?.Role?.Key,
                    Organization = currentUser?.Organization?.Name
                }
            },
            Succeeded = true,
            Message = "Authorization check completed"
        });
    }

    /// <summary>
    /// Example demonstrating multiple permission checks
    /// </summary>
    [HttpGet("multiple-permissions")]
    public async Task<ActionResult<ApiResponse<string>>> CheckMultiplePermissions()
    {
        var requiredPermissions = new[] { Permissions.ViewFiles, Permissions.ViewReports };
        var hasAllPermissions = await _authorizationService.HasAllPermissionsAsync(requiredPermissions);
        var hasAnyPermission = await _authorizationService.HasAnyPermissionAsync(requiredPermissions);

        if (!hasAnyPermission)
        {
            return Forbid("You don't have any of the required permissions");
        }

        return Ok(new ApiResponse<string>
        {
            Data = $"Has all permissions: {hasAllPermissions}, Has any permission: {hasAnyPermission}",
            Succeeded = true,
            Message = "Multiple permission check completed"
        });
    }

    /// <summary>
    /// Example demonstrating region-based authorization
    /// </summary>
    [HttpGet("region/{regionId}")]
    public async Task<ActionResult<ApiResponse<string>>> GetRegionData(Guid regionId)
    {
        var hasRegionAccess = await _authorizationService.HasRegionAccessAsync(regionId);

        if (!hasRegionAccess)
        {
            return Forbid($"You don't have access to region {regionId}");
        }

        return Ok(new ApiResponse<string>
        {
            Data = $"Data for region {regionId}",
            Succeeded = true,
            Message = "Region data retrieved successfully"
        });
    }

    /// <summary>
    /// Example demonstrating organization-based authorization
    /// </summary>
    [HttpGet("organization/{organizationId}")]
    public async Task<ActionResult<ApiResponse<string>>> GetOrganizationSpecificData(Guid organizationId)
    {
        var belongsToOrganization = await _authorizationService.BelongsToOrganizationAsync(organizationId);

        if (!belongsToOrganization)
        {
            return Forbid($"You don't belong to organization {organizationId}");
        }

        return Ok(new ApiResponse<string>
        {
            Data = $"Data for organization {organizationId}",
            Succeeded = true,
            Message = "Organization data retrieved successfully"
        });
    }
}