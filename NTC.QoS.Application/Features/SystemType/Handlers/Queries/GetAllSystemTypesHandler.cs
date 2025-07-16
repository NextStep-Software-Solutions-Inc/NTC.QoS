using MediatR;
using NTC.QoS.Application.Features.SystemType.Requests;
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

        systemTypeQuery = systemTypeQuery.Where(st => request.TechnologyIds == null || request.TechnologyIds.Contains(st.TechnologyId));

        var query = systemTypeQuery
            .OrderBy(st => st.Name)
            .Select(st => new SystemTypeDto
            {
                Id = st.Id,
                Name = st.Name,
                Technology = new()
                {
                    Id = st.TechnologyId,
                    Standard = st.Technology.Standard,
                    Description = st.Technology.Description,
                    Generation = new()
                    {
                        Id = st.Technology.GenerationId,
                        Name = st.Technology.Generation.Name,
                        Description = st.Technology.Generation.Description,
                    }
                }
            });

        return await _repository.PaginateAsync(query, request.PageNumber, request.PageSize, cancellationToken);
    }
}
