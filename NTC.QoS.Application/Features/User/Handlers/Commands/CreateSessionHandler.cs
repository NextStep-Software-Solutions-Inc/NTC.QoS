using MediatR;
using NTC.QoS.Application.Features.User.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.User.Handlers.Commands;

public class CreateSessionHandler(IUserRepository userRepository, IClerkUserService clerkUserService) : IRequestHandler<CreateSessionRequest, ApiResponse<bool>>
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IClerkUserService _clerkUserService = clerkUserService;

    public async Task<ApiResponse<bool>> Handle(CreateSessionRequest request, CancellationToken cancellationToken)
    {
        var userQuery = _userRepository.Query(u => u.AuthUserId == request.AuthUserId && !u.IsDeleted);
        var user = await _userRepository.FirstOrDefaultAsync(userQuery, cancellationToken);

        if (user is null)
        {
            return new ApiResponse<bool>
            {
                Succeeded = false,
                Message = $"User with AuthUserId {request.AuthUserId} not found."
            };
        }

        //if (user.Role is null || user.Role.RolePermissions is null || user.Organization is null)
        //{
        //    return new ApiResponse<bool>
        //    {
        //        Succeeded = false,
        //        Message = "Role or Role Permissions or Role Organization are missing."
        //    };
        //}

        //var metadataPayload = new
        //{
        //    public_metadata = new
        //    {
        //        role = user.Role.Key,
        //        organization = user.Role.Organization.Name,
        //        permissions = user.Role.RolePermissions.Select(rp => rp.Permission!.Key).ToList()
        //    }
        //};

        await _clerkUserService.UpdateUserMetadataAsync(user.AuthUserId, new UserMetadataPayload
        {
            PublicMetadata = new Dictionary<string, object>
            {
                { "role", user.Role?.Key ?? "unknown" },
                { "organization", user.Organization?.Name ?? "unknown" },
                { "permissions", user.Role?.RolePermissions?.Select(rp => rp.Permission?.Key).ToList() ?? [] }
            }
        });

        return new ApiResponse<bool>
        {
            Data = true,
            Message = "Session created successfully.",
            Succeeded = true
        };
    }
}
