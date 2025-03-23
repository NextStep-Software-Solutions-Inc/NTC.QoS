using MediatR;
using NTC.QoS.Application.Features.Role.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Role;

namespace NTC.QoS.Application.Features.Role.Handlers.Commands;

public class UpdateRoleByIdHandler(IRepository<Domain.Entities.Role> repository) : IRequestHandler<UpdateRoleByIdRequest, ApiResponse<RoleDto>>
{
    private readonly IRepository<Domain.Entities.Role> _repository = repository;

    public async Task<ApiResponse<RoleDto>> Handle(UpdateRoleByIdRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Role is null) // replace with fluent validation
        {
            return new ApiResponse<UpdateRoleDto>
            {
                Data = null,
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    { "Role", ["Role data is required."] }
                },
                Message = "The request does not contain role update data.",
            };
        }

        var role = await _repository.FindAsync(r => r.Id == request.Id && !r.IsDeleted, cancellationToken);
        if (role is null) return new ApiResponse<RoleDto>("Role not found.");

        if (!string.IsNullOrEmpty(request.Role.Name))
        {
            return new ApiResponse<UpdateRoleDto>
            {
                Data = null,
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    { "Id", ["Role not found."] }
                },
                Message = "The role you are trying to edit does not exist.",
            };
        }

        var existingRoleWithName = await _repository.FindAsync(
            r => r.Name == request.Role.Name && r.Id != request.Id && !r.IsDeleted,
            cancellationToken);
            if (existingRoleWithName is not null) return new ApiResponse<RoleDto>("Role name already exists.");
            role.Name = request.Role.Name;
        }

        if (!string.IsNullOrEmpty(request.Role.Key))
        {
            var existingRoleWithKey = await _repository.FindAsync(
            r => r.Key == request.Role.Key && r.Id != request.Id && !r.IsDeleted,
            cancellationToken);

        if (existingRoleWithName is not null)
        {
            return new ApiResponse<UpdateRoleDto>
            {
                Data = null,
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    { "Name", ["Role name already exists."] }
                },
                Message = "A role with the specified name already exists.",
            };
        }

        if (existingRoleWithKey is not null)
        {
            return new ApiResponse<UpdateRoleDto>
            {
                Data = null,
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    { "Key", ["Role key already exists."] }
                },
                Message = "A role with the specified key already exists.",
            };
        }

        if (!string.IsNullOrEmpty(request.Role.Key)) role.Description = request.Role.Description;
        
        _repository.Update(role);
        await _repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<RoleDto>
        {
            Succeeded = true,
            Data = new RoleDto
            {
                Id = role.Id,
                Name = role.Name,
                Key = role.Key,
                Description = role.Description ?? string.Empty
            },
            Message = "Role updated successfully.",
        };
    }
}
