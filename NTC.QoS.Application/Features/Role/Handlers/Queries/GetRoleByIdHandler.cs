using MediatR;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Application.Features.Role.Requests;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Role;

namespace NTC.QoS.Application.Features.Role.Handlers.Queries;

public class GetRoleByIdHandler(IRepository<Domain.Entities.Role> repository) : IRequestHandler<GetRoleByIdRequest, ApiResponse<RoleDto>>
{
    private readonly IRepository<Domain.Entities.Role> _repository = repository;

    public async Task<ApiResponse<RoleDto>> Handle(GetRoleByIdRequest request, CancellationToken cancellationToken)
    {
        var roleQuery = _repository.Query(r => r.Id.Equals(request.Id));
        var role = await _repository.FirstOrDefaultAsync(roleQuery, cancellationToken);

        if (role is null)  return new ApiResponse<RoleDto>("Role not found.");

        if (role is null)
        {
            return new ApiResponse<RoleDto>
            {
                Succeeded = false,
                Message = "Role not found.",
                Errors = new Dictionary<string, string[]>
                {
                    {"Id", ["Role with the specified ID does not exist."] }
                }
            };
        }

        return new ApiResponse<RoleDto>
        {
            Succeeded = true,
            Message = "Role retrieved successfully.",
            Data = new RoleDto
            {
                Id = role.Id,
                Name = role.Name,
                Key = role.Key,
                Description = role.Description ?? string.Empty
            },
        };
    }
}
