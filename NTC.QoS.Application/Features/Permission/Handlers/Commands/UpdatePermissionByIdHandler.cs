using MediatR;
using NTC.QoS.Application.Features.Permission.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Permission;

namespace NTC.QoS.Application.Features.Permission.Handlers.Commands;

public class UpdatePermissionByIdHandler(IRepository<Domain.Entities.Permission> repository) : IRequestHandler<UpdatePermissionByIdRequest, ApiResponse<UpdatePermissionDto>>
{
    private readonly IRepository<Domain.Entities.Permission> _repository = repository;

    public async Task<ApiResponse<UpdatePermissionDto>> Handle(UpdatePermissionByIdRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Permission is null)
        {
            return new ApiResponse<UpdatePermissionDto>
            {
                Data = null,
                Succeeded = false,
                Error = new[] { "Permission data is missing." },
                Message = "The request does not contain permission update data.",
            };
        }

        var permission = await _repository.FindAsync(p => p.Id == request.Id && !p.IsDeleted, cancellationToken);

        if (permission is null)
        {
            return new ApiResponse<UpdatePermissionDto>
            {
                Data = null,
                Succeeded = false,
                Error = new[] { "Permission not found." },
                Message = "The permission you are trying to edit does not exist.",
            };
        }

        var existingPermissionWithName = await _repository.FindAsync(
            p => p.Name == request.Permission.Name && p.Id != request.Id && !p.IsDeleted,
            cancellationToken);

        var existingPermissionWithKey = await _repository.FindAsync(
            p => p.Key == request.Permission.Key && p.Id != request.Id && !p.IsDeleted,
            cancellationToken);

        if (existingPermissionWithName is not null)
        {
            return new ApiResponse<UpdatePermissionDto>
            {
                Data = null,
                Succeeded = false,
                Error = new[] { "Permission name already exists." },
                Message = "A permission with the specified name already exists.",
            };
        }

        if (existingPermissionWithKey is not null)
        {
            return new ApiResponse<UpdatePermissionDto>
            {
                Data = null,
                Succeeded = false,
                Error = new[] { "Permission key already exists." },
                Message = "A permission with the specified key already exists.",
            };
        }

        permission.Name = request.Permission.Name;
        permission.Key = request.Permission.Key;
        permission.Description = request.Permission.Description;

        _repository.Update(permission);
        await _repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<UpdatePermissionDto>
        {
            Succeeded = true,
            Data = new UpdatePermissionDto
            {
                Name = request.Permission.Name,
                Key = request.Permission.Key,
                Description = request.Permission.Description
            },
            Message = "Permission updated successfully.",
        };
    }
}
