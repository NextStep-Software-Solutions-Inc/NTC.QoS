using MediatR;
using NTC.QoS.Domain.Models.DTOs.SystemType;

namespace NTC.QoS.Application.Features.SystemType.Requests;

public class GetAllSystemTypesRequest(int pageSize, int pageNumber, List<Guid>? technologyIds) : IRequest<PaginatedResponse<SystemTypeDto>>
{
    public int PageNumber { get; set; } = pageNumber;
    public int PageSize { get; set; } = pageSize;
    public string? Search { get; set; }
    public List<Guid>? TechnologyIds { get; set; } = technologyIds;
}