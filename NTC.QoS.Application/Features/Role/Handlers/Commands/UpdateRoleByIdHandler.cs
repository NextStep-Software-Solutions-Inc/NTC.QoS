using MediatR;
using NTC.QoS.Application.Features.Role.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Role;

namespace NTC.QoS.Application.Features.Role.Handlers.Commands;

public class UpdateRoleByIdHandler(IRepository<Domain.Entities.Role> repository) : IRequestHandler<UpdateRoleByIdRequest, ApiResponse<UpdateRoleDto>>
{
    private readonly IRepository<Domain.Entities.Role> _repository = repository;

    public async Task<ApiResponse<UpdateRoleDto>> Handle(UpdateRoleByIdRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Role is null)
        {
            return new ApiResponse<UpdateRoleDto>
            {
                Data = null,
                Succeeded = false,
                Error = new[] { "Role data is missing." },
                Message = "The request does not contain role update data.",
            };
        }

        var role = await _repository.FindAsync(r => r.Id == request.Id && !r.IsDeleted, cancellationToken);

        if (role is null)
        {
            return new ApiResponse<UpdateRoleDto>
            {
                Data = null,
                Succeeded = false,
                Error = new[] { "Role not found." },
                Message = "The role you are trying to edit does not exist.",
            };
        }

        var existingRoleWithName = await _repository.FindAsync(
            r => r.Name == request.Role.Name && r.Id != request.Id && !r.IsDeleted,
            cancellationToken);

        var existingRoleWithKey = await _repository.FindAsync(
            r => r.Key == request.Role.Key && r.Id != request.Id && !r.IsDeleted,
            cancellationToken);

        if (existingRoleWithName is not null)
        {
            return new ApiResponse<UpdateRoleDto>
            {
                Data = null,
                Succeeded = false,
                Error = new[] { "Role name already exists." },
                Message = "A role with the specified name already exists.",
            };
        }

        if (existingRoleWithKey is not null)
        {
            return new ApiResponse<UpdateRoleDto>
            {
                Data = null,
                Succeeded = false,
                Error = new[] { "Role key already exists." },
                Message = "A role with the specified key already exists.",
            };
        }

        role.Name = request.Role.Name;
        role.Key = request.Role.Key;
        role.Description = request.Role.Description;

        _repository.Update(role);
        await _repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<UpdateRoleDto>
        {
            Succeeded = true,
            Data = new UpdateRoleDto
            {
                Name = request.Role.Name,
                Key = request.Role.Key,
                Description = request.Role.Description
            },
            Message = "Role updated successfully.",
        };
    }
}
