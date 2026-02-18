using MediatR;
using NTC.QoS.Application.Features.SystemType.Requests;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.SystemType;

namespace NTC.QoS.Application.Features.SystemType.Handlers.Queries;

public class GetAllSystemTypesHandler(ISystemTypeRepository repository)
    : IRequestHandler<GetAllSystemTypesRequest, PaginatedResponse<SystemTypeDto>>
{
    private readonly ISystemTypeRepository _repository = repository;

    public async Task<PaginatedResponse<SystemTypeDto>> Handle(GetAllSystemTypesRequest request, CancellationToken cancellationToken)
    {
        var systemTypeQuery = _repository.Query();

        if (request.TechnologyIds != null && request.TechnologyIds.Count > 0)
        {
            systemTypeQuery = systemTypeQuery.Where(st => request.TechnologyIds.Contains(st.TechnologyId));
        }

        if (request.GenerationIds != null && request.GenerationIds.Count > 0)
        {
            systemTypeQuery = systemTypeQuery.Where(st => request.GenerationIds.Contains(st.GenerationId));
        }

        if (!string.IsNullOrEmpty(request.Search))
        {
            systemTypeQuery = _repository.ApplySearch(systemTypeQuery, request.Search, st => st.Name);
        }

        var query = systemTypeQuery
            .OrderBy(st => st.Name)
            .Select(st => new SystemTypeDto
            {
                Id = st.Id,
                Name = st.Name,
                Technology = st.Technology != null ? new()
                {
                    Id = st.Technology.Id,
                    Standard = st.Technology.Standard,
                    Description = st.Technology.Description,
                    Generation = new()
                    {
                        Id = st.Technology.GenerationId,
                        Name = st.Technology.Generation.Name,
                        Description = st.Technology.Generation.Description,
                    }
                } : null,
                Generation = st.Generation != null ? new()
                {
                    Id = st.Generation.Id,
                    Name = st.Generation.Name,
                    Description = st.Generation.Description
                } : null
            });

        return await _repository.PaginateAsync(query, request.PageNumber, request.PageSize, cancellationToken);
    }
}
