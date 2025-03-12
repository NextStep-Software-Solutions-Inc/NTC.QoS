using MediatR;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Application.Features.Role.Requests;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Role;

namespace NTC.QoS.Application.Features.Role.Handlers.Queries;

public class GetRoleByIdHandler(IRepository<Domain.Entities.Role> repository) : IRequestHandler<GetRoleByIdRequest, ApiResponse<GetRoleDto>>
{
    private readonly IRepository<Domain.Entities.Role> _repository = repository;

    public async Task<ApiResponse<GetRoleDto>> Handle(GetRoleByIdRequest request, CancellationToken cancellationToken)
    {
        var role = await _repository.GetByIdAsync(request.Id, cancellationToken);

        if (role is null)
        {
            return new ApiResponse<GetRoleDto>
            {
                Succeeded = false,
                Message = "Role not found.",
                Error = new[] { "Role with the specified ID does not exist." },
            };
        }

        return new ApiResponse<GetRoleDto>
        {
            Succeeded = true,
            Message = "Role retrieved successfully",
            Data = new GetRoleDto
            {
                Id = role.Id,
                Name = role.Name,
                Key = role.Key,
                Description = role.Description
            },
        };
    }
}
