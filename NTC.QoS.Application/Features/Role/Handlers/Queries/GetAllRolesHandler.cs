using MediatR;
using NTC.QoS.Application.Features.Role.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Role;

namespace NTC.QoS.Application.Features.Role.Handlers.Queries;

public class GetAllRolesHandler(IRepository<Domain.Entities.Role> repository) : IRequestHandler<GetAllRolesRequest, PaginatedResponse<GetRoleDto>>
{
    private readonly IRepository<Domain.Entities.Role> _repository = repository;
    public async Task<PaginatedResponse<GetRoleDto>> Handle(GetAllRolesRequest request, CancellationToken cancellationToken)
    {
        var allRoles = await _repository.ListAsync(r => !r.IsDeleted, cancellationToken);

        var roleDtos = allRoles.Select(r => new GetRoleDto
        {
            Id = r.Id,
            Name = r.Name,
            Key = r.Key,
            Description = r.Description
        }).ToList();

        return new PaginatedResponse<GetRoleDto>(roleDtos, request.PageNumber, request.PageSize);
    }
}
