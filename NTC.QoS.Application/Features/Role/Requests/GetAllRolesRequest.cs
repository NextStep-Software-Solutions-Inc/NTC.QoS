using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Role;

namespace NTC.QoS.Application.Features.Role.Requests;

public class GetAllRolesRequest(int pageSize, int pageNumber, string? search = null, string? scope = null, Guid? regionId = null) : IRequest<PaginatedResponse<RoleDto>>
{
    public int PageSize { get; set; } = pageSize;
    public int PageNumber { get; set; } = pageNumber;
    public string? Search { get; set; } = search;
    public string? Scope { get; set; } = scope;
    public Guid? RegionId { get; set; } = regionId;
}
