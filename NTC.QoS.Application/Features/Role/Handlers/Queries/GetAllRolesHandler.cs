using MediatR;
using NTC.QoS.Application.Features.Role.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Role;

namespace NTC.QoS.Application.Features.Role.Handlers.Queries;

public class GetAllRolesHandler(IRepository<Domain.Entities.Role> repository) : IRequestHandler<GetAllRolesRequest, PaginatedResponse<RoleDto>>
{
    private readonly IRepository<Domain.Entities.Role> _repository = repository;
    public async Task<PaginatedResponse<RoleDto>> Handle(GetAllRolesRequest request, CancellationToken cancellationToken)
    {
        var allRoles = await _repository.ListAsync(r => !r.IsDeleted, cancellationToken);

        var roleDtos = allRoles.Select(r => new RoleDto
        {
            Id = r.Id,
            Name = r.Name,
            Key = r.Key,
            Description = r.Description ?? string.Empty
        }).ToList();

        if (roleDtos.Count is 0)
        { 
            return new PaginatedResponse<RoleDto>(roleDtos, request.PageNumber, request.PageSize)
            {
                Data = roleDtos,
                Message = "No records found.",
                Succeeded = true
            };
        }

        return new PaginatedResponse<RoleDto>(roleDtos, request.PageNumber, request.PageSize)
        {
            Data = roleDtos,
            Message = "Roles retrieved successfully.",
            Succeeded = true
        };
    }
}
