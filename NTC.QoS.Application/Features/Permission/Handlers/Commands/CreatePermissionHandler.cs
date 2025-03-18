using MediatR;
using NTC.QoS.Application.Features.Permission.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Permission;

namespace NTC.QoS.Application.Features.Permission.Handlers.Commands;

public class CreatePermissionHandler(IRepository<Domain.Entities.Permission> repository): IRequestHandler<CreatePermissionRequest, ApiResponse<CreatePermissionDto>>
{
    private readonly IRepository<Domain.Entities.Permission> _repository = repository;

    public async Task<ApiResponse<CreatePermissionDto>> Handle(CreatePermissionRequest request, CancellationToken cancellationToken)
    {
        var permission = await _repository.FindAsync(p => p.Name == request.Permission.Name && !p.IsDeleted, cancellationToken);

        if (permission is not null)
        {
            return new ApiResponse<CreatePermissionDto>
            {
                Succeeded = false,
                Message = "Permission already exists."
            };
        }
                
        var newPermission = new Domain.Entities.Permission
        {
            Name = request.Permission.Name,
            Key = request.Permission.Key,
            Description = request.Permission.Description,
        };

        _repository.Add(newPermission);
        await _repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<CreatePermissionDto>
        {
            Data = new CreatePermissionDto
            {
                Name = newPermission.Name,
                Key = newPermission.Key,
                Description = newPermission.Description,
            },
            Succeeded = true,
            Message = "Permission created successfully.",
        };
    }
}
