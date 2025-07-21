using MediatR;
using NTC.QoS.Application.Features.Permission.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Permission;

namespace NTC.QoS.Application.Features.Permission.Handlers.Commands;

public class UpdatePermissionByIdHandler(IRepository<Domain.Entities.Permission> repository) : IRequestHandler<UpdatePermissionByIdRequest, ApiResponse<PermissionDto>>
{
    private readonly IRepository<Domain.Entities.Permission> _repository = repository;

    public async Task<ApiResponse<PermissionDto>> Handle(UpdatePermissionByIdRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Permission is null)
        {
            return new ApiResponse<PermissionDto>
            {
                Data = null,
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    { "Permission", ["Permission data is required."] }
                },
                Message = "The request does not contain permission update data.",
            };
        }

        var permissionQuery = _repository.Query(p => p.Id == request.Id);
        var permission = await _repository.FirstOrDefaultAsync(permissionQuery, cancellationToken);
        if (permission is null)
        {
            return new ApiResponse<PermissionDto>
            {
                Data = null,
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    { "Id", ["Permission not found."] }
                },
                Message = "The permission you are trying to edit does not exist.",
            };
        }

        if (!string.IsNullOrEmpty(request.Permission.Name))
        {
            var existingPermissionWithNameQuery = _repository.Query(
                p => p.Name == request.Permission.Name && p.Id != request.Id);
            var existingPermissionWithName = await _repository.FirstOrDefaultAsync(
                existingPermissionWithNameQuery, cancellationToken);

            if (existingPermissionWithName is not null)
            {
                return new ApiResponse<PermissionDto>
                {
                    Data = null,
                    Succeeded = false,
                    Errors = new Dictionary<string, string[]>
                    {
                        { "duplicate", ["Permission name already exists."] }
                    },
                    Message = "The permission with the specified name already exists."
                };
            }
            permission.Name = request.Permission.Name;
        }

        if (!string.IsNullOrEmpty(request.Permission.Key))
        {
            var existingPermissionWithKeyQuery = _repository.Query(
            p => p.Key == request.Permission.Key && p.Id != request.Id);
            var existingPermissionWithKey = await _repository.FirstOrDefaultAsync(existingPermissionWithKeyQuery, cancellationToken);

            if (existingPermissionWithKey is not null)
            {
                return new ApiResponse<PermissionDto>
                {
                    Data = null,
                    Succeeded = false,
                    Errors = new Dictionary<string, string[]>
                    {
                        { "Key", ["Permission key already exists."] }
                    },
                    Message = "A permission with the specified key already exists.",
                };
            }
            permission.Key = request.Permission.Key;
        }

        if (!string.IsNullOrEmpty(request.Permission.Description))
        {
            permission.Description = request.Permission.Description;
        }

        _repository.Update(permission);
        await _repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<PermissionDto>
        {
            Succeeded = true,
            Data = new PermissionDto
            {
                Id = permission.Id,
                Name = permission.Name,
                Key = permission.Key,
                Description = permission.Description
            },
            Message = "Permission updated successfully.",
        };
    }
}
