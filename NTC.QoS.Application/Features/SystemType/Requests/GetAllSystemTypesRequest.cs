using MediatR;
using NTC.QoS.Domain.Models.DTOs.SystemType;

namespace NTC.QoS.Application.Features.SystemType.Requests;

public class GetAllSystemTypesRequest(int pageSize, int pageNumber, List<Guid>? technologyIds, List<Guid>? generationIds) : IRequest<PaginatedResponse<SystemTypeDto>>
{
    public int PageNumber { get; set; } = pageNumber;
    public int PageSize { get; set; } = pageSize;
    public List<Guid>? TechnologyIds { get; set; } = technologyIds;
    public List<Guid>? GenerationIds { get; set; } = generationIds;
}