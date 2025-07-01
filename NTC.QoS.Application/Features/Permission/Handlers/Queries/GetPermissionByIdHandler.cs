using MediatR;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Application.Features.Permission.Requests;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Permission;

namespace NTC.QoS.Application.Features.Permission.Handlers.Queries;

public class GetPermissionByIdHandler(IRepository<Domain.Entities.Permission> repository) : IRequestHandler<GetPermissionByIdRequest, ApiResponse<PermissionDto>>
{
    private readonly IRepository<Domain.Entities.Permission> _repository = repository;

    public async Task<ApiResponse<PermissionDto>> Handle(GetPermissionByIdRequest request, CancellationToken cancellationToken)
    {
        var permissionQuery = _repository.Query(p => p.Id.Equals(request.Id));
        var permission = await _repository.FirstOrDefaultAsync(permissionQuery, cancellationToken);

        if (permission is null)
        {
            return new ApiResponse<PermissionDto>
            {
                Succeeded = false,
                Message = "Permission not found.",
                Errors = new Dictionary<string, string[]>
                {
                    {"Id", new[] { "Permission with the specified ID does not exist." } }
                },
            };
        }

        return new ApiResponse<PermissionDto>
        {
            Succeeded = true,
            Message = "Permission retrieved successfully.",
            Data = new PermissionDto
            {
                Id = permission.Id,
                Name = permission.Name,
                Key = permission.Key,
                Description = permission.Description
            },
        };
    }
}
