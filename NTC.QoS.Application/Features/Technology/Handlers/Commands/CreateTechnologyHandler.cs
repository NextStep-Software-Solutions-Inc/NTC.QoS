using MediatR;
using NTC.QoS.Application.Features.Technology.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Technology;

namespace NTC.QoS.Application.Features.Technology.Handlers.Commands;

public class CreateTechnologyHandler (IRepository<Domain.Entities.Technology> repository, IGenerationRepository generationRepository): IRequestHandler<CreateTechnologyRequest, ApiResponse<TechnologyDto>>
{
    private readonly IRepository<Domain.Entities.Technology> _repository = repository;
    IGenerationRepository _generationRepository = generationRepository;
    public async Task<ApiResponse<TechnologyDto>> Handle(CreateTechnologyRequest request, CancellationToken cancellationToken)
    {
        var technologyQuery = _repository.Query(t => t.Standard == request.Technology.Standard);
        var technology = await _repository.FirstOrDefaultAsync(technologyQuery, cancellationToken);

        if (technology is not null)
        {
            return new ApiResponse<TechnologyDto>
            {
                Message = "Technology standard already exists.",
                Succeeded = false
            };
        }

        var generationQuery = _generationRepository.Query(g => g.Id == request.Technology.GenerationId);
        var generation = await _generationRepository.FirstOrDefaultAsync(generationQuery, cancellationToken);

        if (generation is null)
        {
            return new ApiResponse<TechnologyDto>
            {
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    {"Error",  ["Generation not found."] }
                },
                Message = "Generation not found.",
            };
        }

        var newTechnology = new Domain.Entities.Technology
        {
            GenerationId = generation.Id,
            Standard = request.Technology.Standard,
            Description = request.Technology.Description,
        };

        _repository.Add(newTechnology);
        await _repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<TechnologyDto>
        {
            Data = new TechnologyDto()
            {
                Id = newTechnology.Id,
                Standard = newTechnology.Standard,
                Description = newTechnology.Description,
                Generation = new()
                {
                    Id = newTechnology.Generation.Id,
                    Name = newTechnology.Generation.Name,
                    Description = newTechnology.Generation.Description
                }
            },
            Succeeded = true,
            Message = "Technology standard created successfully.",
        };
    }
}