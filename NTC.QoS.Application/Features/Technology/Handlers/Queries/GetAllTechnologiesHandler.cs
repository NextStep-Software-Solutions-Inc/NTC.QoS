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
        var allTechnologies = await _technologyRepository.ListAsync(t => !t.IsDeleted, cancellationToken);

        if (request.GenerationId.HasValue)
        {
            allTechnologies = allTechnologies.Where(t => t.GenerationId == request.GenerationId.Value).ToList();
        }

        var technologyDtos = allTechnologies.Select(t => new TechnologyDto
        {
            Id = t.Id,
            Standard = t.Standard,
            Description = t.Description,
            GenerationId = t.GenerationId,
            Generation = new GenerationDto
            {
                Id = t.GenerationId,
                Name = t.Generation?.Name ?? ""
            },
            CreatedBy = t.CreatedBy,
            CreatedDate = t.CreatedAt,
            ModifiedBy = t.UpdatedBy,
            ModifiedDate = t.UpdatedAt 
        }).ToList();


        return new PaginatedResponse<TechnologyDto>(technologyDtos, request.PageNumber, request.PageSize);
    }
}
