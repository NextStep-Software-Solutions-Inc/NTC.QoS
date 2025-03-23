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

        var existingRoleWithName = await _repository.FindAsync(r => r.Id == request.Id && !r.IsDeleted, cancellationToken);
        if (existingRoleWithName is null) return new ApiResponse<RoleDto>("Role not found.");

        // palyload validation will be implemented in the validator class

        existingRoleWithName.Name = request.Role.Name ?? existingRoleWithName.Name;
        existingRoleWithName.Key = request.Role.Key ?? existingRoleWithName.Key;
        existingRoleWithName.Description = request.Role.Description ?? existingRoleWithName.Description;

        _repository.Update(existingRoleWithName);
        await _repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<RoleDto>
        {
            Succeeded = true,
            Data = new RoleDto
            {
                Id = existingRoleWithName.Id,
                Name = existingRoleWithName.Name,
                Key = existingRoleWithName.Key,
                Description = existingRoleWithName.Description
            },
            Message = "Role updated successfully.",
        };
    }
}
