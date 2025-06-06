using MediatR;
using NTC.QoS.Application.Features.Technology.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Technology;
using NTC.QoS.Domain.Models.DTOs.Generation;

namespace NTC.QoS.Application.Features.Technology.Handlers.Queries;

public class GetAllTechnologiesHandler(ITechnologyRepository technologyRepository) : IRequestHandler<GetAllTechnologiesRequest, PaginatedResponse<TechnologyDto>>
{
    private readonly ITechnologyRepository _technologyRepository = technologyRepository;

    public async Task<PaginatedResponse<TechnologyDto>> Handle(GetAllTechnologiesRequest request, CancellationToken cancellationToken)
    {
        var technologyQuery = _technologyRepository.Query(t => !t.IsDeleted);

        technologyQuery = technologyQuery.Where(t => request.GenerationIds == null || request.GenerationIds.Contains(t.GenerationId));

        var technologyQueryDtos = technologyQuery.Select(t => new TechnologyDto
        {
            Id = t.Id,
            Standard = t.Standard,
            Description = t.Description,
            GenerationId = t.GenerationId,
            Generation = new GenerationDto
            {
                Id = t.GenerationId,
                Name = t.Generation.Name
            },
            CreatedBy = t.CreatedBy,
            CreatedDate = t.CreatedAt,
            ModifiedBy = t.UpdatedBy,
            ModifiedDate = t.UpdatedAt
        });

        return await _technologyRepository.PaginateAsync(technologyQueryDtos, request.PageNumber, request.PageSize, cancellationToken);
    }
}
