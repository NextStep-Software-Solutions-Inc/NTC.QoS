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
        var role = await _repository.FindAsync(r => r.Id == request.Id && !r.IsDeleted, cancellationToken);
        if (role is null) return new ApiResponse<RoleDto>("Role not found.");

        if (!string.IsNullOrEmpty(request.Role.Name))
        {
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
            if (existingRoleWithKey is not null) return new ApiResponse<RoleDto>("Role key already exists.");
            role.Key = request.Role.Key;
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
