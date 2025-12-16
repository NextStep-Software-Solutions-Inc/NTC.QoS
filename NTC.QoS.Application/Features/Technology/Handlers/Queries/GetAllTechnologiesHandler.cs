using MediatR;
using NTC.QoS.Application.Features.Technology.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.Technology;

namespace NTC.QoS.Application.Features.Technology.Handlers.Queries;

public class GetAllTechnologiesHandler(ITechnologyRepository technologyRepository) : IRequestHandler<GetAllTechnologiesRequest, PaginatedResponse<TechnologyDto>>
{
    private readonly ITechnologyRepository _technologyRepository = technologyRepository;

    public async Task<PaginatedResponse<TechnologyDto>> Handle(GetAllTechnologiesRequest request, CancellationToken cancellationToken)
    {
        var technologyQuery = _technologyRepository.Query();

        technologyQuery = technologyQuery.Where(t => request.GenerationIds == null || request.GenerationIds.Contains(t.GenerationId));

        var technologyQueryDtos = technologyQuery.Select(t => new TechnologyDto
        {
            Id = t.Id,
            Standard = t.Standard,
            Description = t.Description,
            Generation = new()
            {
                Id = t.Generation.Id,
                Name = t.Generation.Name,
                Description = t.Generation.Description
            }
        });

        return await _technologyRepository.PaginateAsync(technologyQueryDtos, request.PageNumber, request.PageSize, cancellationToken);
    }
}
