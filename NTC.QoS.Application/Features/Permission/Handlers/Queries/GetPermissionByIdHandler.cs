using MediatR;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Application.Features.Permission.Requests;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Permission;

namespace NTC.QoS.Application.Features.Permission.Handlers.Queries;

public class GetPermissionByIdHandler(IRepository<Domain.Entities.Permission> repository) : IRequestHandler<GetPermissionByIdRequest, ApiResponse<GetPermissionDto>>
{
    private readonly IRepository<Domain.Entities.Permission> _repository = repository;

    public async Task<ApiResponse<GetPermissionDto>> Handle(GetPermissionByIdRequest request, CancellationToken cancellationToken)
    {
        var permission = await _repository.GetByIdAsync(request.Id, cancellationToken);

        if (permission is null)
        {
            return new ApiResponse<GetPermissionDto>
            {
                Succeeded = false,
                Message = "Permission not found.",
                Errors = new Dictionary<string, string[]>
                {
                    {"Id", ["Permission with the specified ID does not exist."] }
                },
            };
        }

        return new ApiResponse<GetPermissionDto>
        {
            Succeeded = true,
            Message = "Permission retrieved successfully",
            Data = new GetPermissionDto
            {
                Id = permission.Id,
                Name = permission.Name,
                Key = permission.Key,
                Description = permission.Description
            },
        };
    }
}
