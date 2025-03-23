using MediatR;
using NTC.QoS.Application.Features.Role.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Role;

namespace NTC.QoS.Application.Features.Role.Handlers.Commands;

public class CreateRoleHandler (IRepository<Domain.Entities.Role> roleRepository): IRequestHandler<CreateRoleRequest, ApiResponse<RoleDto>>
{
    private readonly IRepository<Domain.Entities.Role> _roleRepository = roleRepository;

    public async Task<ApiResponse<RoleDto>> Handle(CreateRoleRequest request, CancellationToken cancellationToken)
    {
        var role = await _roleRepository.FindAsync(r => r.Name == request.Role.Name && !r.IsDeleted, cancellationToken);
        if (role is not null) return new ApiResponse<RoleDto>("Role already exists.");
                
        var newRole = new Domain.Entities.Role
        {
            Name = request.Role.Name,
            Key = request.Role.Key,
            Description = request.Role.Description,
        };

        _roleRepository.Add(newRole);
        await _roleRepository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<RoleDto>
        {
            Data = new RoleDto
            {
                Id = newRole.Id,
                Name = newRole.Name,
                Key = newRole.Key,
                Description = newRole.Description ?? string.Empty,
            },
            Succeeded = true,
            Message = "Role created successfully.",
        };
    }
}
