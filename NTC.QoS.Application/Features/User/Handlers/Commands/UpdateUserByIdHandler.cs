using MediatR;
using NTC.QoS.Application.Features.User.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.User;

namespace NTC.QoS.Application.Features.User.Handlers.Commands;

public class UpdateUserByIdHandler(IUserRepository repository,
                                   IRepository<Domain.Entities.Role> roleRepository,
                                   IRepository<Domain.Entities.Organization> organizationRepository) : IRequestHandler<UpdateUserByIdRequest, ApiResponse<UpdateUserDto>>
{
    private readonly IUserRepository _repository = repository;
    private readonly IRepository<Domain.Entities.Role> _roleRepository = roleRepository;
    private readonly IRepository<Domain.Entities.Organization> _organizationRepository = organizationRepository;
    public async Task<ApiResponse<UpdateUserDto>> Handle(UpdateUserByIdRequest request, CancellationToken cancellationToken)
    {
        var user = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (user is null)
        {
            return new ApiResponse<UpdateUserDto>
            {
                Data = null,
                Succeeded = false,
                Error = new[] { $"User with ID [{request.Id}] not found." },
                Message = $"User does not exist."
            };
        }

        var existingUser = await _repository.GetAllUsersAsync(cancellationToken);
        if (existingUser.Any(u => u.Id != request.Id &&
                                 u.RoleId == request.User.RoleId &&
                                 u.OrganizationId == request.User.OrganizationId &&
                                 u.AuthUserId == request.User.AuthUserId))
        {
            return new ApiResponse<UpdateUserDto>
            {
                Data = null,
                Succeeded = false,
                Error = new[] { "User with the same Role, Organization, and AuthUserId already exists." },
                Message = "Duplicate user data."
            };
        }

        var role = await _roleRepository.GetByIdAsync(request.User.RoleId, cancellationToken);
        if (role is null)
        {
            return new ApiResponse<UpdateUserDto>
            {
                Message = "Role not found.",
                Succeeded = false,
                Error = new[] { "Role with the specified Id does not exist." }
            };
        }

        var organization = await _organizationRepository.GetByIdAsync(request.User.OrganizationId, cancellationToken);
        if (organization is null)
        {
            return new ApiResponse<UpdateUserDto>
            {
                Message = "Organization not found.",
                Succeeded = false,
                Error = new[] { "Organization with the specified Id does not exist." }
            };
        }

        user.RoleId = role.Id;
        user.OrganizationId = organization.Id;
        user.AuthUserId = request.User.AuthUserId;

        _repository.Update(user);
        await _repository.SaveChangesAsync(cancellationToken);

        var result = new UpdateUserDto
        {
            RoleId = user.RoleId,
            OrganizationId = user.OrganizationId,
            AuthUserId = user.AuthUserId
        };

        return new ApiResponse<UpdateUserDto>
        {
            Data = result,
            Message = "User updated successfully.",
            Succeeded = true
        };
    }
}
