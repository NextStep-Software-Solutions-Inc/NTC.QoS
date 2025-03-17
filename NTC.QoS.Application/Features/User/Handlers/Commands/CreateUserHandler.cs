using MediatR;
using NTC.QoS.Application.Features.User.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.User;

namespace NTC.QoS.Application.Features.User.Handlers.Commands;

public class CreateUserHandler(IRepository<Domain.Entities.User> repository,
                                   IRepository<Domain.Entities.Role> roleRepository,
                                   IRepository<Domain.Entities.Organization> organizationRepository) : IRequestHandler<CreateUserRequest, ApiResponse<CreateUserDto>>
{
    private readonly IRepository<Domain.Entities.User> _repository = repository;
    private readonly IRepository<Domain.Entities.Role> _roleRepository = roleRepository;
    private readonly IRepository<Domain.Entities.Organization> _organizationRepository = organizationRepository;

    public async Task<ApiResponse<CreateUserDto>> Handle(CreateUserRequest request, CancellationToken cancellationToken)
    {
        var existingUser= await _repository.FindAsync(u => u.RoleId == request.User.RoleId
                                                           && u.OrganizationId == request.User.OrganizationId
                                                           && u.AuthUserId == request.User.AuthUserId, cancellationToken);

        if (existingUser is not null)
        {
            return new ApiResponse<CreateUserDto>
            {
                Message = "User already exists.",
                Succeeded = false,
                Error = new[] { "User with the same RoleId and OrganizationId already exists." }
            };
        }

        
        var role = await _roleRepository.GetByIdAsync(request.User.RoleId, cancellationToken);
        if (role is null)
        {
            return new ApiResponse<CreateUserDto>
            {
                Message = "Role not found.",
                Succeeded = false,
                Error = new[] { "Role with the specified Id does not exist." }
            };
        }
        
        var organization = await _organizationRepository.GetByIdAsync(request.User.OrganizationId, cancellationToken);
        if (organization is null)
        {
            return new ApiResponse<CreateUserDto>
            {
                Message = "Organization not found.",
                Succeeded = false,
                Error = new[] { "Organization with the specified Id does not exist." }
            };
        }

        var newUser = new Domain.Entities.User
        {
            RoleId = role.Id,
            OrganizationId = organization.Id,
            AuthUserId = request.User.AuthUserId
        };

        _repository.Add(newUser);
        await _repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<CreateUserDto>
        {
            Data = new CreateUserDto
            {
                RoleId = newUser.RoleId,
                OrganizationId = newUser.OrganizationId,
                AuthUserId = newUser.AuthUserId
            },
            Message = "User created successfully.",
            Succeeded = true
        };
    }
}
