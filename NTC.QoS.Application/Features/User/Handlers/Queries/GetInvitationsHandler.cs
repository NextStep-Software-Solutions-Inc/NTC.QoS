using MediatR;
using Microsoft.Extensions.Logging;
using NTC.QoS.Application.Features.User.Requests;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.User;
using System.Text.Json;

namespace NTC.QoS.Application.Features.User.Handlers.Queries;

public class GetInvitationsHandler(
    IClerkUserService clerkUserService,
    IRepository<Domain.Entities.Role> roleRepository,
    IRepository<Domain.Entities.Region> regionRepository,
    ILogger<GetInvitationsHandler> logger) : IRequestHandler<GetInvitationsRequest, ApiResponse<List<InvitationDto>>>
{
    private readonly IClerkUserService _clerkUserService = clerkUserService;
    private readonly IRepository<Domain.Entities.Role> _roleRepository = roleRepository;
    private readonly IRepository<Domain.Entities.Region> _regionRepository = regionRepository;
    private readonly ILogger<GetInvitationsHandler> _logger = logger;

    public async Task<ApiResponse<List<InvitationDto>>> Handle(GetInvitationsRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var clerkInvitations = await _clerkUserService.GetInvitationsAsync(request.Status);

            // Fetch roles and regions for metadata resolution
            var rolesQuery = _roleRepository.Query(r => true);
            var roles = await _roleRepository.ToListAsync(rolesQuery, cancellationToken);
            var roleMap = roles.ToDictionary(r => r.Id);

            var regionsQuery = _regionRepository.Query(reg => !reg.IsDeleted);
            var regions = await _regionRepository.ToListAsync(regionsQuery, cancellationToken);
            var regionMap = regions.ToDictionary(reg => reg.Id);

            var dtos = new List<InvitationDto>();

            foreach (var inv in clerkInvitations)
            {
                var dto = new InvitationDto
                {
                    Id = inv.Id,
                    Email = inv.EmailAddress,
                    Status = inv.Status,
                    Url = inv.Url,
                    CreatedAt = ConvertTimestamp(inv.CreatedAt),
                    UpdatedAt = ConvertTimestamp(inv.UpdatedAt)
                };

                try
                {
                    if (inv.PublicMetadata.HasValue && inv.PublicMetadata.Value.ValueKind == JsonValueKind.Object)
                    {
                        var meta = inv.PublicMetadata.Value;

                        // Resolve primary role
                        if (meta.TryGetProperty("role_id", out var roleIdProp) &&
                            roleIdProp.ValueKind == JsonValueKind.String &&
                            Guid.TryParse(roleIdProp.GetString(), out var roleId))
                        {
                            dto.RoleId = roleId;
                            if (roleMap.TryGetValue(roleId, out var role))
                            {
                                dto.RoleName = role.Name;
                                dto.RoleScope = role.Scope;
                            }
                        }

                        // Resolve target regions
                        if (meta.TryGetProperty("regions", out var regionsProp) &&
                            regionsProp.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var elem in regionsProp.EnumerateArray())
                            {
                                if (elem.ValueKind == JsonValueKind.Object &&
                                    elem.TryGetProperty("region_id", out var regIdProp) &&
                                    regIdProp.ValueKind == JsonValueKind.String &&
                                    Guid.TryParse(regIdProp.GetString(), out var regId))
                                {
                                    regionMap.TryGetValue(regId, out var regEntity);

                                    Guid? regRoleId = null;
                                    if (elem.TryGetProperty("role_id", out var regRoleIdProp) &&
                                        regRoleIdProp.ValueKind == JsonValueKind.String &&
                                        Guid.TryParse(regRoleIdProp.GetString(), out var parsedRoleId))
                                    {
                                        regRoleId = parsedRoleId;
                                    }

                                    string? regRoleName = null;
                                    if (regRoleId.HasValue && roleMap.TryGetValue(regRoleId.Value, out var regRole))
                                    {
                                        regRoleName = regRole.Name;
                                    }

                                    string status = "Active";
                                    if (elem.TryGetProperty("status", out var stProp) &&
                                        stProp.ValueKind == JsonValueKind.String)
                                    {
                                        status = stProp.GetString() ?? "Active";
                                    }

                                    dto.Regions.Add(new InvitationRegionDto
                                    {
                                        RegionId = regId,
                                        RegionName = regEntity?.Name ?? regId.ToString(),
                                        RegionCode = regEntity?.Code,
                                        RoleId = regRoleId,
                                        RoleName = regRoleName,
                                        Status = status
                                    });
                                }
                            }
                        }
                    }
                }
                catch
                {
                    // Non-fatal: if metadata for a specific invitation is corrupted or drifted, continue listing
                }

                dtos.Add(dto);
            }

            // Order newest first
            dtos = dtos.OrderByDescending(d => d.CreatedAt ?? DateTimeOffset.MinValue).ToList();

            return new ApiResponse<List<InvitationDto>>
            {
                Data = dtos,
                Succeeded = true,
                Message = $"Successfully retrieved {dtos.Count} invitations."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve invitations: {Message}", ex.Message);
            return new ApiResponse<List<InvitationDto>>
            {
                Succeeded = false,
                Message = $"Failed to retrieve invitations: {ex.Message}",
                Errors = new Dictionary<string, string[]>
                {
                    { "Invitations", [ex.Message] }
                }
            };
        }
    }

    private static DateTimeOffset? ConvertTimestamp(long? timestamp)
    {
        if (!timestamp.HasValue) return null;
        return timestamp.Value > 100_000_000_000
            ? DateTimeOffset.FromUnixTimeMilliseconds(timestamp.Value)
            : DateTimeOffset.FromUnixTimeSeconds(timestamp.Value);
    }
}
