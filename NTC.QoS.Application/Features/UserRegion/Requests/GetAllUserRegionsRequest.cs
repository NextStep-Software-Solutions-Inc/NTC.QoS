using MediatR;
using NTC.QoS.Domain.Models.DTOs.UserRegion;

namespace NTC.QoS.Application.Features.UserRegion.Requests;

public record GetAllUserRegionsRequest : IRequest<PaginatedResponse<UserRegionDto>>
{
    public int PageSize { get; set; } = 10;
    public int PageNumber { get; set; } = 1;
    public List<Guid>? UserIds { get; set; } 
    public List<Guid>? RegionIds { get; set; }
    public List<string>? Statuses { get; set; }
}
