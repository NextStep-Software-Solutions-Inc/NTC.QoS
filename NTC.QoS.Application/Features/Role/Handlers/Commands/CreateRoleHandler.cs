using MediatR;
using NTC.QoS.Application.Features.Role.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Role;

namespace NTC.QoS.Application.Features.Role.Handlers.Commands;

public class CreateRoleHandler (IRepository<Domain.Entities.Role> repository): IRequestHandler<CreateRoleRequest, ApiResponse<CreateRoleDto>>
{
    private readonly IRepository<Domain.Entities.Role> _repository = repository;

    public async Task<ApiResponse<CreateRoleDto>> Handle(CreateRoleRequest request, CancellationToken cancellationToken)
    {
        var role = await _repository.FindAsync(r => r.Name == request.Role.Name && !r.IsDeleted, cancellationToken);

        if (role is not null)
        {
            return new ApiResponse<CreateRoleDto>
            {
                Succeeded = false,
                Message = "Role already exists."
            };
        }
                
        var newRole = new Domain.Entities.Role
        {
            Name = request.Role.Name,
            Key = request.Role.Key,
            Description = request.Role.Description,
        };

        _repository.Add(newRole);
        await _repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<CreateRoleDto>
        {
            Data = new CreateRoleDto
            {
                Name = newRole.Name,
                Key = newRole.Key,
                Description = newRole.Description,
            },
            Succeeded = true,
            Message = "Role created successfully.",
        };
    }
}
