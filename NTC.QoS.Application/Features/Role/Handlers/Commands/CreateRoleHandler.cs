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
        var existingRoleQuery = _roleRepository.Query(r => r.Name == request.Role.Name);
        var existingRole = await _roleRepository.FirstOrDefaultAsync(existingRoleQuery, cancellationToken);

        if (existingRole is not null)
        {
            if (existingRole.IsDeleted)
            {
                existingRole.IsDeleted = false;
                _roleRepository.Update(existingRole);
                await _roleRepository.SaveChangesAsync(cancellationToken);

                return new ApiResponse<RoleDto>
                {
                    Data = new RoleDto
                    {
                        Id = existingRole.Id,
                        Name = existingRole.Name,
                        Key = existingRole.Key,
                        Description = existingRole.Description ?? string.Empty,
                    },
                    Succeeded = true,
                    Message = "Role restored successfully."
                };
            }
            else 
            {
                return new ApiResponse<RoleDto>
                {
                    Message = "Role already exists.",
                    Succeeded = false,
                    Errors = new Dictionary<string, string[]>
                    {
                        { "Duplicate", ["Role with the same name already exists."] }
                    }
                };
            }
        }

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
