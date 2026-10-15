using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NTC.QoS.Application.Features.User.Requests;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.User.Handlers.Commands;

public class CreateSessionHandler(
    IUserRepository userRepository,
    IClerkUserService clerkUserService,
    ILogger<CreateSessionHandler>? logger = null) : IRequestHandler<CreateSessionRequest, ApiResponse<bool>>
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IClerkUserService _clerkUserService = clerkUserService;
    private readonly ILogger<CreateSessionHandler>? _logger = logger;

    public async Task<ApiResponse<bool>> Handle(CreateSessionRequest request, CancellationToken cancellationToken)
    {
        var userQuery = _userRepository.Query(u => u.AuthUserId == request.AuthUserId && !u.IsDeleted)
            .Include(u => u.Role);
        var user = await _userRepository.FirstOrDefaultAsync(userQuery, cancellationToken);

        if (user is null)
        {
            return new ApiResponse<bool>
            {
                Succeeded = false,
                Message = $"User with AuthUserId {request.AuthUserId} not found."
            };
        }

        var roleKey = user.Role?.Key ?? "unknown";
        var permissions = user.Role?.Permissions ?? [];

        try
        {
            await _clerkUserService.UpdateUserMetadataAsync(user.AuthUserId, new UserMetadataPayload
            {
                PublicMetadata = new Dictionary<string, object>
                {
                    { "role", roleKey },
                    { "permissions", permissions }
                }
            });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex,
                "Failed to synchronize Clerk metadata for AuthUserId {AuthUserId} (Role: {RoleKey}, PermissionsCount: {PermissionsCount}): {ErrorMessage}",
                user.AuthUserId,
                roleKey,
                permissions.Count,
                StringHelper.SanitizeLogMessage(ex.Message));

            return new ApiResponse<bool>
            {
                Data = false,
                Succeeded = false,
                Message = $"Failed to synchronize session metadata with Clerk: {ex.Message}"
            };
        }

        return new ApiResponse<bool>
        {
            Data = true,
            Message = "Session created successfully.",
            Succeeded = true
        };
    }
}
