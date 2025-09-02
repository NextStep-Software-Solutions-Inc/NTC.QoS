using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using NTC.QoS.Domain.Authorization;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Infrastructure.Authorization;
using NTC.QoS.Infrastructure.Services;

namespace NTC.QoS.Infrastructure.Extensions;

/// <summary>
/// Extension methods for configuring authorization services
/// </summary>
public static class AuthorizationServiceExtensions
{
    /// <summary>
    /// Adds dynamic authorization services to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddDynamicAuthorization(this IServiceCollection services)
    {
        // Register authorization service
        services.AddScoped<IUserAuthorizationService, UserAuthorizationService>();

        // Register authorization handlers
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddScoped<IAuthorizationHandler, RoleAuthorizationHandler>();
        services.AddScoped<IAuthorizationHandler, OrganizationAuthorizationHandler>();
        services.AddScoped<IAuthorizationHandler, RegionAuthorizationHandler>();

        // Register dynamic policy provider
        services.AddSingleton<IAuthorizationPolicyProvider, DynamicAuthorizationPolicyProvider>();

        // Configure predefined authorization policies
        services.AddAuthorization(options =>
        {
            ConfigurePermissionPolicies(options);
            ConfigureRolePolicies(options);
            ConfigureOrganizationPolicies(options);
            ConfigureRegionPolicies(options);
            ConfigureCombinedPolicies(options);
        });

        return services;
    }

    private static void ConfigurePermissionPolicies(AuthorizationOptions options)
    {
        // Permission-based policies
        options.AddPolicy(Policies.CanViewFiles, policy =>
            policy.AddRequirements(new PermissionRequirement(Permissions.ViewFiles)));

        options.AddPolicy(Policies.CanUploadFiles, policy =>
            policy.AddRequirements(new PermissionRequirement(Permissions.UploadFiles)));

        options.AddPolicy(Policies.CanDeleteFiles, policy =>
            policy.AddRequirements(new PermissionRequirement(Permissions.DeleteFiles)));

        options.AddPolicy(Policies.CanManageUsers, policy =>
            policy.AddRequirements(
                new PermissionRequirement(Permissions.CreateUsers),
                new PermissionRequirement(Permissions.UpdateUsers),
                new PermissionRequirement(Permissions.DeleteUsers)));

        options.AddPolicy(Policies.CanManageRoles, policy =>
            policy.AddRequirements(
                new PermissionRequirement(Permissions.CreateRoles),
                new PermissionRequirement(Permissions.UpdateRoles),
                new PermissionRequirement(Permissions.DeleteRoles)));

        options.AddPolicy(Policies.CanManagePermissions, policy =>
            policy.AddRequirements(
                new PermissionRequirement(Permissions.CreatePermissions),
                new PermissionRequirement(Permissions.UpdatePermissions),
                new PermissionRequirement(Permissions.DeletePermissions)));

        options.AddPolicy(Policies.CanViewReports, policy =>
            policy.AddRequirements(new PermissionRequirement(Permissions.ViewReports)));

        options.AddPolicy(Policies.CanManageOrganizations, policy =>
            policy.AddRequirements(
                new PermissionRequirement(Permissions.CreateOrganizations),
                new PermissionRequirement(Permissions.UpdateOrganizations),
                new PermissionRequirement(Permissions.DeleteOrganizations)));

        options.AddPolicy(Policies.CanViewCells, policy =>
            policy.AddRequirements(new PermissionRequirement(Permissions.ViewCells)));

        options.AddPolicy(Policies.CanManageCells, policy =>
            policy.AddRequirements(
                new PermissionRequirement(Permissions.CreateCells),
                new PermissionRequirement(Permissions.UpdateCells),
                new PermissionRequirement(Permissions.DeleteCells)));

        options.AddPolicy(Policies.CanViewNetworks, policy =>
            policy.AddRequirements(new PermissionRequirement(Permissions.ViewNetworks)));

        options.AddPolicy(Policies.CanManageNetworks, policy =>
            policy.AddRequirements(
                new PermissionRequirement(Permissions.CreateNetworks),
                new PermissionRequirement(Permissions.UpdateNetworks),
                new PermissionRequirement(Permissions.DeleteNetworks)));

        options.AddPolicy(Policies.CanViewProviders, policy =>
            policy.AddRequirements(new PermissionRequirement(Permissions.ViewProviders)));

        options.AddPolicy(Policies.CanManageProviders, policy =>
            policy.AddRequirements(
                new PermissionRequirement(Permissions.CreateProviders),
                new PermissionRequirement(Permissions.UpdateProviders),
                new PermissionRequirement(Permissions.DeleteProviders)));
    }

    private static void ConfigureRolePolicies(AuthorizationOptions options)
    {
        // Role-based policies using actual database role keys
        options.AddPolicy(Policies.AdminOnly, policy =>
            policy.AddRequirements(new RoleRequirement(Roles.Admin)));

        options.AddPolicy(Policies.ManagerOnly, policy =>
            policy.AddRequirements(new RoleRequirement(Roles.Manager)));

        options.AddPolicy(Policies.UserOnly, policy =>
            policy.AddRequirements(new RoleRequirement(Roles.User)));

        options.AddPolicy(Policies.AdminOrManager, policy =>
            policy.RequireAssertion(context =>
                context.User.HasClaim("role", Roles.Admin) ||
                context.User.HasClaim("role", Roles.Manager)));

        // Additional policies for existing roles
        options.AddPolicy("SuperAdminOnly", policy =>
            policy.AddRequirements(new RoleRequirement(Roles.SuperAdmin)));

        options.AddPolicy("FieldOfficerOnly", policy =>
            policy.AddRequirements(new RoleRequirement(Roles.FieldOfficer)));

        options.AddPolicy("RegionalManagerOnly", policy =>
            policy.AddRequirements(new RoleRequirement(Roles.RegionalManager)));

        options.AddPolicy("ViewerOnly", policy =>
            policy.AddRequirements(new RoleRequirement(Roles.Viewer)));
    }

    private static void ConfigureOrganizationPolicies(AuthorizationOptions options)
    {
        // Organization-based policies
        options.AddPolicy(Policies.SameOrganization, policy =>
            policy.AddRequirements(new OrganizationRequirement(requireSameOrganization: true)));
    }

    private static void ConfigureRegionPolicies(AuthorizationOptions options)
    {
        // Region-based policies
        options.AddPolicy(Policies.RegionAccess, policy =>
            policy.AddRequirements(new RegionRequirement(requireAnyRegion: true)));
    }

    private static void ConfigureCombinedPolicies(AuthorizationOptions options)
    {
        // Combined policies
        options.AddPolicy(Policies.AdminOrSameOrganization, policy =>
            policy.RequireAssertion(async context =>
            {
                // Check if user is admin
                if (context.User.HasClaim("role", Roles.Admin))
                    return true;

                // Check if user belongs to same organization (this would need custom logic)
                var authService = context.Resource as IUserAuthorizationService;
                if (authService != null)
                {
                    var userOrgId = await authService.GetUserOrganizationIdAsync();
                    // Additional logic would be needed here to compare with target organization
                    return userOrgId.HasValue;
                }

                return false;
            }));

        options.AddPolicy(Policies.ManagerWithRegionAccess, policy =>
            policy.AddRequirements(
                new RoleRequirement(Roles.Manager),
                new RegionRequirement(requireAnyRegion: true)));
    }
}