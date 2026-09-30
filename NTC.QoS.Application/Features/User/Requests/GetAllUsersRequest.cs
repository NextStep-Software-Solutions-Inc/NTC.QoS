using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.User;

namespace NTC.QoS.Application.Features.User.Requests;

public class GetAllUsersRequest(
    int pageSize,
    int pageNumber,
    Guid? roleId = null,
    List<string>? authUserIds = null,
    List<Guid>? regionIds = null,
    string? search = null,
    string? email = null) : IRequest<PaginatedResponse<UserDto>>
{
    public int PageSize { get; set; } = pageSize;
    public int PageNumber { get; set; } = pageNumber;
    public Guid? RoleId { get; set; } = roleId;
    public List<string>? AuthUserIds { get; set; } = authUserIds;
    public List<Guid>? RegionIds { get; set; } = regionIds;
    public string? Search { get; set; } = search;
    public string? Email { get; set; } = email;
}
